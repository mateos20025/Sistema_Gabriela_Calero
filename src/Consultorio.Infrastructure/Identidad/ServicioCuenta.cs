using Consultorio.Application.Abstracciones;
using Consultorio.Application.Comun;
using Consultorio.Application.Cuenta;
using Consultorio.Infrastructure.Persistencia;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Consultorio.Infrastructure.Identidad;

internal sealed class ServicioCuenta(
    SignInManager<Usuario> signInManager,
    UserManager<Usuario> userManager,
    ConsultorioDbContext db,
    IUsuarioActual usuarioActual,
    IValidator<CambiarPasswordComando> validadorCambioPassword,
    IRelojSistema reloj,
    ILogger<ServicioCuenta> logger) : IServicioCuenta
{
    public async Task<ResultadoInicioSesion> IniciarSesionAsync(IniciarSesionComando comando, CancellationToken cancellationToken)
    {
        var email = comando.Email.Trim();

        // isPersistent: false => la cookie no sobrevive al cierre del navegador (equipos compartidos).
        // lockoutOnFailure: true => los intentos fallidos cuentan para el bloqueo temporal.
        var resultado = await signInManager.PasswordSignInAsync(email, comando.Password, isPersistent: false, lockoutOnFailure: true);

        if (resultado.IsLockedOut)
        {
            logger.LogWarning("Inicio de sesión rechazado: la cuenta {Email} está bloqueada por intentos fallidos", Enmascarar(email));
            return ResultadoInicioSesion.Bloqueado;
        }

        if (!resultado.Succeeded)
        {
            logger.LogWarning("Intento de inicio de sesión fallido para {Email}", Enmascarar(email));
            return ResultadoInicioSesion.CredencialesInvalidas;
        }

        var usuario = await userManager.FindByNameAsync(email)
            ?? throw new InvalidOperationException("El usuario inició sesión pero no se encontró en la base de datos.");

        var ahora = reloj.AhoraUtc;
        await db.Users
            .Where(u => u.Id == usuario.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.UltimoAccesoEn, ahora), cancellationToken);

        logger.LogInformation("Inicio de sesión exitoso del usuario {UsuarioId}", usuario.Id);

        return usuario.DebeCambiarPassword
            ? ResultadoInicioSesion.DebeCambiarPassword
            : ResultadoInicioSesion.Exitoso;
    }

    public async Task CerrarSesionAsync()
    {
        var usuarioId = userManager.GetUserId(signInManager.Context.User);
        await signInManager.SignOutAsync();
        logger.LogInformation("El usuario {UsuarioId} cerró sesión", usuarioId);
    }

    public async Task<Resultado> CambiarPasswordAsync(CambiarPasswordComando comando, CancellationToken cancellationToken)
    {
        var validacion = await validadorCambioPassword.ValidateAsync(comando, cancellationToken);
        if (!validacion.IsValid)
        {
            return Resultado.Falla(validacion.Errors.Select(e => e.ErrorMessage));
        }

        var actual = await usuarioActual.ObtenerAsync()
            ?? throw new UnauthorizedAccessException("Debes iniciar sesión para cambiar la contraseña.");

        var usuario = await userManager.FindByIdAsync(actual.Id.ToString());
        if (usuario is null)
        {
            return Resultado.Falla("Tu cuenta ya no existe.");
        }

        // ChangePasswordAsync verifica la contraseña actual, aplica la política y renueva el sello de
        // seguridad: así se cierran las sesiones abiertas en otros equipos.
        var cambio = await userManager.ChangePasswordAsync(usuario, comando.PasswordActual, comando.NuevaPassword);
        if (!cambio.Succeeded)
        {
            return Resultado.Falla(cambio.Errors.Select(e => e.Description).Distinct());
        }

        if (usuario.DebeCambiarPassword)
        {
            usuario.DebeCambiarPassword = false;
            await userManager.UpdateAsync(usuario);
        }

        // Vuelve a emitir la cookie de ESTE navegador con el sello nuevo (si no, también se cerraría aquí).
        await signInManager.RefreshSignInAsync(usuario);

        logger.LogInformation("El usuario {UsuarioId} cambió su contraseña", usuario.Id);
        return Resultado.Ok();
    }

    /// <summary>"gabriela@correo.com" => "ga***@correo.com". Suficiente para investigar sin exponer datos.</summary>
    private static string Enmascarar(string email)
    {
        var arroba = email.IndexOf('@');
        return arroba <= 0
            ? "***"
            : $"{email[..Math.Min(2, arroba)]}***{email[arroba..]}";
    }
}
