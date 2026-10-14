using Consultorio.Application.Abstracciones;
using Consultorio.Application.Cuenta;
using Consultorio.Application.Usuarios;
using Consultorio.Infrastructure.Comun;
using Consultorio.Infrastructure.Identidad;
using Consultorio.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Consultorio.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registra base de datos, Identity e implementaciones de los puertos de Application.
    /// La capa Web solo llama a este método; nunca usa estas clases directamente.
    /// </summary>
    public static IServiceCollection AddInfraestructura(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IRelojSistema, RelojSistema>();
        services.AddSingleton<MarcasDeTiempoInterceptor>();

        services.AddDbContext<ConsultorioDbContext>((proveedor, opciones) =>
        {
            var cadenaConexion = configuration.GetConnectionString("Default");
            if (string.IsNullOrWhiteSpace(cadenaConexion))
            {
                throw new InvalidOperationException(
                    "Falta la cadena de conexión 'ConnectionStrings:Default'. En desarrollo configúrala con User Secrets " +
                    "y en producción con la variable de entorno ConnectionStrings__Default.");
            }

            opciones
                .UseNpgsql(cadenaConexion)
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(proveedor.GetRequiredService<MarcasDeTiempoInterceptor>());
        });

        // El orden importa: AddRoles antes de AddClaimsPrincipalFactory, o la fábrica propia se reemplaza.
        services.AddIdentityCore<Usuario>(OpcionesIdentidad.Configurar)
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ConsultorioDbContext>()
            .AddSignInManager<SignInManagerConsultorio>()
            .AddClaimsPrincipalFactory<FabricaClaimsUsuario>()
            .AddErrorDescriber<MensajesIdentidadEspanol>()
            .AddDefaultTokenProviders();

        // Cada minuto la cookie de sesión se contrasta con la base de datos: si el usuario fue desactivado
        // o cambió su contraseña/rol, la sesión deja de valer.
        services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(1));

        services.AddScoped<IServicioCuenta, ServicioCuenta>();
        services.AddScoped<IValidadorSesion, ValidadorSesion>();
        services.AddScoped<IServicioUsuarios, ServicioUsuarios>();

        services.Configure<OpcionesBaseDatos>(configuration.GetSection(OpcionesBaseDatos.Seccion));
        services.Configure<OpcionesUsuarioInicial>(configuration.GetSection(OpcionesUsuarioInicial.Seccion));

        return services;
    }
}
