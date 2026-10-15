using Consultorio.Application.Pacientes;
using Consultorio.Application.Pagos;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Consultorio.Application;

public static class DependencyInjection
{
    /// <summary>Registra los validadores y los casos de uso de la capa Application.</summary>
    public static IServiceCollection AddAplicacion(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Pacientes
        services.AddScoped<RegistrarPaciente>();
        services.AddScoped<ActualizarPaciente>();
        services.AddScoped<DeshabilitarPaciente>();
        services.AddScoped<HabilitarPaciente>();
        services.AddScoped<BuscarPacientes>();
        services.AddScoped<ObtenerPaciente>();

        // Pagos
        services.AddScoped<RegistrarPago>();
        services.AddScoped<ValidarPago>();
        services.AddScoped<RechazarPago>();
        services.AddScoped<MarcarPagoRecibido>();
        services.AddScoped<AnularPago>();
        services.AddScoped<BuscarPagos>();
        services.AddScoped<ObtenerPago>();
        services.AddScoped<ListarPagosDePaciente>();
        services.AddScoped<ObtenerComprobantePago>();
        services.AddScoped<ObtenerConfiguracionPagos>();
        services.AddScoped<ActualizarConfiguracionPagos>();

        return services;
    }
}
