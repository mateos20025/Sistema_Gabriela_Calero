using Consultorio.Application.Abstracciones;
using Consultorio.Application.Comun;
using Consultorio.Application.Pagos;
using Consultorio.Domain.Pagos;

namespace Consultorio.Application.Tests.Pagos;

internal sealed class RepositorioPagosEnMemoria : IRepositorioPagos
{
    public List<Pago> Pagos { get; } = [];

    public bool LanzarDuplicadoAlAgregar { get; set; }

    public Exception? ExcepcionAlAgregar { get; set; }

    public bool LanzarConflictoAlActualizar { get; set; }

    public Task<Pago?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Pagos.FirstOrDefault(p => p.Id == id));

    public Task<PagoDetalle?> ObtenerDetalleAsync(Guid id, CancellationToken cancellationToken)
    {
        var pago = Pagos.FirstOrDefault(p => p.Id == id);
        return Task.FromResult(pago is null
            ? null
            : new PagoDetalle(
                pago.Id, pago.PacienteId, "María Pérez", "1710034065", pago.CitaId, null, pago.Metodo, pago.Estado, pago.Monto, pago.Concepto,
                pago.NumeroComprobante, pago.ComprobanteTipo, pago.ComprobanteTamanoBytes, "Asistente", pago.CreadoEn,
                null, pago.ResueltoEn, pago.MotivoResolucion, []));
    }

    public Task<PagoConMismoComprobante?> BuscarPorComprobanteAsync(string hashSha256, CancellationToken cancellationToken)
    {
        var existe = Pagos.Any(p => p.ComprobanteHash == hashSha256 && p.Estado != EstadoPago.Anulado);
        return Task.FromResult(existe ? new PagoConMismoComprobante(DateTime.UtcNow, "María Pérez") : null);
    }

    public Task<PaginaResultado<PagoResumen>> BuscarAsync(FiltroPagos filtro, CancellationToken cancellationToken) =>
        Task.FromResult(new PaginaResultado<PagoResumen>([], 0));

    public Task<IReadOnlyList<PagoResumen>> ListarPorPacienteAsync(Guid pacienteId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PagoResumen>>([]);

    public Task<IReadOnlyList<PagoResumen>> ListarPorCitaAsync(Guid citaId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PagoResumen>>([]);

    public Task AgregarAsync(Pago pago, CancellationToken cancellationToken)
    {
        if (ExcepcionAlAgregar is not null)
        {
            throw ExcepcionAlAgregar;
        }

        if (LanzarDuplicadoAlAgregar)
        {
            throw new RegistroDuplicadoException();
        }

        Pagos.Add(pago);
        return Task.CompletedTask;
    }

    public Task ActualizarAsync(Pago pago, CancellationToken cancellationToken) =>
        LanzarConflictoAlActualizar ? throw new ConflictoConcurrenciaException() : Task.CompletedTask;
}

internal sealed class AlmacenArchivosEnMemoria : IAlmacenArchivos
{
    public Dictionary<string, byte[]> Archivos { get; } = [];

    public int Eliminados { get; private set; }

    public Task<string> GuardarAsync(string carpeta, string extension, ReadOnlyMemory<byte> contenido, CancellationToken cancellationToken)
    {
        var id = $"{carpeta}/2026/10/{Guid.NewGuid():N}{extension}";
        Archivos[id] = contenido.ToArray();
        return Task.FromResult(id);
    }

    public Task<Stream?> AbrirAsync(string archivoId, CancellationToken cancellationToken) =>
        Task.FromResult<Stream?>(Archivos.TryGetValue(archivoId, out var datos) ? new MemoryStream(datos) : null);

    public Task EliminarAsync(string archivoId, CancellationToken cancellationToken)
    {
        if (Archivos.Remove(archivoId))
        {
            Eliminados++;
        }

        return Task.CompletedTask;
    }
}

internal sealed class RepositorioConfiguracionPagosEnMemoria : IRepositorioConfiguracionPagos
{
    public ConfiguracionPagos Configuracion { get; } = ConfiguracionPagos.Nueva();

    public int Guardados { get; private set; }

    public bool LanzarConflictoAlGuardar { get; set; }

    public Task<ConfiguracionPagos> ObtenerAsync(CancellationToken cancellationToken) => Task.FromResult(Configuracion);

    public Task GuardarAsync(ConfiguracionPagos configuracion, CancellationToken cancellationToken)
    {
        if (LanzarConflictoAlGuardar)
        {
            throw new ConflictoConcurrenciaException();
        }

        Guardados++;
        return Task.CompletedTask;
    }
}

internal static class ArchivosPrueba
{
    public static byte[] Png(int relleno = 64) =>
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, .. Enumerable.Repeat((byte)relleno, 100)];

    public static byte[] Pdf() => [.. "%PDF-1.7\n"u8, .. Enumerable.Repeat((byte)'x', 100)];

    public static byte[] Gif() => [.. "GIF89a"u8, .. Enumerable.Repeat((byte)0, 100)];
}
