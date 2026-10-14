using System.Threading.RateLimiting;
using Consultorio.Application.Abstracciones;
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
            // Máximo 10 envíos de formularios de cuenta por minuto desde la misma IP (frena ataques de fuerza bruta).
            opciones.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(contexto =>
                HttpMethods.IsPost(contexto.Request.Method) && contexto.Request.Path.StartsWithSegments("/cuenta")
                    ? RateLimitPartition.GetFixedWindowLimiter(
                        contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
                        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })
                    : RateLimitPartition.GetNoLimiter("sin-limite"));

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
