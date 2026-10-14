namespace Consultorio.Application.Cuenta;

/// <summary>Datos del formulario para que el usuario con sesión cambie su propia contraseña.</summary>
public sealed class CambiarPasswordComando
{
    public string PasswordActual { get; set; } = "";

    public string NuevaPassword { get; set; } = "";

    public string ConfirmarPassword { get; set; } = "";
}
