using System.Security.Claims;
using Consultorio.Application.Cuenta;
using Microsoft.AspNetCore.Identity;

namespace Consultorio.Infrastructure.Identidad;

internal sealed class ValidadorSesion(SignInManager<Usuario> signInManager) : IValidadorSesion
{
    public async Task<bool> EsValidaAsync(ClaimsPrincipal usuario, CancellationToken cancellationToken)
    {
        // Devuelve null si el usuario ya no existe o si su sello de seguridad cambió
        // (cambio de contraseña, de rol o desactivación).
        var encontrado = await signInManager.ValidateSecurityStampAsync(usuario);
        return encontrado is { Activo: true };
    }
}
