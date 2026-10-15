using Consultorio.Application.Citas;
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
        services.AddScoped<ListarPagosDeCita>();
        services.AddScoped<ObtenerComprobantePago>();
        services.AddScoped<ObtenerConfiguracionPagos>();
        services.AddScoped<ActualizarConfiguracionPagos>();

        // Citas y agenda
        services.AddScoped<MotorDisponibilidad>();
        services.AddScoped<ExpirarReservasVencidas>();
        services.AddScoped<ObtenerConfiguracionAgenda>();
        services.AddScoped<ActualizarConfiguracionAgenda>();
        services.AddScoped<ListarBloqueos>();
        services.AddScoped<CrearBloqueo>();
        services.AddScoped<EliminarBloqueo>();
        services.AddScoped<ConsultarDisponibilidad>();
        services.AddScoped<ConsultarDisponibilidadPublica>();
        services.AddScoped<ObtenerAgenda>();
        services.AddScoped<AgendarCita>();
        services.AddScoped<CambiarEstadoCita>();
        services.AddScoped<ReprogramarCita>();
        services.AddScoped<ObtenerCita>();
        services.AddScoped<ListarSolicitudes>();
        services.AddScoped<ListarCitasDePaciente>();
        services.AddScoped<SolicitarCita>();

        return services;
    }
}
