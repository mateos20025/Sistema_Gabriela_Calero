using System.Security.Claims;
using Consultorio.Application.Citas;
using Microsoft.AspNetCore.Components.Authorization;

namespace Consultorio.Web.Tareas;

/// <summary>
/// Cada minuto libera las solicitudes web cuya reserva venció sin que el consultorio las atendiera,
/// para que esos horarios vuelvan a ofrecerse. Los cambios quedan en el historial como hechos por el sistema.
/// </summary>
internal sealed class ExpiradorReservasCitas(IServiceScopeFactory fabricaScopes, ILogger<ExpiradorReservasCitas> logger)
    : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var temporizador = new PeriodicTimer(Intervalo);

        try
        {
            // Primero se espera un intervalo: al arrancar, la base de datos puede estar aplicando migraciones.
            while (await temporizador.WaitForNextTickAsync(stoppingToken))
            {
                await LiberarAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // La aplicación se está deteniendo.
        }
    }

    private async Task LiberarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = fabricaScopes.CreateAsyncScope();

            // Fuera de una petición web no hay usuario: se indica explícitamente que es anónimo,
            // así la auditoría y el historial lo registran como "Sistema".
            if (scope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>() is IHostEnvironmentAuthenticationStateProvider proveedor)
            {
                proveedor.SetAuthenticationState(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
            }

            var liberadas = await scope.ServiceProvider.GetRequiredService<ExpirarReservasVencidas>().EjecutarAsync(cancellationToken);
            if (liberadas > 0)
            {
                logger.LogInformation("Se liberaron {Cantidad} solicitudes de cita con la reserva vencida.", liberadas);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Se registra y se reintenta en el siguiente minuto; un error aquí no debe detener la aplicación.
            logger.LogError(ex, "No se pudieron liberar las reservas de citas vencidas.");
        }
    }
}
