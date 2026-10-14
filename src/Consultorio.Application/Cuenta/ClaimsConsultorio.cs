namespace Consultorio.Application.Cuenta;

/// <summary>
/// Nombres de los "claims" propios que viajan en la cookie de sesión, además de los de Identity (id, correo, roles).
/// </summary>
public static class ClaimsConsultorio
{
    public const string NombreCompleto = "consultorio:nombre_completo";

    /// <summary>
    /// Presente cuando el usuario entra con una contraseña temporal.
    /// Mientras exista, el panel /admin queda bloqueado hasta que cambie la contraseña.
    /// </summary>
    public const string DebeCambiarPassword = "consultorio:debe_cambiar_password";
}
