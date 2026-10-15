using Consultorio.Domain.Citas;
using Consultorio.Domain.Pacientes;

namespace Consultorio.Application.Citas;

/// <summary>Fila de agenda o de la cola. Fechas en UTC.</summary>
public sealed record CitaResumen(
    Guid Id,
    Guid PacienteId,
    string PacienteNombre,
    string PacienteCedula,
    string PacienteTelefono,
    string? ContactoTelefono,
    DateTime InicioUtc,
    DateTime FinUtc,
    EstadoCita Estado,
    OrigenRegistro Origen,
    DateTime? ReservaExpiraUtc,
    DateTime CreadoUtc)
{
    /// <summary>Número al que conviene llamar: el que dejó en la solicitud, o el de su ficha.</summary>
    public string TelefonoParaLlamar => ContactoTelefono ?? PacienteTelefono;
}

/// <summary>Un cambio del historial, con el nombre de quien lo hizo.</summary>
public sealed record CambioCita(
    DateTime FechaUtc,
    string Usuario,
    EstadoCita? EstadoAnterior,
    EstadoCita EstadoNuevo,
    string? Observacion,
    DateTime? InicioAnteriorUtc,
    DateTime? InicioNuevoUtc);

public sealed record CitaDetalle(
    Guid Id,
    Guid PacienteId,
    string PacienteNombre,
    string PacienteCedula,
    string PacienteTelefono,
    bool PacienteDeshabilitado,
    DateTime InicioUtc,
    DateTime FinUtc,
    EstadoCita Estado,
    OrigenRegistro Origen,
    DateTime? ReservaExpiraUtc,
    string? ContactoNombre,
    string? ContactoTelefono,
    string? ContactoCorreo,
    DateTime CreadoUtc,
    IReadOnlyList<CambioCita> Historial)
{
    /// <summary>Estados a los que puede pasar desde el actual (sin contar Reprogramada, que pide horario).</summary>
    public IReadOnlyList<EstadoCita> SiguientesEstados =>
        [.. ReglasEstadoCita.SiguientesDe(Estado).Where(e => e != EstadoCita.Reprogramada)];

    public bool SePuedeReprogramar => ReglasEstadoCita.PuedePasar(Estado, EstadoCita.Reprogramada);

    public bool EstaActiva => ReglasEstadoCita.EsActivo(Estado);
}

public sealed record DiaDisponible(DateOnly Fecha, IReadOnlyList<IntervaloUtc> Horarios);

public sealed record BloqueoVista(Guid Id, DateTime InicioUtc, DateTime FinUtc, string Motivo);

public sealed record FranjaVista(DayOfWeek DiaSemana, TimeOnly Inicio, TimeOnly Fin);

/// <summary>Lo necesario para dibujar la agenda de uno o varios días.</summary>
public sealed record AgendaVista(
    DateOnly Desde,
    int Dias,
    bool Configurada,
    int DuracionCitaMinutos,
    IReadOnlyList<FranjaVista> Franjas,
    IReadOnlyList<CitaResumen> Citas,
    IReadOnlyList<BloqueoVista> Bloqueos);

/// <summary>Configuración de la agenda para mostrar y editar.</summary>
public sealed record ConfiguracionAgendaVista(
    bool Configurada,
    int DuracionCitaMinutos,
    int AnticipacionMinimaHoras,
    int AnticipacionMaximaDias,
    int HorasReservaSolicitud,
    int MaximoSolicitudesActivasPorCedula,
    bool SolicitudesWebHabilitadas,
    IReadOnlyList<FranjaVista> Franjas,
    uint Version)
{
    public static ConfiguracionAgendaVista SinConfigurar { get; } = new(false, 0, 0, 0, 0, 0, false, [], 0);

    public static ConfiguracionAgendaVista Desde(AgendaConfigurada agenda) => new(
        true,
        agenda.Configuracion.DuracionCitaMinutos,
        agenda.Configuracion.AnticipacionMinimaHoras,
        agenda.Configuracion.AnticipacionMaximaDias,
        agenda.Configuracion.HorasReservaSolicitud,
        agenda.Configuracion.MaximoSolicitudesActivasPorCedula,
        agenda.Configuracion.SolicitudesWebHabilitadas,
        [.. agenda.Franjas.OrderBy(f => DiaDesdeLunes(f.DiaSemana)).ThenBy(f => f.Inicio).Select(f => new FranjaVista(f.DiaSemana, f.Inicio, f.Fin))],
        agenda.Configuracion.Version);

    /// <summary>En Ecuador la semana se lee de lunes (0) a domingo (6).</summary>
    private static int DiaDesdeLunes(DayOfWeek dia) => ((int)dia + 6) % 7;
}

