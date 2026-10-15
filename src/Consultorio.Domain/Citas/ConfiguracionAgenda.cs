using Consultorio.Domain.Comun;

namespace Consultorio.Domain.Citas;

/// <summary>
/// Reglas de la agenda que define la doctora. No hay valores por defecto escritos en el código:
/// mientras no la configure, no se pueden agendar citas.
/// </summary>
public sealed class ConfiguracionAgenda
{
    public static readonly Guid IdUnico = new("7c1d8e2f-3a4b-4c5d-8e6f-a1b2c3d4e5f6");

    public const int DuracionMinimaMinutos = 10;
    public const int DuracionMaximaMinutos = 240;
    public const int AnticipacionMinimaMaximaHoras = 168;
    public const int AnticipacionMaximaLimiteDias = 365;
    public const int HorasReservaMaximas = 168;
    public const int SolicitudesActivasLimite = 10;

    private ConfiguracionAgenda()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Duración de cada cita. Los horarios libres se ofrecen en saltos de esta duración.</summary>
    public int DuracionCitaMinutos { get; private set; }

    /// <summary>No se pueden pedir citas que empiecen antes de este número de horas desde ahora.</summary>
    public int AnticipacionMinimaHoras { get; private set; }

    /// <summary>No se pueden pedir citas a más de este número de días.</summary>
    public int AnticipacionMaximaDias { get; private set; }

    /// <summary>Cuánto tiempo reserva el horario una solicitud web sin que el consultorio la atienda.</summary>
    public int HorasReservaSolicitud { get; private set; }

    /// <summary>Máximo de citas activas (no canceladas ni pasadas) por paciente al pedir por la web.</summary>
    public int MaximoSolicitudesActivasPorCedula { get; private set; }

    public bool SolicitudesWebHabilitadas { get; private set; }

    public uint Version { get; private set; }

    public DateTime CreadoEn { get; private set; }

    public DateTime ActualizadoEn { get; private set; }

    public static ConfiguracionAgenda Crear(
        int duracionCitaMinutos, int anticipacionMinimaHoras, int anticipacionMaximaDias,
        int horasReservaSolicitud, int maximoSolicitudesActivasPorCedula, bool solicitudesWebHabilitadas)
    {
        var configuracion = new ConfiguracionAgenda { Id = IdUnico };
        configuracion.Actualizar(duracionCitaMinutos, anticipacionMinimaHoras, anticipacionMaximaDias,
            horasReservaSolicitud, maximoSolicitudesActivasPorCedula, solicitudesWebHabilitadas);
        return configuracion;
    }

    public void Actualizar(
        int duracionCitaMinutos, int anticipacionMinimaHoras, int anticipacionMaximaDias,
        int horasReservaSolicitud, int maximoSolicitudesActivasPorCedula, bool solicitudesWebHabilitadas)
    {
        if (duracionCitaMinutos is < DuracionMinimaMinutos or > DuracionMaximaMinutos || duracionCitaMinutos % 5 != 0)
        {
            throw new ExcepcionDominio($"La duración debe estar entre {DuracionMinimaMinutos} y {DuracionMaximaMinutos} minutos, en múltiplos de 5.");
        }

        if (anticipacionMinimaHoras is < 0 or > AnticipacionMinimaMaximaHoras)
        {
            throw new ExcepcionDominio($"La anticipación mínima debe estar entre 0 y {AnticipacionMinimaMaximaHoras} horas.");
        }

        if (anticipacionMaximaDias is < 1 or > AnticipacionMaximaLimiteDias)
        {
            throw new ExcepcionDominio($"La anticipación máxima debe estar entre 1 y {AnticipacionMaximaLimiteDias} días.");
        }

        if (horasReservaSolicitud is < 1 or > HorasReservaMaximas)
        {
            throw new ExcepcionDominio($"La reserva de una solicitud debe durar entre 1 y {HorasReservaMaximas} horas.");
        }

        if (maximoSolicitudesActivasPorCedula is < 1 or > SolicitudesActivasLimite)
        {
            throw new ExcepcionDominio($"El máximo de citas activas por paciente debe estar entre 1 y {SolicitudesActivasLimite}.");
        }

        DuracionCitaMinutos = duracionCitaMinutos;
        AnticipacionMinimaHoras = anticipacionMinimaHoras;
        AnticipacionMaximaDias = anticipacionMaximaDias;
        HorasReservaSolicitud = horasReservaSolicitud;
        MaximoSolicitudesActivasPorCedula = maximoSolicitudesActivasPorCedula;
        SolicitudesWebHabilitadas = solicitudesWebHabilitadas;
    }
}
