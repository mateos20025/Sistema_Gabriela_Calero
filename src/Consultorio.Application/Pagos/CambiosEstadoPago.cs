using Consultorio.Application.Abstracciones;
using Consultorio.Application.Comun;
using Consultorio.Domain.Pagos;

namespace Consultorio.Application.Pagos;

/// <summary>Pasos comunes de los cambios de estado: cargar, comprobar, aplicar y guardar.</summary>
internal static class CambioEstadoPago
{
    public static async Task<Resultado> AplicarAsync(
        IRepositorioPagos repositorio,
        Guid pagoId,
        Func<Pago, string?> comprobar,
        Action<Pago> aplicar,
        CancellationToken cancellationToken)
    {
        var pago = await repositorio.ObtenerPorIdAsync(pagoId, cancellationToken);
        if (pago is null)
        {
            return Resultado.Falla(MensajesPagos.NoExiste);
        }

        if (comprobar(pago) is { } error)
        {
            return Resultado.Falla(error);
        }

        aplicar(pago);

        try
        {
            await repositorio.ActualizarAsync(pago, cancellationToken);
        }
        catch (ConflictoConcurrenciaException)
        {
            return Resultado.Falla(MensajesPagos.ModificadoPorOtraPersona);
        }

        return Resultado.Ok();
    }
}

/// <summary>Caso de uso: la asistente o la doctora confirman que la transferencia llegó.</summary>
public sealed class ValidarPago(IRepositorioPagos repositorio, IUsuarioActual usuarioActual, IRelojSistema reloj)
{
    public async Task<Resultado> EjecutarAsync(Guid pagoId, CancellationToken cancellationToken)
    {
        var sesion = await usuarioActual.ExigirSesionAsync();

        return await CambioEstadoPago.AplicarAsync(
            repositorio,
            pagoId,
            p => p.Estado == EstadoPago.PendienteValidacion ? null : MensajesPagos.NoEstaPorValidar,
            p => p.Validar(sesion.Id, reloj.AhoraUtc),
            cancellationToken);
    }
}

/// <summary>Caso de uso: rechazar una transferencia (no llegó, monto incorrecto, captura ilegible...).</summary>
public sealed class RechazarPago(IRepositorioPagos repositorio, IUsuarioActual usuarioActual, IRelojSistema reloj)
{
    public async Task<Resultado> EjecutarAsync(Guid pagoId, string motivo, CancellationToken cancellationToken)
    {
        var sesion = await usuarioActual.ExigirSesionAsync();

        if (!Pago.MotivoEsValido(motivo))
        {
            return Resultado.Falla(MensajesPagos.MotivoObligatorio);
        }

        return await CambioEstadoPago.AplicarAsync(
            repositorio,
            pagoId,
            p => p.Estado == EstadoPago.PendienteValidacion ? null : MensajesPagos.NoEstaPorValidar,
            p => p.Rechazar(sesion.Id, reloj.AhoraUtc, motivo),
            cancellationToken);
    }
}

/// <summary>Caso de uso: el paciente pagó en efectivo en el consultorio.</summary>
public sealed class MarcarPagoRecibido(IRepositorioPagos repositorio, IUsuarioActual usuarioActual, IRelojSistema reloj)
{
    public async Task<Resultado> EjecutarAsync(Guid pagoId, CancellationToken cancellationToken)
    {
        var sesion = await usuarioActual.ExigirSesionAsync();

        return await CambioEstadoPago.AplicarAsync(
            repositorio,
            pagoId,
            p => p.Estado == EstadoPago.PendienteEnConsultorio ? null : MensajesPagos.NoEstaPorCobrar,
            p => p.MarcarPagado(sesion.Id, reloj.AhoraUtc),
            cancellationToken);
    }
}

/// <summary>Caso de uso: anular un pago pendiente (registrado por error, paciente que no llegó...).</summary>
public sealed class AnularPago(IRepositorioPagos repositorio, IUsuarioActual usuarioActual, IRelojSistema reloj)
{
    public async Task<Resultado> EjecutarAsync(Guid pagoId, string motivo, CancellationToken cancellationToken)
    {
        var sesion = await usuarioActual.ExigirSesionAsync();

        if (!Pago.MotivoEsValido(motivo))
        {
            return Resultado.Falla(MensajesPagos.MotivoObligatorio);
        }

        return await CambioEstadoPago.AplicarAsync(
            repositorio,
            pagoId,
            p => p.EstaPendiente ? null : MensajesPagos.NoEstaPendiente,
            p => p.Anular(sesion.Id, reloj.AhoraUtc, motivo),
            cancellationToken);
    }
}
