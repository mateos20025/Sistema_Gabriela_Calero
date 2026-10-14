using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Consultorio.Application;

public static class DependencyInjection
{
    /// <summary>Registra los servicios de la capa Application (por ahora, los validadores).</summary>
    public static IServiceCollection AddAplicacion(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}
