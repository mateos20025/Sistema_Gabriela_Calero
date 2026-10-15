using Consultorio.Application.Comun;
using Consultorio.Domain.Citas;

namespace Consultorio.Application.Citas;

public interface IRepositorioCitas
{
    Task<Cita?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task<CitaDetalle?> ObtenerDetalleAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Horarios que ocupan las citas activas en el rango (sin contar reservas vencidas).</summary>
    Task<IReadOnlyList<IntervaloUtc>> ListarHorariosOcupadosAsync(
        DateTime desdeUtc, DateTime hastaUtc, DateTime ahoraUtc, Guid? excluirCitaId, CancellationToken cancellationToken);

    /// <summary>Citas activas y futuras del paciente (para el límite de solicitudes por cédula).</summary>
    Task<int> ContarActivasDePacienteAsync(Guid pacienteId, DateTime ahoraUtc, CancellationToken cancellationToken);

    Task<IReadOnlyList<CitaResumen>> ListarEnRangoAsync(DateTime desdeUtc, DateTime hastaUtc, CancellationToken cancellationToken);

    Task<IReadOnlyList<CitaResumen>> ListarPorEstadosAsync(IReadOnlyList<EstadoCita> estados, CancellationToken cancellationToken);

    Task<IReadOnlyList<CitaResumen>> ListarPorPacienteAsync(Guid pacienteId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Cita>> ListarReservasVencidasAsync(DateTime ahoraUtc, CancellationToken cancellationToken);

    /// <exception cref="HorarioOcupadoException">Otra cita activa se cruza con ese horario.</exception>
    Task AgregarAsync(Cita cita, CancellationToken cancellationToken);

    /// <exception cref="ConflictoConcurrenciaException">Otra persona la modificó antes.</exception>
    /// <exception cref="HorarioOcupadoException">El nuevo horario se cruza con otra cita.</exception>
    Task ActualizarAsync(Cita cita, CancellationToken cancellationToken);
}

/// <summary>Configuración de la agenda junto con su horario semanal.</summary>
public sealed record AgendaConfigurada(ConfiguracionAgenda Configuracion, IReadOnlyList<FranjaAtencion> Franjas);

public interface IRepositorioAgenda
{
    /// <summary><c>null</c> si la doctora todavía no configuró la agenda.</summary>
    Task<AgendaConfigurada?> ObtenerAsync(CancellationToken cancellationToken);

    /// <summary>Guarda la configuración y reemplaza el horario semanal, todo en una transacción.</summary>
    /// <exception cref="ConflictoConcurrenciaException">Otra persona la modificó antes.</exception>
    Task GuardarAsync(ConfiguracionAgenda configuracion, IReadOnlyList<FranjaAtencion> franjas, CancellationToken cancellationToken);

    Task<IReadOnlyList<BloqueoAgenda>> ListarBloqueosAsync(DateTime desdeUtc, DateTime hastaUtc, CancellationToken cancellationToken);

    Task AgregarBloqueoAsync(BloqueoAgenda bloqueo, CancellationToken cancellationToken);

    /// <returns><c>false</c> si no existía.</returns>
    Task<bool> EliminarBloqueoAsync(Guid id, CancellationToken cancellationToken);
}

/// <summary>
/// Comprueba que quien envía el formulario público es una persona y no un programa (captcha).
/// La implementación usa Cloudflare Turnstile; si no está configurado, queda deshabilitado.
/// </summary>
public interface IVerificadorHumano
{
    bool Habilitado { get; }

    /// <summary>Clave pública del widget que se muestra en la página.</summary>
    string? ClaveSitio { get; }

    Task<bool> VerificarAsync(string? respuesta, string? ipCliente, CancellationToken cancellationToken);
}
