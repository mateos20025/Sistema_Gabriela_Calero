using Consultorio.Application.Cuenta;
using Microsoft.AspNetCore.Antiforgery;

namespace Consultorio.Web.Seguridad;

public static class EndpointsCuenta
{
    /// <summary>
    /// Cerrar sesión es un POST (no un enlace GET) con token antifalsificación: así otra página web
    /// no puede cerrar la sesión de la doctora con solo incrustar un enlace.
    /// </summary>
    public static IEndpointRouteBuilder MapEndpointsCuenta(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(RutasCuenta.CerrarSesion, async (HttpContext contexto, IAntiforgery antiforgery, IServicioCuenta servicioCuenta) =>
        {
            if (!await antiforgery.IsRequestValidAsync(contexto))
            {
                return Results.Text(
                    "La solicitud no es válida. Recarga la página e inténtalo de nuevo.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            await servicioCuenta.CerrarSesionAsync();
            return Results.LocalRedirect("~" + RutasCuenta.IniciarSesion);
        });

        return endpoints;
    }
}
