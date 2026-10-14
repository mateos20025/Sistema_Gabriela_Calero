using Consultorio.Domain.Usuarios;

namespace Consultorio.Application.Usuarios;

/// <summary>Datos para crear una cuenta del personal. La contraseña la genera el sistema.</summary>
public sealed class CrearUsuarioComando
{
    public string NombreCompleto { get; set; } = "";

    public string Email { get; set; } = "";

    public string Rol { get; set; } = Roles.Asistente;
}
