using System.Security.Cryptography;
using Consultorio.Application.Abstracciones;
using Consultorio.Application.Citas;
using Consultorio.Application.Comun;
using Consultorio.Application.Pacientes;
using Consultorio.Domain.Pagos;
using FluentValidation;

namespace Consultorio.Application.Pagos;

/// <summary>
/// Caso de uso: el personal registra un pago (transferencia con su captura, o efectivo por cobrar).
/// </summary>
public sealed class RegistrarPago(
    IRepositorioPagos pagos,
    IRepositorioPacientes pacientes,
    IRepositorioCitas citas,
    IAlmacenArchivos almacen,
    IUsuarioActual usuarioActual,
    IValidator<RegistrarPagoComando> validador)
{
    public const string CarpetaComprobantes = "comprobantes";

    public async Task<Resultado<Guid>> EjecutarAsync(
        RegistrarPagoComando comando, ArchivoSubido? archivo, CancellationToken cancellationToken)
    {
        var sesion = await usuarioActual.ExigirSesionAsync();

        var validacion = await validador.ValidateAsync(comando, cancellationToken);
        if (!validacion.IsValid)
        {
            return Resultado<Guid>.Falla(validacion.Errors.Select(e => e.ErrorMessage).Distinct());
        }

        var paciente = await pacientes.ObtenerPorIdAsync(comando.PacienteId, cancellationToken);
        if (paciente is null)
        {
            return Resultado<Guid>.Falla(MensajesPagos.PacienteNoExiste);
        }

        if (paciente.EstaDeshabilitado)
        {
            return Resultado<Guid>.Falla(MensajesPagos.PacienteDeshabilitado);
        }

        var citaId = comando.CitaId is { } id && id != Guid.Empty ? id : (Guid?)null;
        if (citaId is not null)
        {
            var cita = await citas.ObtenerPorIdAsync(citaId.Value, cancellationToken);
            if (cita is null || cita.PacienteId != paciente.Id)
            {
                return Resultado<Guid>.Falla(MensajesPagos.CitaNoCorresponde);
            }
        }

        MontoDolares.TryParse(comando.Monto, out var monto);

        if (comando.Metodo == MetodoPago.Efectivo)
        {
            if (archivo is not null)
            {
                return Resultado<Guid>.Falla(MensajesPagos.EfectivoSinComprobante);
            }

            var pagoEfectivo = Pago.RegistrarEfectivo(paciente.Id, monto, comando.Concepto, sesion.Id, citaId);
            await pagos.AgregarAsync(pagoEfectivo, cancellationToken);
            return Resultado<Guid>.Ok(pagoEfectivo.Id);
        }

        return await RegistrarTransferenciaAsync(comando, archivo, paciente.Id, citaId, monto, sesion.Id, cancellationToken);
    }

    private async Task<Resultado<Guid>> RegistrarTransferenciaAsync(
        RegistrarPagoComando comando,
        ArchivoSubido? archivo,
        Guid pacienteId,
        Guid? citaId,
        decimal monto,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        if (archivo is null || archivo.Contenido.Length == 0)
        {
            return Resultado<Guid>.Falla(MensajesPagos.ComprobanteObligatorio);
        }

        if (archivo.Contenido.Length > ArchivosComprobante.TamanoMaximoBytes)
        {
            return Resultado<Guid>.Falla(MensajesPagos.ComprobanteMuyGrande);
        }

        // El tipo se decide por el contenido real, no por la extensión del nombre.
        if (ArchivosComprobante.DetectarTipo(archivo.Contenido) is not { } tipo)
        {
            return Resultado<Guid>.Falla(MensajesPagos.TipoNoPermitido);
        }

        var hash = Convert.ToHexStringLower(SHA256.HashData(archivo.Contenido));
        var repetido = await pagos.BuscarPorComprobanteAsync(hash, cancellationToken);
        if (repetido is not null)
        {
            return Resultado<Guid>.Falla(MensajesPagos.ComprobanteRepetido(repetido));
        }

        var archivoId = await almacen.GuardarAsync(
            CarpetaComprobantes, ArchivosComprobante.Extension(tipo), archivo.Contenido, cancellationToken);

        try
        {
            var comprobante = new ComprobanteTransferencia(archivoId, tipo, archivo.Contenido.Length, hash);
            var pago = Pago.RegistrarTransferencia(
                pacienteId, monto, comando.Concepto, comando.NumeroComprobante, comprobante, usuarioId, citaId);
            await pagos.AgregarAsync(pago, cancellationToken);
            return Resultado<Guid>.Ok(pago.Id);
        }
        catch (RegistroDuplicadoException)
        {
            // Otra persona registró la misma captura en el mismo instante.
            await almacen.EliminarAsync(archivoId, CancellationToken.None);
            return Resultado<Guid>.Falla(MensajesPagos.ComprobanteYaRegistrado);
        }
        catch
        {
            // Si no se pudo guardar el pago, no se deja un archivo huérfano en el disco.
            await almacen.EliminarAsync(archivoId, CancellationToken.None);
            throw;
        }
    }
}
