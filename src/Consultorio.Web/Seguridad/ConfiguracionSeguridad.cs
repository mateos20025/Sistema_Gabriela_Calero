using System.Threading.RateLimiting;
using Consultorio.Application.Abstracciones;
using Consultorio.Web.Navegacion;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

namespace Consultorio.Web.Seguridad;

public static class ConfiguracionSeguridad
{
    /// <summary>Cookies de sesión, autorización, límite de intentos y protección de datos.</summary>
    public static IServiceCollection AddSeguridadWeb(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCascadingAuthenticationState();
        services.AddScoped<AuthenticationStateProvider, ProveedorEstadoAutenticacion>();
        services.AddScoped<IUsuarioActual, UsuarioActual>();

        services.AddAuthentication(opciones =>
            {
                opciones.DefaultScheme = IdentityConstants.ApplicationScheme;
                opciones.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddIdentityCookies();

        // Debe ir DESPUÉS de AddIdentityCookies para modificar su configuración, no reemplazarla.
        services.ConfigureApplicationCookie(opciones =>
        {
            opciones.Cookie.Name = "consultorio.sesion";
            opciones.Cookie.HttpOnly = true;                          // JavaScript no puede leerla.
            opciones.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Solo viaja por HTTPS.
            opciones.Cookie.SameSite = SameSiteMode.Lax;              // Otros sitios no pueden usarla en un POST.
            opciones.ExpireTimeSpan = TimeSpan.FromHours(8);          // Una jornada de trabajo...
            opciones.SlidingExpiration = true;                        // ...que se renueva mientras se use.
            opciones.LoginPath = RutasCuenta.IniciarSesion;
            opciones.AccessDeniedPath = RutasCuenta.AccesoDenegado;

            // Con contraseña temporal no se muestra "acceso denegado": se lleva directo a cambiarla.
            opciones.Events.OnRedirectToAccessDenied = contexto =>
            {
                contexto.Response.Redirect(Politicas.DebeCambiarPassword(contexto.HttpContext.User)
                    ? RutasCuenta.CambiarPassword
                    : contexto.RedirectUri);
                return Task.CompletedTask;
            };
        });

        services.AddAuthorizationBuilder().AgregarPoliticasConsultorio();

        services.AddAntiforgery(opciones =>
        {
            opciones.Cookie.Name = "consultorio.antiforgery";
            opciones.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });

        // Claves que cifran las cookies. Si no se guardan en un lugar persistente, cada vez que se
        // reinicie el contenedor Docker se cerrarán todas las sesiones.
        var proteccionDatos = services.AddDataProtection().SetApplicationName("Consultorio");
        var rutaClaves = configuration["DataProtection:RutaClaves"];
        if (!string.IsNullOrWhiteSpace(rutaClaves))
        {
            proteccionDatos.PersistKeysToFileSystem(new DirectoryInfo(rutaClaves));
        }

        services.AddRateLimiter(opciones =>
        {
            opciones.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ParticionLimite);

            opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opciones.OnRejected = async (contexto, cancellationToken) =>
            {
                contexto.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
                await contexto.HttpContext.Response.WriteAsync(
                    "Demasiados intentos. Espera un minuto y vuelve a intentarlo.", cancellationToken);
            };
        });

        services.AddHsts(opciones => opciones.MaxAge = TimeSpan.FromDays(365));

        return services;
    }

    /// <summary>
    /// Límites por IP:
    /// <list type="bullet">
    /// <item>Formularios de cuenta: 10 envíos por minuto (frena ataques de fuerza bruta).</item>
    /// <item>Solicitar cita: 5 envíos cada 10 minutos (frena el acaparamiento de horarios).</item>
    /// <item>Consultar horarios públicos: 60 por minuto (cada consulta calcula la disponibilidad).</item>
    /// </list>
    /// </summary>
    private static RateLimitPartition<string> ParticionLimite(HttpContext contexto)
    {
        var ip = contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida";
        var ruta = contexto.Request.Path;
        var esEnvio = HttpMethods.IsPost(contexto.Request.Method);

        if (esEnvio && ruta.StartsWithSegments("/cuenta"))
        {
            return Ventana($"cuenta:{ip}", 10, TimeSpan.FromMinutes(1));
        }

        if (ruta.StartsWithSegments(RutasPublicas.SolicitarCita))
        {
            return esEnvio
                ? Ventana($"solicitar-cita:{ip}", 5, TimeSpan.FromMinutes(10))
                : Ventana($"consultar-horarios:{ip}", 60, TimeSpan.FromMinutes(1));
        }

        return RateLimitPartition.GetNoLimiter("sin-limite");

        static RateLimitPartition<string> Ventana(string clave, int permitidos, TimeSpan ventana) =>
            RateLimitPartition.GetFixedWindowLimiter(
                clave, _ => new FixedWindowRateLimiterOptions { PermitLimit = permitidos, Window = ventana, QueueLimit = 0 });
    }

    /// <summary>Cabeceras HTTP que endurecen el navegador contra ataques comunes.</summary>
    public static IApplicationBuilder UseCabecerasDeSeguridad(this IApplicationBuilder app) =>
        app.Use(async (contexto, siguiente) =>
        {
            var cabeceras = contexto.Response.Headers;
            cabeceras.XContentTypeOptions = "nosniff";                // No adivinar tipos de archivo.
            cabeceras.XFrameOptions = "DENY";                         // Nadie puede incrustar el sitio en un iframe.
            cabeceras["Referrer-Policy"] = "strict-origin-when-cross-origin";
            cabeceras["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
            cabeceras.ContentSecurityPolicy = "frame-ancestors 'none'; base-uri 'self'; form-action 'self'; object-src 'none'";
            await siguiente(contexto);
        });
}
