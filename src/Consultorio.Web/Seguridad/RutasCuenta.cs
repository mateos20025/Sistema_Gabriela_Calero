namespace Consultorio.Web.Seguridad;

/// <summary>Rutas de las páginas de cuenta (deben coincidir con las directivas @page).</summary>
public static class RutasCuenta
{
    public const string IniciarSesion = "/cuenta/iniciar-sesion";
    public const string CerrarSesion = "/cuenta/cerrar-sesion";
    public const string CambiarPassword = "/cuenta/cambiar-password";
    public const string AccesoDenegado = "/cuenta/acceso-denegado";

    /// <summary>Página a la que se va después de iniciar sesión si no se indicó otra.</summary>
    public const string Panel = "/admin";

    /// <summary>
    /// Evita las "redirecciones abiertas": después del login solo se permite volver a rutas de este
    /// mismo sitio. Un enlace malicioso como ?ReturnUrl=https://sitio-falso.com terminaría en el panel.
    /// </summary>
    public static string DestinoSeguro(string? url) =>
        !string.IsNullOrEmpty(url)
        && url.StartsWith('/')
        && !url.StartsWith("//", StringComparison.Ordinal)
        && !url.StartsWith("/\\", StringComparison.Ordinal)
            ? url
            : Panel;
}
