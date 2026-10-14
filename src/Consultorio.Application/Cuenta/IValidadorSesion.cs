using System.Security.Claims;

namespace Consultorio.Application.Cuenta;

/// <summary>
/// Comprueba si una sesión abierta sigue siendo válida: el usuario existe, está activo y no cambió
/// su contraseña ni su rol desde que inició sesión. Se usa para expulsar sesiones de usuarios desactivados.
/// </summary>
public interface IValidadorSesion
{
    Task<bool> EsValidaAsync(ClaimsPrincipal usuario, CancellationToken cancellationToken);
}
