using System.Text.RegularExpressions;
using Consultorio.Application.Abstracciones;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Consultorio.Infrastructure.Archivos;

/// <summary>Sección "Archivos" de la configuración.</summary>
public sealed class OpcionesArchivos
{
    public const string Seccion = "Archivos";

    /// <summary>
    /// Carpeta donde se guardan los archivos subidos. Debe estar FUERA de wwwroot para que nadie pueda
    /// descargarlos sin iniciar sesión. Si es relativa, se toma desde la carpeta de la aplicación.
    /// En Docker debe ser un volumen (por ejemplo /app/almacen) para que sobreviva a los reinicios.
    /// </summary>
    public string RutaBase { get; init; } = "";
}

/// <summary>
/// Guarda los archivos en el disco con nombres aleatorios: "comprobantes/2026/10/3f2a...c1.png".
/// El nombre original del usuario nunca se usa (podría traer rutas o caracteres peligrosos).
/// </summary>
internal sealed partial class AlmacenArchivosDisco(
    OpcionesArchivos opciones,
    IHostEnvironment entorno,
    IRelojSistema reloj,
    ILogger<AlmacenArchivosDisco> logger) : IAlmacenArchivos
{
    private const int TamanoBuffer = 81920;

    private string RutaBase { get; } = Path.GetFullPath(
        Path.IsPathRooted(opciones.RutaBase) ? opciones.RutaBase : Path.Combine(entorno.ContentRootPath, opciones.RutaBase));

    public async Task<string> GuardarAsync(string carpeta, string extension, ReadOnlyMemory<byte> contenido, CancellationToken cancellationToken)
    {
        if (!CarpetaValida().IsMatch(carpeta) || !ExtensionValida().IsMatch(extension))
        {
            throw new ArgumentException("Carpeta o extensión no permitida.");
        }

        var ahora = reloj.AhoraUtc;
        var archivoId = $"{carpeta}/{ahora:yyyy}/{ahora:MM}/{Guid.NewGuid():N}{extension}";
        var ruta = RutaSegura(archivoId);

        Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);

        // CreateNew: nunca sobrescribe un archivo existente.
        await using var archivo = new FileStream(ruta, FileMode.CreateNew, FileAccess.Write, FileShare.None, TamanoBuffer, useAsync: true);
        await archivo.WriteAsync(contenido, cancellationToken);

        return archivoId;
    }

    public Task<Stream?> AbrirAsync(string archivoId, CancellationToken cancellationToken)
    {
        if (!IdentificadorValido().IsMatch(archivoId))
        {
            logger.LogWarning("Se pidió un archivo con un identificador no válido");
            return Task.FromResult<Stream?>(null);
        }

        var ruta = RutaSegura(archivoId);
        if (!File.Exists(ruta))
        {
            logger.LogWarning("No se encontró el archivo {ArchivoId} en el almacén", archivoId);
            return Task.FromResult<Stream?>(null);
        }

        Stream contenido = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.Read, TamanoBuffer, useAsync: true);
        return Task.FromResult<Stream?>(contenido);
    }

    public Task EliminarAsync(string archivoId, CancellationToken cancellationToken)
    {
        if (IdentificadorValido().IsMatch(archivoId))
        {
            var ruta = RutaSegura(archivoId);
            if (File.Exists(ruta))
            {
                File.Delete(ruta);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Convierte el identificador en ruta y comprueba que quede DENTRO de la carpeta base
    /// (protección contra "../" para leer archivos del sistema).
    /// </summary>
    private string RutaSegura(string archivoId)
    {
        var ruta = Path.GetFullPath(Path.Combine(RutaBase, archivoId));
        if (!ruta.StartsWith(RutaBase + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Ruta de archivo fuera del almacén.");
        }

        return ruta;
    }

    [GeneratedRegex("^[a-z]{1,30}$")]
    private static partial Regex CarpetaValida();

    [GeneratedRegex(@"^\.[a-z0-9]{1,5}$")]
    private static partial Regex ExtensionValida();

    [GeneratedRegex(@"^[a-z]{1,30}/\d{4}/\d{2}/[0-9a-f]{32}\.[a-z0-9]{1,5}$")]
    private static partial Regex IdentificadorValido();
}