/// <summary>Cola de trabajo de la asistente, agrupada por estado.</summary>
public sealed record ColaSolicitudes(
    IReadOnlyList<CitaResumen> Nuevas,
    IReadOnlyList<CitaResumen> PorLlamar,
    IReadOnlyList<CitaResumen> NoContestaron,
    IReadOnlyList<CitaResumen> Reprogramadas);

/// <summary>Resultado de una solicitud web: lo que se le muestra al paciente.</summary>
public sealed record SolicitudRegistrada(DateTime InicioUtc, DateTime ReservaExpiraUtc);

/// <summary>Nombre de cada estado tal como lo lee el personal.</summary>
public static class NombresEstadoCita
{
    public static string De(EstadoCita estado) => estado switch
    {
        EstadoCita.Pendiente => "Pendiente",
        EstadoCita.EnCola => "En cola",
        EstadoCita.Confirmada => "Confirmada",
        EstadoCita.Reprogramada => "Reprogramada",
        EstadoCita.Cancelada => "Cancelada",
        EstadoCita.NoContesta => "No contesta",
        EstadoCita.Atendida => "Atendida",
        EstadoCita.NoAsistio => "No asistió",
        _ => estado.ToString(),
    };
}

public static class MensajesCitas
{
    public const string AgendaNoConfigurada = "La agenda aún no está configurada. La doctora debe definir la duración de las citas y los horarios de atención.";
    public const string HorarioNoDisponible = "Ese horario ya no está disponible. Elige otro.";
    public const string PacienteNoExiste = "El paciente no existe.";
    public const string PacienteDeshabilitado = "El paciente está deshabilitado. Habilítalo para agendarle citas.";
    public const string CitaNoExiste = "La cita no existe.";
    public const string MotivoCancelacionObligatorio = "Indica el motivo de la cancelación.";
    public const string AunNoEsHora = "Solo se puede marcar después de la hora de la cita.";
    public const string NoSePuedeReprogramar = "Esta cita no se puede reprogramar en su estado actual.";
    public const string MismoHorario = "Elige un horario distinto al actual.";
    public const string SolicitudesWebDeshabilitadas = "Por ahora no recibimos solicitudes de cita en línea. Comunícate con el consultorio.";
    public const string NoPudimosRegistrar = "No pudimos registrar tu solicitud en línea. Comunícate con el consultorio.";
    public const string VerificacionFallida = "No pudimos verificar que no eres un robot. Inténtalo de nuevo.";
    public const string BloqueoNoExiste = "El bloqueo no existe.";
    public const string ModificadoPorOtraPersona =
        "Otra persona modificó esta cita mientras la revisabas. Vuelve a abrirla para ver los datos actuales.";

    public static string TransicionNoPermitida(EstadoCita actual, EstadoCita nuevo) =>
        $"Una cita en estado «{NombresEstadoCita.De(actual)}» no puede pasar a «{NombresEstadoCita.De(nuevo)}».";

    public static string LimiteSolicitudes(int maximo) =>
        $"Esta cédula ya tiene {maximo} citas o solicitudes activas, que es el máximo permitido. Comunícate con el consultorio.";

    public static string BloqueoConCitas(int cantidad) =>
        $"Hay {cantidad} cita(s) activa(s) en ese período. Reprográmalas o cancélalas antes de bloquearlo.";
}
