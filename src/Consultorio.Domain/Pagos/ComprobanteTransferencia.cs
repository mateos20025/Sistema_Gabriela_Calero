using Consultorio.Domain.Comun;

namespace Consultorio.Domain.Pagos;

/// <summary>Reglas de las capturas de transferencia.</summary>
public static class ArchivosComprobante
{
    public const int TamanoMaximoBytes = 5 * 1024 * 1024;

    /// <summary>
    /// Reconoce el tipo por los primeros bytes del contenido ("firma"), no por la extensión del nombre:
    /// renombrar un archivo peligroso a ".jpg" no sirve para colarlo.
    /// </summary>
    public static TipoArchivoComprobante? DetectarTipo(ReadOnlySpan<byte> contenido)
    {
        if (contenido.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF]))
        {
            return TipoArchivoComprobante.Jpg;
        }

        if (contenido.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]))
        {
            return TipoArchivoComprobante.Png;
        }

        if (contenido.StartsWith("%PDF-"u8))
        {
            return TipoArchivoComprobante.Pdf;
        }

        return null;
    }

    public static string Extension(TipoArchivoComprobante tipo) => tipo switch
    {
        TipoArchivoComprobante.Jpg => ".jpg",
        TipoArchivoComprobante.Png => ".png",
        TipoArchivoComprobante.Pdf => ".pdf",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo)),
    };

    public static string TipoContenido(TipoArchivoComprobante tipo) => tipo switch
    {
        TipoArchivoComprobante.Jpg => "image/jpeg",
        TipoArchivoComprobante.Png => "image/png",
        TipoArchivoComprobante.Pdf => "application/pdf",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo)),
    };
}

/// <summary>
/// Captura de una transferencia ya guardada en el almacén de archivos.
/// <see cref="HashSha256"/> es la "huella" del contenido: dos archivos idénticos tienen la misma huella,
/// así se detecta si alguien reutiliza la misma captura para dos pagos.
/// </summary>
public sealed record ComprobanteTransferencia
{
    public const int LongitudMaximaArchivoId = 200;
    public const int LongitudHash = 64;

    public ComprobanteTransferencia(string archivoId, TipoArchivoComprobante tipo, long tamanoBytes, string hashSha256)
    {
        if (string.IsNullOrWhiteSpace(archivoId) || archivoId.Length > LongitudMaximaArchivoId)
        {
            throw new ExcepcionDominio("El identificador del archivo no es válido.");
        }

        if (!Enum.IsDefined(tipo))
        {
            throw new ExcepcionDominio("El tipo de archivo no es válido.");
        }

        if (tamanoBytes is <= 0 or > ArchivosComprobante.TamanoMaximoBytes)
        {
            throw new ExcepcionDominio("El tamaño del archivo no es válido.");
        }

        if (hashSha256.Length != LongitudHash || !hashSha256.All(c => char.IsAsciiDigit(c) || c is >= 'a' and <= 'f'))
        {
            throw new ExcepcionDominio("La huella SHA-256 no es válida.");
        }

        ArchivoId = archivoId;
        Tipo = tipo;
        TamanoBytes = tamanoBytes;
        HashSha256 = hashSha256;
    }

    public string ArchivoId { get; }

    public TipoArchivoComprobante Tipo { get; }

    public long TamanoBytes { get; }

    public string HashSha256 { get; }
}
