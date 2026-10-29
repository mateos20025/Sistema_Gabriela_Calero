using Consultorio.Domain.Pacientes;
using Microsoft.Extensions.Options;

namespace Consultorio.Web.Configuracion;

/// <summary>
/// Contenido de la página pública (sección "SitioPublico" del archivo Contenido/sitio-publico.json):
/// textos, servicios, consultorios, redes y fotos. Así se cambia el sitio sin tocar el código.
/// El nombre de la doctora sale de "Consultorio:Nombre" y el horario, de la configuración de la agenda.
/// </summary>
public sealed class OpcionesSitioPublico
{
    public const string Seccion = "SitioPublico";

    /// <summary>Frase corta sobre el título, por ejemplo "Tu salud es nuestra labor".</summary>
    public string? Lema { get; init; }

    public List<string> Especialidades { get; init; } = [];

    /// <summary>Párrafo de la portada (para la página y para los buscadores).</summary>
    public string Presentacion { get; init; } = "";

    public List<string> Biografia { get; init; } = [];

    /// <summary>Rutas dentro de wwwroot (por ejemplo "img/sitio/doctora-principal.jpg").</summary>
    public string FotoPrincipal { get; init; } = "";

    public string? FotoConsultorio { get; init; }

    public string? FotoAgenda { get; init; }

    /// <summary>Emblema cuadrado del logotipo (opcional). Sin él se muestra un ícono con los colores de la marca.</summary>
    public string? Logo { get; init; }

    /// <summary>Logotipo completo con el nombre (opcional), para la pantalla de inicio de sesión.</summary>
    public string? LogoCompleto { get; init; }

    public ContactoSitio Contacto { get; init; } = new();

    public List<PilarSitio> Pilares { get; init; } = [];

    public List<ServicioSitio> Servicios { get; init; } = [];

    public List<ConsultorioSitio> Consultorios { get; init; } = [];
}

public sealed class ContactoSitio
{
    /// <summary>Número de WhatsApp ecuatoriano (por ejemplo "096 005 7144").</summary>
    public string WhatsApp { get; init; } = "";

    /// <summary>Texto con el que se abre la conversación de WhatsApp.</summary>
    public string? MensajeWhatsApp { get; init; }

    public string? Instagram { get; init; }

    public string? Facebook { get; init; }

    public string? TikTok { get; init; }
}

/// <summary>Idea de marca destacada (por ejemplo "Previene", "Cuida", "Acompaña").</summary>
public sealed class PilarSitio
{
    public string Icono { get; init; } = "";

    public string Titulo { get; init; } = "";

    public string Descripcion { get; init; } = "";
}

public sealed class ServicioSitio
{
    public string Icono { get; init; } = "";

    public string Titulo { get; init; } = "";

    public string Descripcion { get; init; } = "";

    /// <summary>Lista opcional de lo que incluye (por ejemplo, un paquete).</summary>
    public List<string> Incluye { get; init; } = [];
}

public sealed class ConsultorioSitio
{
    public string Ciudad { get; init; } = "";

    /// <summary>Nombre del edificio o clínica.</summary>
    public string Lugar { get; init; } = "";

    public string? Direccion { get; init; }

    /// <summary>Enlace de Google Maps (opcional). Si falta, se arma una búsqueda con el lugar y la dirección.</summary>
    public string? UrlMapa { get; init; }
}

/// <summary>
/// Revisa el contenido al arrancar: si algo obligatorio falta o un enlace no es seguro, la app no inicia
/// y el mensaje dice qué corregir (mejor que publicar una página rota).
/// </summary>
public sealed class ValidadorOpcionesSitioPublico : IValidateOptions<OpcionesSitioPublico>
{
    private const int LongitudMaximaTexto = 600;

    public ValidateOptionsResult Validate(string? name, OpcionesSitioPublico opciones)
    {
        var errores = new List<string>();
        void Exigir(bool condicion, string mensaje)
        {
            if (!condicion)
            {
                errores.Add($"{OpcionesSitioPublico.Seccion}: {mensaje}");
            }
        }

        Exigir(opciones.Especialidades.Count > 0, "agrega al menos una especialidad.");
        Exigir(EsTextoValido(opciones.Presentacion), "falta la presentación (máximo 600 caracteres).");
        Exigir(opciones.Biografia.All(EsTextoValido), "cada párrafo de la biografía debe tener entre 1 y 600 caracteres.");
        Exigir(EsRutaLocal(opciones.FotoPrincipal), "FotoPrincipal debe ser una ruta dentro de wwwroot, por ejemplo img/sitio/foto.jpg.");
        Exigir(EsRutaLocalOpcional(opciones.FotoConsultorio), "FotoConsultorio debe ser una ruta dentro de wwwroot.");
        Exigir(EsRutaLocalOpcional(opciones.FotoAgenda), "FotoAgenda debe ser una ruta dentro de wwwroot.");
        Exigir(EsRutaLocalOpcional(opciones.Logo), "Logo debe ser una ruta dentro de wwwroot.");
        Exigir(EsRutaLocalOpcional(opciones.LogoCompleto), "LogoCompleto debe ser una ruta dentro de wwwroot.");

        Exigir(TelefonoEcuatoriano.Normalizar(opciones.Contacto.WhatsApp) is not null, "el número de WhatsApp no es un teléfono ecuatoriano válido.");
        Exigir(EsEnlaceSeguro(opciones.Contacto.Instagram), "Instagram debe ser un enlace https.");
        Exigir(EsEnlaceSeguro(opciones.Contacto.Facebook), "Facebook debe ser un enlace https.");
        Exigir(EsEnlaceSeguro(opciones.Contacto.TikTok), "TikTok debe ser un enlace https.");

        Exigir(opciones.Pilares.All(p => EsTextoValido(p.Titulo) && EsTextoValido(p.Descripcion)), "cada pilar necesita título y descripción.");
        Exigir(opciones.Servicios.Count > 0, "agrega al menos un servicio.");
        Exigir(opciones.Servicios.All(s => EsTextoValido(s.Titulo) && EsTextoValido(s.Descripcion)), "cada servicio necesita título y descripción.");
        Exigir(opciones.Consultorios.Count > 0, "agrega al menos un consultorio.");
        Exigir(opciones.Consultorios.All(c => EsTextoValido(c.Ciudad) && EsTextoValido(c.Lugar)), "cada consultorio necesita ciudad y lugar.");
        Exigir(opciones.Consultorios.All(c => EsEnlaceSeguro(c.UrlMapa)), "UrlMapa debe ser un enlace https.");

        return errores.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errores);
    }

    private static bool EsTextoValido(string? texto) => !string.IsNullOrWhiteSpace(texto) && texto.Length <= LongitudMaximaTexto;

    /// <summary>Solo rutas relativas dentro del sitio: nada de "..", ni direcciones de otros servidores.</summary>
    private static bool EsRutaLocal(string? ruta) =>
        !string.IsNullOrWhiteSpace(ruta)
        && !ruta.Contains("..", StringComparison.Ordinal)
        && !ruta.Contains(':', StringComparison.Ordinal)
        && !ruta.StartsWith('/')
        && !ruta.StartsWith('\\');

    private static bool EsRutaLocalOpcional(string? ruta) => string.IsNullOrWhiteSpace(ruta) || EsRutaLocal(ruta);

    private static bool EsEnlaceSeguro(string? url) =>
        string.IsNullOrWhiteSpace(url)
        || (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps);
}
