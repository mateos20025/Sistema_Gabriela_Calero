using Consultorio.Application.Comun;

namespace Consultorio.Application.Cuenta;

/// <summary>
/// Inicio de sesión y gestión de la propia cuenta. Lo implementa Infrastructure con ASP.NET Core Identity.
/// Necesita una petición HTTP real (escribe la cookie de sesión), por eso solo se usa desde páginas
/// estáticas (sin interactividad) o endpoints.
/// </summary>
public interface IServicioCuenta
{
    Task<ResultadoInicioSesion> IniciarSesionAsync(IniciarSesionComando comando, CancellationToken cancellationToken);

    Task CerrarSesionAsync();

    Task<Resultado> CambiarPasswordAsync(CambiarPasswordComando comando, CancellationToken cancellationToken);
}

public enum ResultadoInicioSesion
{
    Exitoso,

    /// <summary>Entró, pero con contraseña temporal: debe cambiarla antes de usar el panel.</summary>
    DebeCambiarPassword,

    /// <summary>Demasiados intentos fallidos: la cuenta queda bloqueada unos minutos.</summary>
    Bloqueado,

    /// <summary>Correo o contraseña incorrectos, o usuario desactivado (no se distingue a propósito).</summary>
    CredencialesInvalidas,
}
