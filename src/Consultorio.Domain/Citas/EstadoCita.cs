namespace Consultorio.Domain.Citas;

public enum EstadoCita
{
    /// <summary>Solicitud recién llegada (web). Reserva el horario por un tiempo limitado.</summary>
    Pendiente = 1,

    /// <summary>La asistente la tomó y está en su cola de llamadas.</summary>
    EnCola = 2,

    Confirmada = 3,

    /// <summary>Se movió a otro horario; falta que el paciente lo confirme.</summary>
    Reprogramada = 4,

    Cancelada = 5,

    /// <summary>Se llamó al paciente y no contestó.</summary>
    NoContesta = 6,

    Atendida = 7,

    NoAsistio = 8,
}

/// <summary>
/// Reglas de transición de estados (CLAUDE.md):
/// Pendiente → EnCola; EnCola → Confirmada | Reprogramada | Cancelada | NoContesta;
/// NoContesta → EnCola | Cancelada; Confirmada → Reprogramada | Cancelada | Atendida | NoAsistio;
/// Reprogramada → Confirmada | Cancelada. Además, Pendiente → Cancelada para liberar reservas vencidas.
/// Cancelada, Atendida y NoAsistio son finales.
/// </summary>
public static class ReglasEstadoCita
{
    private static readonly Dictionary<EstadoCita, EstadoCita[]> Transiciones = new()
    {
        [EstadoCita.Pendiente] = [EstadoCita.EnCola, EstadoCita.Cancelada],
        [EstadoCita.EnCola] = [EstadoCita.Confirmada, EstadoCita.Reprogramada, EstadoCita.Cancelada, EstadoCita.NoContesta],
        [EstadoCita.NoContesta] = [EstadoCita.EnCola, EstadoCita.Cancelada],
        [EstadoCita.Confirmada] = [EstadoCita.Reprogramada, EstadoCita.Cancelada, EstadoCita.Atendida, EstadoCita.NoAsistio],
        [EstadoCita.Reprogramada] = [EstadoCita.Confirmada, EstadoCita.Cancelada],
    };

    /// <summary>Estados que ocupan el horario (no puede haber otra cita cruzada).</summary>
    public static IReadOnlyList<EstadoCita> Activos { get; } =
    [
        EstadoCita.Pendiente, EstadoCita.EnCola, EstadoCita.Confirmada, EstadoCita.Reprogramada, EstadoCita.NoContesta,
    ];

    public static bool EsActivo(EstadoCita estado) => Activos.Contains(estado);

    public static bool EsFinal(EstadoCita estado) => !Transiciones.ContainsKey(estado);

    public static bool PuedePasar(EstadoCita desde, EstadoCita hacia) =>
        Transiciones.TryGetValue(desde, out var destinos) && destinos.Contains(hacia);

    public static IReadOnlyList<EstadoCita> SiguientesDe(EstadoCita estado) =>
        Transiciones.TryGetValue(estado, out var destinos) ? destinos : [];
}
