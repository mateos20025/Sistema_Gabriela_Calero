using Consultorio.Application.Cuenta;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace Consultorio.Web.Seguridad;

/// <summary>
/// En Blazor Server una pestaña abierta mantiene una conexión ("circuito") durante horas sin hacer
/// peticiones HTTP nuevas, así que la cookie no se vuelve a revisar. Este proveedor revisa cada minuto
/// si la sesión sigue siendo válida; si la doctora desactivó la cuenta, la pantalla se cierra sola.
/// </summary>
internal sealed class ProveedorEstadoAutenticacion(ILoggerFactory loggerFactory, IServiceScopeFactory fabricaScopes)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(1);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        // Scope nuevo para leer datos frescos de la base de datos (no los del circuito).
        await using var scope = fabricaScopes.CreateAsyncScope();
        var validador = scope.ServiceProvider.GetRequiredService<IValidadorSesion>();
        return await validador.EsValidaAsync(authenticationState.User, cancellationToken);
    }
}
