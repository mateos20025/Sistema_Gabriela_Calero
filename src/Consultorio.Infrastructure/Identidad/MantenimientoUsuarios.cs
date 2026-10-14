using Consultorio.Application.Usuarios;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Consultorio.Infrastructure.Identidad;

/// <summary>
/// Tareas de emergencia que se ejecutan desde la consola del servidor (no desde la web).
/// Solo puede usarlas quien tenga acceso al servidor.
/// </summary>
public static class MantenimientoUsuarios
{
    /// <summary>
    /// Para cuando nadie puede entrar al panel (por ejemplo, la doctora olvidó su contraseña).
    /// Genera una contraseña temporal, reactiva y desbloquea la cuenta, y obliga a cambiarla al entrar.
    /// </summary>
    public static async Task<string> RestablecerPasswordDesdeConsolaAsync(this IServiceProvider servicios, string email)
    {
        await using var scope = servicios.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(MantenimientoUsuarios));

        var usuario = await userManager.FindByEmailAsync(email.Trim())
            ?? throw new InvalidOperationException($"No existe ningún usuario con el correo {email}.");

        var token = await userManager.GeneratePasswordResetTokenAsync(usuario);
        var passwordTemporal = GeneradorPasswordTemporal.Generar();
        usuario.Activo = true;
        usuario.DebeCambiarPassword = true;

        var cambio = await userManager.ResetPasswordAsync(usuario, token, passwordTemporal);
        if (!cambio.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", cambio.Errors.Select(e => e.Description)));
        }

        await userManager.SetLockoutEndDateAsync(usuario, null);
        await userManager.ResetAccessFailedCountAsync(usuario);

        logger.LogWarning("Contraseña del usuario {UsuarioId} restablecida desde la consola del servidor", usuario.Id);
        return passwordTemporal;
    }
}
