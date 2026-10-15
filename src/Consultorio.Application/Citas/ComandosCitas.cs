using System.Globalization;
using Consultorio.Application.Pacientes;
using Consultorio.Domain.Citas;

namespace Consultorio.Application.Citas;

/// <summary>Formulario de configuración de la agenda (solo la doctora).</summary>
public sealed class ConfiguracionAgendaComando
{
    public int DuracionCitaMinutos { get; set; }

    public int AnticipacionMinimaHoras { get; set; }

    public int AnticipacionMaximaDias { get; set; }

    public int HorasReservaSolicitud { get; set; }

    public int MaximoSolicitudesActivasPorCedula { get; set; }

    public bool SolicitudesWebHabilitadas { get; set; }

    public List<FranjaComando> Franjas { get; set; } = [];

    public uint Version { get; set; }

    public static ConfiguracionAgendaComando Desde(ConfiguracionAgendaVista vista) => new()
    {
        DuracionCitaMinutos = vista.DuracionCitaMinutos,
        AnticipacionMinimaHoras = vista.AnticipacionMinimaHoras,
        AnticipacionMaximaDias = vista.AnticipacionMaximaDias,
        HorasReservaSolicitud = vista.HorasReservaSolicitud,
        MaximoSolicitudesActivasPorCedula = vista.MaximoSolicitudesActivasPorCedula,
        SolicitudesWebHabilitadas = vista.SolicitudesWebHabilitadas,
        Franjas = [.. vista.Franjas.Select(f => new FranjaComando
        {
            DiaSemana = f.DiaSemana,
            Inicio = f.Inicio.ToTimeSpan(),
            Fin = f.Fin.ToTimeSpan(),
        })],
        Version = vista.Version,
    };
}

/// <summary>Una franja del horario semanal (hora de Ecuador).</summary>
public sealed class FranjaComando
{
    public DayOfWeek DiaSemana { get; set; } = DayOfWeek.Monday;

    public TimeSpan? Inicio { get; set; }

    public TimeSpan? Fin { get; set; }
}

/// <summary>Bloqueo de la agenda en hora de Ecuador: días completos o un rango de horas.</summary>
public sealed class CrearBloqueoComando
{
    public DateTime? Desde { get; set; }

    public DateTime? Hasta { get; set; }

    public bool DiaCompleto { get; set; } = true;

    public TimeSpan? HoraDesde { get; set; }

    public TimeSpan? HoraHasta { get; set; }

    public string Motivo { get; set; } = "";
}

public sealed class AgendarCitaComando
{
    public Guid PacienteId { get; set; }

    /// <summary>Uno de los horarios libres que devolvió la consulta de disponibilidad.</summary>
    public DateTime? InicioUtc { get; set; }

    public string Observacion { get; set; } = "";
}

/// <summary>Formulario público "Solicitar cita".</summary>
public sealed class SolicitarCitaComando : DatosPacienteComando
{
    public bool ConsentimientoAceptado { get; set; }

    /// <summary>Inicio del horario elegido en formato ISO 8601 UTC (viene de un botón de opción del formulario).</summary>
    public string Horario { get; set; } = "";

    public static string FormatearHorario(DateTime inicioUtc) => inicioUtc.ToString("O", CultureInfo.InvariantCulture);

    public static bool TryLeerHorario(string? valor, out DateTime inicioUtc)
    {
        inicioUtc = default;
        return !string.IsNullOrWhiteSpace(valor)
               && DateTime.TryParse(valor, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out inicioUtc)
               && inicioUtc.Kind == DateTimeKind.Utc;
    }
}
