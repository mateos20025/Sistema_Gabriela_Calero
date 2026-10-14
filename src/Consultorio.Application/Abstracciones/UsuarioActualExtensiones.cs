namespace Consultorio.Application.Abstracciones;

/// <summary>
/// Comprobaciones de permisos que usan los casos de uso como segunda barrera (la primera es la página).
/// Si fallan es un error de programación o un intento de abuso, por eso lanzan excepción.
/// </summary>
public static class UsuarioActualExtensiones
{
    public static async Task<UsuarioSesion> ExigirSesionAsync(this IUsuarioActual usuarioActual) =>
        await usuarioActual.ObtenerAsync()
        ?? throw new UnauthorizedAccessException("Debes iniciar sesión para realizar esta acción.");

    public static async Task<UsuarioSesion> ExigirDoctoraAsync(this IUsuarioActual usuarioActual)
    {
        var sesion = await usuarioActual.ExigirSesionAsync();
        return sesion.EsDoctora
            ? sesion
            : throw new UnauthorizedAccessException("Solo la doctora puede realizar esta acción.");
    }
}
