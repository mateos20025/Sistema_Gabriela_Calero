namespace Consultorio.Application.Abstracciones;

/// <summary>
/// Dónde se guardan los archivos subidos (capturas de pago). Hoy en el disco del servidor, fuera de la
/// carpeta pública; más adelante podría ser un servicio como S3 o R2 sin cambiar los casos de uso.
/// </summary>
public interface IAlmacenArchivos
{
    /// <summary>Guarda el archivo con un nombre aleatorio y devuelve su identificador.</summary>
    /// <param name="carpeta">Solo letras minúsculas, por ejemplo "comprobantes".</param>
    /// <param name="extension">Con punto, por ejemplo ".pdf".</param>
    Task<string> GuardarAsync(string carpeta, string extension, ReadOnlyMemory<byte> contenido, CancellationToken cancellationToken);

    /// <summary>Abre el archivo para leerlo, o devuelve <c>null</c> si no existe.</summary>
    Task<Stream?> AbrirAsync(string archivoId, CancellationToken cancellationToken);

    Task EliminarAsync(string archivoId, CancellationToken cancellationToken);
}
