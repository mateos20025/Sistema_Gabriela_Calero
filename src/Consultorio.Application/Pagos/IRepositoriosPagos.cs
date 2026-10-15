using Consultorio.Application.Comun;
using Consultorio.Domain.Pagos;

namespace Consultorio.Application.Pagos;

public interface IRepositorioPagos
{
    Task<Pago?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Ficha del pago con los nombres del paciente y de quienes lo registraron y resolvieron (sin historial).</summary>
    Task<PagoDetalle?> ObtenerDetalleAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Busca un pago no anulado que ya use la misma captura (misma huella SHA-256).</summary>
    Task<PagoConMismoComprobante?> BuscarPorComprobanteAsync(string hashSha256, CancellationToken cancellationToken);

    Task<PaginaResultado<PagoResumen>> BuscarAsync(FiltroPagos filtro, CancellationToken cancellationToken);

    Task<IReadOnlyList<PagoResumen>> ListarPorPacienteAsync(Guid pacienteId, CancellationToken cancellationToken);

    /// <exception cref="RegistroDuplicadoException">La captura ya está en otro pago.</exception>
    Task AgregarAsync(Pago pago, CancellationToken cancellationToken);

    /// <exception cref="ConflictoConcurrenciaException">Otra persona lo modificó antes.</exception>
    Task ActualizarAsync(Pago pago, CancellationToken cancellationToken);
}

public interface IRepositorioConfiguracionPagos
{
    /// <summary>Devuelve la configuración guardada, o una vacía si la doctora aún no la ha definido.</summary>
    Task<ConfiguracionPagos> ObtenerAsync(CancellationToken cancellationToken);

    /// <exception cref="ConflictoConcurrenciaException">Otra persona la modificó antes.</exception>
    Task GuardarAsync(ConfiguracionPagos configuracion, CancellationToken cancellationToken);
}
