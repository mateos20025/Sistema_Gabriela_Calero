using Consultorio.Application.Abstracciones;
using Consultorio.Application.Auditoria;
using Consultorio.Application.Comun;
using Consultorio.Domain.Pagos;

namespace Consultorio.Application.Pagos;

/// <summary>Caso de uso: lista paginada de pagos filtrada por estado y por paciente (nombre o cédula).</summary>
public sealed class BuscarPagos(IRepositorioPagos repositorio, IUsuarioActual usuarioActual)
{
    public async Task<PaginaResultado<PagoResumen>> EjecutarAsync(FiltroPagos filtro, CancellationToken cancellationToken)
    {
        await usuarioActual.ExigirSesionAsync();

        var texto = filtro.Texto?.Trim();
        if (texto?.Length > FiltroPagos.LongitudMaximaTexto)
        {
            texto = texto[..FiltroPagos.LongitudMaximaTexto];
        }

        var filtroSeguro = filtro with
        {
            Texto = string.IsNullOrEmpty(texto) ? null : texto,
            Estado = Enum.IsDefined(filtro.Estado) ? filtro.Estado : FiltroEstadoPago.Pendientes,
            Pagina = Math.Max(0, filtro.Pagina),
            TamanoPagina = Math.Clamp(filtro.TamanoPagina, 1, FiltroPagos.TamanoPaginaMaximo),
        };

        return await repositorio.BuscarAsync(filtroSeguro, cancellationToken);
    }
}

/// <summary>Caso de uso: ficha de un pago. El historial de cambios solo lo ve la doctora.</summary>
public sealed class ObtenerPago(IRepositorioPagos repositorio, IConsultaAuditoria auditoria, IUsuarioActual usuarioActual)
{
    public async Task<PagoDetalle?> EjecutarAsync(Guid pagoId, CancellationToken cancellationToken)
    {
        var sesion = await usuarioActual.ExigirSesionAsync();

        var detalle = await repositorio.ObtenerDetalleAsync(pagoId, cancellationToken);
        if (detalle is null || !sesion.EsDoctora)
        {
            return detalle;
        }

        var historial = await auditoria.ObtenerHistorialAsync(nameof(Pago), pagoId, cancellationToken);
        return detalle with { Historial = historial };
    }
}

/// <summary>Caso de uso: pagos de un paciente, para su ficha.</summary>
public sealed class ListarPagosDePaciente(IRepositorioPagos repositorio, IUsuarioActual usuarioActual)
{
    public async Task<IReadOnlyList<PagoResumen>> EjecutarAsync(Guid pacienteId, CancellationToken cancellationToken)
    {
        await usuarioActual.ExigirSesionAsync();
        return await repositorio.ListarPorPacienteAsync(pacienteId, cancellationToken);
    }
}

/// <summary>Caso de uso: pagos de una cita, para su ficha.</summary>
public sealed class ListarPagosDeCita(IRepositorioPagos repositorio, IUsuarioActual usuarioActual)
{
    public async Task<IReadOnlyList<PagoResumen>> EjecutarAsync(Guid citaId, CancellationToken cancellationToken)
    {
        await usuarioActual.ExigirSesionAsync();
        return await repositorio.ListarPorCitaAsync(citaId, cancellationToken);
    }
}

/// <summary>Caso de uso: abrir la captura de una transferencia (solo personal con sesión).</summary>
public sealed class ObtenerComprobantePago(IRepositorioPagos repositorio, IAlmacenArchivos almacen, IUsuarioActual usuarioActual)
{
    public async Task<ArchivoDescargable?> EjecutarAsync(Guid pagoId, CancellationToken cancellationToken)
    {
        await usuarioActual.ExigirSesionAsync();

        var pago = await repositorio.ObtenerPorIdAsync(pagoId, cancellationToken);
        if (pago?.ComprobanteArchivoId is null || pago.ComprobanteTipo is null)
        {
            return null;
        }

        var contenido = await almacen.AbrirAsync(pago.ComprobanteArchivoId, cancellationToken);
        return contenido is null
            ? null
            : new ArchivoDescargable(contenido, ArchivosComprobante.TipoContenido(pago.ComprobanteTipo.Value));
    }
}
