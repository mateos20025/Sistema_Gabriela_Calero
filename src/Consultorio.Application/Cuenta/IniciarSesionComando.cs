namespace Consultorio.Application.Cuenta;

/// <summary>Datos del formulario de inicio de sesión.</summary>
public sealed class IniciarSesionComando
{
    public string Email { get; set; } = "";

    public string Password { get; set; } = "";
}
