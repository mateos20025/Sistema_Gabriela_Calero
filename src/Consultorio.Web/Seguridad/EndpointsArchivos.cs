using Consultorio.Application.Pagos;
using Consultorio.Web.Navegacion;
using Microsoft.AspNetCore.Components.Authorization;

namespace Consultorio.Web.Seguridad;

public static class EndpointsArchivos
{
    /// <summary>
    /// Entrega la captura de una transferencia. Exige sesión del personal (sin sesión redirige al login),
    /// no se guarda en la caché del navegador y el tipo de contenido sale de lo que detectó el sistema,
    /// nunca de lo que dijo el usuario al subirla.
    /// </summary>
    public static IEndpointRouteBuilder MapEndpointsArchivos(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(RutasPanel.PatronComprobante, async (
                Guid pagoId,
                HttpContext contexto,
                AuthenticationStateProvider proveedorAutenticacion,
                ObtenerComprobantePago casoDeUso,
                CancellationToken cancellationToken) =>
            {
                // Los casos de uso leen el usuario desde AuthenticationStateProvider. Dentro de Blazor eso es
                // automático; en un endpoint normal hay que entregarle el usuario de esta petición.
                if (proveedorAutenticacion is IHostEnvironmentAuthenticationStateProvider anfitrion)
                {
                    anfitrion.SetAuthenticationState(Task.FromResult(new AuthenticationState(contexto.User)));
                }

                var archivo = await casoDeUso.EjecutarAsync(pagoId, cancellationToken);
                if (archivo is null)
                {
                    return Results.NotFound();
                }

                contexto.Response.Headers.CacheControl = "no-store, private";
                return Results.Stream(archivo.Contenido, archivo.TipoContenido);
            })
            .RequireAuthorization(Politicas.Personal);

        return endpoints;
    }
}
