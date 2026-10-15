using Consultorio.Application.Abstracciones;
using Consultorio.Application.Comun;
using Consultorio.Domain.Pagos;
using FluentValidation;

namespace Consultorio.Application.Pagos;

/// <summary>
/// Caso de uso: ver la configuración de pagos. Todo el personal puede verla
/// (la asistente necesita los datos bancarios para enviarlos a los pacientes).
/// </summary>
public sealed class ObtenerConfiguracionPagos(IRepositorioConfiguracionPagos repositorio, IUsuarioActual usuarioActual)
{
    public async Task<ConfiguracionPagosVista> EjecutarAsync(CancellationToken cancellationToken)
    {
        await usuarioActual.ExigirSesionAsync();
        return ConfiguracionPagosVista.Desde(await repositorio.ObtenerAsync(cancellationToken));
    }
}

/// <summary>Caso de uso: la doctora define el precio de la consulta y la cuenta para transferencias.</summary>
public sealed class ActualizarConfiguracionPagos(
    IRepositorioConfiguracionPagos repositorio,
    IUsuarioActual usuarioActual,
    IValidator<ConfiguracionPagosComando> validador)
{
    public async Task<Resultado> EjecutarAsync(ConfiguracionPagosComando comando, CancellationToken cancellationToken)
    {
        await usuarioActual.ExigirDoctoraAsync();

        var validacion = await validador.ValidateAsync(comando, cancellationToken);
        if (!validacion.IsValid)
        {
            return Resultado.Falla(validacion.Errors.Select(e => e.ErrorMessage).Distinct());
        }

        var configuracion = await repositorio.ObtenerAsync(cancellationToken);
        if (configuracion.Version != comando.Version)
        {
            return Resultado.Falla(MensajesPagos.ModificadoPorOtraPersona);
        }

        decimal? precio = MontoDolares.TryParse(comando.PrecioConsulta, out var monto) ? monto : null;
        var datosBancarios = comando.RecibeTransferencias
            ? new DatosBancarios(comando.Banco, comando.TipoCuenta, comando.NumeroCuenta, comando.Titular, comando.IdentificacionTitular)
            : null;

        configuracion.Actualizar(precio, comando.ConceptoPredeterminado, datosBancarios);

        try
        {
            await repositorio.GuardarAsync(configuracion, cancellationToken);
        }
        catch (ConflictoConcurrenciaException)
        {
            return Resultado.Falla(MensajesPagos.ModificadoPorOtraPersona);
        }

        return Resultado.Ok();
    }
}
