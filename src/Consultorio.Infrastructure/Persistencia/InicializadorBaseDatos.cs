using Consultorio.Domain.Usuarios;
using Consultorio.Infrastructure.Identidad;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Consultorio.Infrastructure.Persistencia;

/// <summary>Opciones de la sección "BaseDatos" de la configuración.</summary>
public sealed class OpcionesBaseDatos
{
    public const string Seccion = "BaseDatos";

    /// <summary>
    /// Aplica las migraciones pendientes al arrancar. Es seguro con UNA sola instancia de la app
    /// (nuestro caso: un VPS con Docker Compose).
    /// </summary>
    public bool AplicarMigracionesAlIniciar { get; set; } = true;
}

/// <summary>
/// Datos de la primera cuenta de la doctora (sección "UsuarioInicial").
/// Solo se usan si todavía no existe ninguna doctora. Nunca se guardan en el código ni en GitHub:
/// vienen de User Secrets (desarrollo) o de variables de entorno (producción).
/// </summary>
public sealed class OpcionesUsuarioInicial
{
    public const string Seccion = "UsuarioInicial";

    public string NombreCompleto { get; set; } = "";

    public string Email { get; set; } = "";

    public string Password { get; set; } = "";
}

public static class InicializadorBaseDatos
{
    /// <summary>
    /// Prepara la base de datos al arrancar: migraciones, roles y cuenta inicial de la doctora.
    /// Es idempotente: ejecutarlo varias veces no duplica nada.
    /// </summary>
    public static async Task InicializarBaseDatosAsync(this IServiceProvider servicios, CancellationToken cancellationToken = default)
    {
        await using var scope = servicios.CreateAsyncScope();
        var proveedor = scope.ServiceProvider;
        var logger = proveedor.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(InicializadorBaseDatos));

        var opcionesBaseDatos = proveedor.GetRequiredService<IOptions<OpcionesBaseDatos>>().Value;
        if (opcionesBaseDatos.AplicarMigracionesAlIniciar)
        {
            await AplicarMigracionesAsync(proveedor.GetRequiredService<ConsultorioDbContext>(), logger, cancellationToken);
        }

        await CrearRolesAsync(proveedor.GetRequiredService<RoleManager<IdentityRole<Guid>>>(), logger);

        await CrearDoctoraInicialAsync(
            proveedor.GetRequiredService<UserManager<Usuario>>(),
            proveedor.GetRequiredService<IOptions<OpcionesUsuarioInicial>>().Value,
            logger);
    }

    private static async Task AplicarMigracionesAsync(ConsultorioDbContext db, ILogger logger, CancellationToken cancellationToken)
    {
        var pendientes = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pendientes.Count == 0)
        {
            return;
        }

        logger.LogInformation("Aplicando {Cantidad} migraciones pendientes: {Migraciones}", pendientes.Count, pendientes);
        await db.Database.MigrateAsync(cancellationToken);
    }

    private static async Task CrearRolesAsync(RoleManager<IdentityRole<Guid>> roleManager, ILogger logger)
    {
        foreach (var rol in Roles.Todos)
        {
            if (await roleManager.RoleExistsAsync(rol))
            {
                continue;
            }

            var resultado = await roleManager.CreateAsync(new IdentityRole<Guid>(rol) { Id = Guid.CreateVersion7() });
            if (!resultado.Succeeded)
            {
                throw new InvalidOperationException(
                    $"No se pudo crear el rol {rol}: {string.Join(" ", resultado.Errors.Select(e => e.Description))}");
            }

            logger.LogInformation("Rol {Rol} creado", rol);
        }
    }

    private static async Task CrearDoctoraInicialAsync(UserManager<Usuario> userManager, OpcionesUsuarioInicial opciones, ILogger logger)
    {
        if ((await userManager.GetUsersInRoleAsync(Roles.Doctora)).Count > 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(opciones.Email)
            || string.IsNullOrWhiteSpace(opciones.Password)
            || string.IsNullOrWhiteSpace(opciones.NombreCompleto))
        {
            logger.LogWarning(
                "No existe ninguna cuenta con rol Doctora y la sección '{Seccion}' no está completa. " +
                "Configura UsuarioInicial:NombreCompleto, UsuarioInicial:Email y UsuarioInicial:Password para crearla.",
                OpcionesUsuarioInicial.Seccion);
            return;
        }

        var email = opciones.Email.Trim();
        var doctora = new Usuario
        {
            Id = Guid.CreateVersion7(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            NombreCompleto = opciones.NombreCompleto.Trim(),
            Activo = true,
            // La contraseña inicial está escrita en un archivo de configuración: se obliga a cambiarla.
            DebeCambiarPassword = true,
        };

        var creacion = await userManager.CreateAsync(doctora, opciones.Password);
        if (!creacion.Succeeded)
        {
            // Mejor no arrancar que arrancar sin nadie que pueda entrar al panel.
            throw new InvalidOperationException(
                $"No se pudo crear la cuenta inicial de la doctora: {string.Join(" ", creacion.Errors.Select(e => e.Description))}");
        }

        await userManager.AddToRoleAsync(doctora, Roles.Doctora);
        logger.LogInformation(
            "Cuenta inicial de la doctora creada ({UsuarioId}). Deberá cambiar la contraseña en su primer inicio de sesión.",
            doctora.Id);
    }
}
