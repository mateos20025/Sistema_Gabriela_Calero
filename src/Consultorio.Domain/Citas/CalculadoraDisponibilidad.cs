namespace Consultorio.Domain.Citas;

/// <summary>
/// Calcula los horarios libres de un día: dentro del horario de atención, en saltos de la duración de
/// la cita, respetando la anticipación mínima y máxima, y fuera de bloqueos y de citas ocupadas.
/// Es una función pura (no consulta la base de datos ni la hora del sistema): fácil de probar.
/// </summary>
public static class CalculadoraDisponibilidad
{
    /// <summary>Ecuador continental: UTC-5 todo el año.</summary>
    private static readonly TimeSpan DesfaseEcuador = TimeSpan.FromHours(-5);

    public static IReadOnlyList<IntervaloUtc> HorariosLibres(
        DateOnly fechaEcuador,
        ConfiguracionAgenda configuracion,
        IReadOnlyList<FranjaAtencion> franjas,
        IReadOnlyList<IntervaloUtc> bloqueos,
        IReadOnlyList<IntervaloUtc> ocupados,
        DateTime ahoraUtc)
    {
        var hoyEcuador = DateOnly.FromDateTime(ahoraUtc + DesfaseEcuador);
        if (fechaEcuador < hoyEcuador || fechaEcuador > hoyEcuador.AddDays(configuracion.AnticipacionMaximaDias))
        {
            return [];
        }

        var primerInicioPermitido = ahoraUtc.AddHours(configuracion.AnticipacionMinimaHoras);
        var duracion = configuracion.DuracionCitaMinutos;
        var libres = new List<IntervaloUtc>();

        foreach (var franja in franjas.Where(f => f.DiaSemana == fechaEcuador.DayOfWeek).OrderBy(f => f.Inicio))
        {
            var minutoFin = MinutosDelDia(franja.Fin);
            for (var minuto = MinutosDelDia(franja.Inicio); minuto + duracion <= minutoFin; minuto += duracion)
            {
                var inicioLocal = fechaEcuador.ToDateTime(TimeOnly.MinValue).AddMinutes(minuto);
                var inicioUtc = DateTime.SpecifyKind(inicioLocal - DesfaseEcuador, DateTimeKind.Utc);
                var horario = new IntervaloUtc(inicioUtc, inicioUtc.AddMinutes(duracion));

                if (horario.Inicio < primerInicioPermitido
                    || horario.Inicio <= ahoraUtc
                    || bloqueos.Any(b => b.SeCruzaCon(horario))
                    || ocupados.Any(o => o.SeCruzaCon(horario)))
                {
                    continue;
                }

                libres.Add(horario);
            }
        }

        return libres;
    }

    /// <summary>Comprueba que un horario pedido sea exactamente uno de los horarios libres de su día.</summary>
    public static bool EstaLibre(
        IntervaloUtc horario,
        ConfiguracionAgenda configuracion,
        IReadOnlyList<FranjaAtencion> franjas,
        IReadOnlyList<IntervaloUtc> bloqueos,
        IReadOnlyList<IntervaloUtc> ocupados,
        DateTime ahoraUtc)
    {
        var fecha = FechaEcuador(horario.Inicio);
        return HorariosLibres(fecha, configuracion, franjas, bloqueos, ocupados, ahoraUtc).Contains(horario);
    }

    public static DateOnly FechaEcuador(DateTime fechaUtc) => DateOnly.FromDateTime(fechaUtc + DesfaseEcuador);

    /// <summary>Inicio del día de Ecuador expresado en UTC (00:00 en Ecuador = 05:00 UTC).</summary>
    public static DateTime InicioDelDiaUtc(DateOnly fechaEcuador) =>
        DateTime.SpecifyKind(fechaEcuador.ToDateTime(TimeOnly.MinValue) - DesfaseEcuador, DateTimeKind.Utc);

    private static int MinutosDelDia(TimeOnly hora) => hora.Hour * 60 + hora.Minute;
}
