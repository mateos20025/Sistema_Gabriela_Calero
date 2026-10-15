using Microsoft.Extensions.Options;

namespace Consultorio.Web.Configuracion;

/// <summary>Ubicación del texto de la política de privacidad (sección "ProteccionDatos").</summary>
public sealed class OpcionesPoliticaPrivacidad
{
    public const string Seccion = "ProteccionDatos";

    /// <summary>
    /// Archivo de texto con la política. Puede ser relativo a la carpeta de la aplicación.
    /// En producción se monta un archivo propio con el texto que aprobó la doctora.
    /// </summary>
    public string? RutaPoliticaPrivacidad { get; init; }
}

/// <summary>Un bloque del texto de la política: título, párrafo o elemento de lista.</summary>
public sealed record BloquePolitica(TipoBloquePolitica Tipo, string Texto);

public enum TipoBloquePolitica
{
    Titulo,
    Parrafo,
    ElementoLista,
}

/// <summary>
/// Lee el texto de la política de privacidad. Formato simple: "## " para títulos, "- " para listas y
/// párrafos separados por una línea en blanco. Se muestra como texto (nunca como HTML), así que no
/// puede inyectar código en la página.
/// <para>
/// Sin este texto no se reciben solicitudes de cita en línea: la LOPDP exige informar al titular
/// antes de que entregue sus datos.
/// </para>
/// </summary>
public sealed class PoliticaPrivacidad(
    IOptions<OpcionesPoliticaPrivacidad> opciones, IWebHostEnvironment entorno, ILogger<PoliticaPrivacidad> logger)
{
    /// <summary>Tope de tamaño del archivo, para no cargar en memoria algo que no es un texto legal.</summary>
    private const long TamanoMaximoBytes = 256 * 1024;

    /// <returns><c>null</c> si no está configurada o no se puede leer.</returns>
    public async Task<IReadOnlyList<BloquePolitica>?> LeerAsync(CancellationToken cancellationToken)
    {
        var ruta = RutaCompleta();
        if (ruta is null)
        {
            return null;
        }

        try
        {
            var archivo = new FileInfo(ruta);
            if (!archivo.Exists || archivo.Length is 0 or > TamanoMaximoBytes)
            {
                logger.LogWarning("La política de privacidad no existe, está vacía o es demasiado grande; no se reciben solicitudes en línea.");
                return null;
            }

            var texto = await File.ReadAllTextAsync(ruta, cancellationToken);
            return Interpretar(texto);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "No se pudo leer el archivo de la política de privacidad.");
            return null;
        }
    }

    public async Task<bool> EstaDisponibleAsync(CancellationToken cancellationToken) =>
        await LeerAsync(cancellationToken) is { Count: > 0 };

    private string? RutaCompleta()
    {
        var ruta = opciones.Value.RutaPoliticaPrivacidad?.Trim();
        return string.IsNullOrEmpty(ruta) ? null : Path.GetFullPath(ruta, entorno.ContentRootPath);
    }

    private static List<BloquePolitica> Interpretar(string texto)
    {
        var bloques = new List<BloquePolitica>();
        var parrafo = new List<string>();

        void CerrarParrafo()
        {
            if (parrafo.Count > 0)
            {
                bloques.Add(new BloquePolitica(TipoBloquePolitica.Parrafo, string.Join(" ", parrafo)));
                parrafo.Clear();
            }
        }

        foreach (var linea in texto.ReplaceLineEndings("\n").Split('\n').Select(l => l.Trim()))
        {
            if (linea.Length == 0)
            {
                CerrarParrafo();
            }
            else if (linea.StartsWith('#'))
            {
                CerrarParrafo();
                bloques.Add(new BloquePolitica(TipoBloquePolitica.Titulo, linea.TrimStart('#').Trim()));
            }
            else if (linea.StartsWith("- ", StringComparison.Ordinal))
            {
                CerrarParrafo();
                bloques.Add(new BloquePolitica(TipoBloquePolitica.ElementoLista, linea[2..].Trim()));
            }
            else
            {
                parrafo.Add(linea);
            }
        }

        CerrarParrafo();
        return bloques;
    }
}
