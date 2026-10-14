using Consultorio.Application.Abstracciones;
using Consultorio.Application.Comun;
using Consultorio.Application.Usuarios;
using Consultorio.Domain.Usuarios;
using Consultorio.Infrastructure.Persistencia;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Consultorio.Infrastructure.Identidad;

/// <summary>
/// Gestión de cuentas del personal.
/// <para>
/// Las pantallas de Blazor Server viven mucho tiempo (un "circuito" por pestaña). Si este servicio
/// usara el mismo DbContext todo ese tiempo, acumularía datos viejos y fallaría con dos clics seguidos.
/// Por eso cada operación crea su propio "scope" y obtiene un DbContext y un UserManager nuevos.
/// </para>
/// <para>
/// Regla de seguridad: la doctora no puede desactivarse, cambiarse el rol ni restablecerse la
/// contraseña a sí misma. Eso garantiza que siempre quede al menos una doctora activa.
/// </para>
/// </summary>
internal sealed class ServicioUsuarios(
    IServiceScopeFactory fabricaScopes,
    IUsuarioActual usuarioActual,
    IValidator<CrearUsuarioComando> validadorCrear,
    IRelojSistema reloj,
    ILogger<ServicioUsuarios> logger) : IServicioUsuarios
{
    private const string UsuarioNoExiste = "El usuario no existe.";

    public async Task<IReadOnlyList<UsuarioResumen>> ListarAsync(CancellationToken cancellationToken)
    {
        await ExigirDoctoraAsync();

        await using var scope = fabricaScopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConsultorioDbContext>();

        var asignaciones = await (
                from usuarioRol in db.UserRoles
                join rol in db.Roles on usuarioRol.RoleId equals rol.Id
                select new { usuarioRol.UserId, Rol = rol.Name ?? "" })
            .ToListAsync(cancellationToken);

        var rolesPorUsuario = asignaciones
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(x => x.Rol)));

        var usuarios = await db.Users
            .AsNoTracking()
            .OrderBy(u => u.NombreCompleto)
            .ToListAsync(cancellationToken);

        var ahora = new DateTimeOffset(reloj.AhoraUtc);

        return usuarios
            .Select(u => new UsuarioResumen(
                u.Id,
                u.NombreCompleto,
                u.Email ?? "",
                rolesPorUsuario.GetValueOrDefault(u.Id, "Sin rol"),
                u.Activo,
                Bloqueado: u.LockoutEnd > ahora,
                u.DebeCambiarPassword,
                u.UltimoAccesoEn,
                u.CreadoEn))
            .ToList();
    }

    public async Task<Resultado<string>> CrearAsync(CrearUsuarioComando comando, CancellationToken cancellationToken)
    {
        var actor = await ExigirDoctoraAsync();

        var validacion = await validadorCrear.ValidateAsync(comando, cancellationToken);
        if (!validacion.IsValid)
        {
            return Resultado<string>.Falla(validacion.Errors.Select(e => e.ErrorMessage));
        }

        await using var scope = fabricaScopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConsultorioDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();

        var email = comando.Email.Trim();
        var usuario = new Usuario
        {
            Id = Guid.CreateVersion7(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            NombreCompleto = comando.NombreCompleto.Trim(),
            Activo = true,
            DebeCambiarPassword = true,
        };
        var passwordTemporal = GeneradorPasswordTemporal.Generar();

        // Crear la cuenta y asignar el rol son dos pasos: la transacción evita que quede
        // una cuenta sin rol si el segundo paso falla.
        await using var transaccion = await db.Database.BeginTransactionAsync(cancellationToken);

        var creacion = await userManager.CreateAsync(usuario, passwordTemporal);
        if (!creacion.Succeeded)
        {
            return Resultado<string>.Falla(Errores(creacion));
        }

        var asignacion = await userManager.AddToRoleAsync(usuario, comando.Rol);
        if (!asignacion.Succeeded)
        {
            return Resultado<string>.Falla(Errores(asignacion));
        }

        await transaccion.CommitAsync(cancellationToken);

        logger.LogInformation("El usuario {ActorId} creó la cuenta {UsuarioId} con rol {Rol}", actor.Id, usuario.Id, comando.Rol);
        return Resultado<string>.Ok(passwordTemporal);
    }

    public async Task<Resultado> CambiarRolAsync(Guid usuarioId, string nuevoRol, CancellationToken cancellationToken)
    {
        var actor = await ExigirDoctoraAsync();

        if (!Roles.EsValido(nuevoRol))
        {
            return Resultado.Falla("Elige un rol válido.");
        }

        if (usuarioId == actor.Id)
        {
            return Resultado.Falla("No puedes cambiar tu propio rol.");
        }

        await using var scope = fabricaScopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConsultorioDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();

        var usuario = await userManager.FindByIdAsync(usuarioId.ToString());
        if (usuario is null)
        {
            return Resultado.Falla(UsuarioNoExiste);
        }

        var rolesActuales = await userManager.GetRolesAsync(usuario);
        if (rolesActuales.Count == 1 && rolesActuales[0] == nuevoRol)
        {
            return Resultado.Ok();
        }

        await using var transaccion = await db.Database.BeginTransactionAsync(cancellationToken);

        var quitar = await userManager.RemoveFromRolesAsync(usuario, rolesActuales);
        if (!quitar.Succeeded)
        {
            return Resultado.Falla(Errores(quitar));
        }

        var agregar = await userManager.AddToRoleAsync(usuario, nuevoRol);
        if (!agregar.Succeeded)
        {
            return Resultado.Falla(Errores(agregar));
        }

        // Cambiar el sello de seguridad cierra sus sesiones abiertas: al volver a entrar ya tendrá los permisos nuevos.
        var sello = await userManager.UpdateSecurityStampAsync(usuario);
        if (!sello.Succeeded)
        {
            return Resultado.Falla(Errores(sello));
        }

        await transaccion.CommitAsync(cancellationToken);

        logger.LogInformation("El usuario {ActorId} cambió el rol de {UsuarioId} a {Rol}", actor.Id, usuario.Id, nuevoRol);
        return Resultado.Ok();
    }

    public async Task<Resultado<string>> RestablecerPasswordAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var actor = await ExigirDoctoraAsync();

        if (usuarioId == actor.Id)
        {
            return Resultado<string>.Falla("Para cambiar tu propia contraseña usa la opción «Cambiar contraseña».");
        }

        await using var scope = fabricaScopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConsultorioDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();

        var usuario = await userManager.FindByIdAsync(usuarioId.ToString());
        if (usuario is null)
        {
            return Resultado<string>.Falla(UsuarioNoExiste);
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(usuario);
        var passwordTemporal = GeneradorPasswordTemporal.Generar();
        usuario.DebeCambiarPassword = true;

        await using var transaccion = await db.Database.BeginTransactionAsync(cancellationToken);

        // ResetPasswordAsync también renueva el sello de seguridad (cierra sus sesiones abiertas).
        var cambio = await userManager.ResetPasswordAsync(usuario, token, passwordTemporal);
        if (!cambio.Succeeded)
        {
            return Resultado<string>.Falla(Errores(cambio));
        }

        await userManager.SetLockoutEndDateAsync(usuario, null);
        await userManager.ResetAccessFailedCountAsync(usuario);

        await transaccion.CommitAsync(cancellationToken);

        logger.LogInformation("El usuario {ActorId} restableció la contraseña de {UsuarioId}", actor.Id, usuario.Id);
        return Resultado<string>.Ok(passwordTemporal);
    }

    public async Task<Resultado> DesactivarAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var actor = await ExigirDoctoraAsync();

        if (usuarioId == actor.Id)
        {
            return Resultado.Falla("No puedes desactivar tu propia cuenta.");
        }

        await using var scope = fabricaScopes.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();

        var usuario = await userManager.FindByIdAsync(usuarioId.ToString());
        if (usuario is null)
        {
            return Resultado.Falla(UsuarioNoExiste);
        }

        if (!usuario.Activo)
        {
            return Resultado.Ok();
        }

        usuario.Activo = false;

        // Guarda el cambio y renueva el sello de seguridad: sus sesiones abiertas se cierran en menos de un minuto.
        var resultado = await userManager.UpdateSecurityStampAsync(usuario);
        if (!resultado.Succeeded)
        {
            return Resultado.Falla(Errores(resultado));
        }

        logger.LogInformation("El usuario {ActorId} desactivó la cuenta {UsuarioId}", actor.Id, usuario.Id);
        return Resultado.Ok();
    }

    public async Task<Resultado> ActivarAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var actor = await ExigirDoctoraAsync();

        await using var scope = fabricaScopes.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();

        var usuario = await userManager.FindByIdAsync(usuarioId.ToString());
        if (usuario is null)
        {
            return Resultado.Falla(UsuarioNoExiste);
        }

        if (usuario.Activo)
        {
            return Resultado.Ok();
        }

        usuario.Activo = true;
        var resultado = await userManager.UpdateAsync(usuario);
        if (!resultado.Succeeded)
        {
            return Resultado.Falla(Errores(resultado));
        }

        logger.LogInformation("El usuario {ActorId} activó la cuenta {UsuarioId}", actor.Id, usuario.Id);
        return Resultado.Ok();
    }

    public async Task<Resultado> DesbloquearAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var actor = await ExigirDoctoraAsync();

        await using var scope = fabricaScopes.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();

        var usuario = await userManager.FindByIdAsync(usuarioId.ToString());
        if (usuario is null)
        {
            return Resultado.Falla(UsuarioNoExiste);
        }

        var resultado = await userManager.SetLockoutEndDateAsync(usuario, null);
        if (!resultado.Succeeded)
        {
            return Resultado.Falla(Errores(resultado));
        }

        await userManager.ResetAccessFailedCountAsync(usuario);

        logger.LogInformation("El usuario {ActorId} desbloqueó la cuenta {UsuarioId}", actor.Id, usuario.Id);
        return Resultado.Ok();
    }

    /// <summary>
    /// Segunda barrera de seguridad: aunque la página ya exige el rol Doctora, el servicio también lo comprueba.
    /// </summary>
    private Task<UsuarioSesion> ExigirDoctoraAsync() => usuarioActual.ExigirDoctoraAsync();

    // Distinct: como el usuario ES el correo, Identity avisa dos veces del mismo duplicado.
    private static IEnumerable<string> Errores(IdentityResult resultado) =>
        resultado.Errors.Select(e => e.Description).Distinct();
}
