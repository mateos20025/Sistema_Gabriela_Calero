namespace Consultorio.Application.Abstracciones;

/// <summary>
/// Quién está usando el sistema en este momento. Lo implementa la capa Web a partir de la sesión.
/// Los casos de uso lo consultan en vez de confiar en un "id de usuario" enviado desde la pantalla.
/// </summary>
public interface IUsuarioActual
{
    /// <summary>Devuelve el usuario con sesión iniciada, o <c>null</c> si nadie ha iniciado sesión.</summary>
    Task<UsuarioSesion?> ObtenerAsync();
}

public sealed record UsuarioSesion(Guid Id, string NombreCompleto, bool EsDoctora);
