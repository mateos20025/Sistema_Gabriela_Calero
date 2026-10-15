using Consultorio.Application.Abstracciones;
using Consultorio.Application.Auditoria;
using Consultorio.Application.Comun;
using Consultorio.Application.Cuenta;
using Consultorio.Application.Pacientes;
using Consultorio.Application.Pagos;
using Consultorio.Application.Usuarios;
using Consultorio.Domain.Pacientes;
using Consultorio.Infrastructure.Archivos;
using Consultorio.Infrastructure.Auditoria;
using Consultorio.Infrastructure.Comun;
using Consultorio.Infrastructure.Identidad;
using Consultorio.Infrastructure.Pacientes;
using Consultorio.Infrastructure.Pagos;
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
        services.AddSingleton<AuditoriaInterceptor>();
        services.AddSingleton<MarcasDeTiempoInterceptor>();
        services.AddSingleton(LeerOpcionesProteccionDatos(configuration));

        // Fábrica de contextos: los repositorios crean un DbContext por operación (recomendado en Blazor Server).
        // También registra ConsultorioDbContext como "scoped" para Identity.
        services.AddDbContextFactory<ConsultorioDbContext>((proveedor, opciones) =>
        {
            var cadenaConexion = configuration.GetConnectionString("Default");
            if (string.IsNullOrWhiteSpace(cadenaConexion))
            {
                throw new InvalidOperationException(
                    "Falta la cadena de conexión 'ConnectionStrings:Default'. En desarrollo configúrala con User Secrets " +
                    "y en producción con la variable de entorno ConnectionStrings__Default.");
            }

            // Orden: primero la auditoría (agrega filas) y después las marcas de tiempo (que también les ponen fecha).
            opciones
                .UseNpgsql(cadenaConexion)
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(
                    proveedor.GetRequiredService<AuditoriaInterceptor>(),
                    proveedor.GetRequiredService<MarcasDeTiempoInterceptor>());
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

        services.AddScoped<FabricaContextos>();
        services.AddScoped<IRepositorioPacientes, RepositorioPacientes>();
        services.AddScoped<IConsultaAuditoria, ConsultaAuditoria>();
        services.AddScoped<IRepositorioPagos, RepositorioPagos>();
        services.AddScoped<IRepositorioConfiguracionPagos, RepositorioConfiguracionPagos>();

        services.AddSingleton(LeerOpcionesArchivos(configuration));
        services.AddSingleton<IAlmacenArchivos, AlmacenArchivosDisco>();

        services.Configure<OpcionesBaseDatos>(configuration.GetSection(OpcionesBaseDatos.Seccion));
        services.Configure<OpcionesUsuarioInicial>(configuration.GetSection(OpcionesUsuarioInicial.Seccion));

        return services;
    }

    /// <summary>Lee y valida la sección "ProteccionDatos". Si falta, la app no arranca (mejor que guardar consentimientos sin versión).</summary>
    private static OpcionesProteccionDatos LeerOpcionesProteccionDatos(IConfiguration configuration)
    {
        var opciones = configuration.GetSection(OpcionesProteccionDatos.Seccion).Get<OpcionesProteccionDatos>();
        var version = opciones?.VersionPoliticaPrivacidad?.Trim();

        if (string.IsNullOrEmpty(version) || version.Length > ConsentimientoDatos.LongitudMaximaVersion)
        {
            throw new InvalidOperationException(
                $"Configura '{OpcionesProteccionDatos.Seccion}:VersionPoliticaPrivacidad' (máximo " +
                $"{ConsentimientoDatos.LongitudMaximaVersion} caracteres) en appsettings.json o en variables de entorno.");
        }

        return new OpcionesProteccionDatos { VersionPoliticaPrivacidad = version };
    }

    /// <summary>Lee la sección "Archivos". Sin ella no se sabría dónde guardar las capturas de pago.</summary>
    private static OpcionesArchivos LeerOpcionesArchivos(IConfiguration configuration)
    {
        var ruta = configuration.GetSection(OpcionesArchivos.Seccion).Get<OpcionesArchivos>()?.RutaBase?.Trim();
        if (string.IsNullOrEmpty(ruta))
        {
            throw new InvalidOperationException(
                $"Configura '{OpcionesArchivos.Seccion}:RutaBase' (carpeta fuera de wwwroot donde se guardan los archivos subidos).");
        }

        return new OpcionesArchivos { RutaBase = ruta };
    }
}
