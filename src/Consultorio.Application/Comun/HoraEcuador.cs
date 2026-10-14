namespace Consultorio.Application.Comun;

/// <summary>
/// Conversión entre UTC y la hora de Ecuador continental.
/// Ecuador usa UTC-5 todo el año (no tiene horario de verano), por eso basta un desfase fijo.
/// </summary>
public static class HoraEcuador
{
    public static readonly TimeSpan Desfase = TimeSpan.FromHours(-5);

    public static DateTime DesdeUtc(DateTime utc)
    {
        if (utc.Kind == DateTimeKind.Local)
        {
            throw new ArgumentException("Se esperaba una fecha en UTC, no en hora local.", nameof(utc));
        }

        return DateTime.SpecifyKind(utc + Desfase, DateTimeKind.Unspecified);
    }

    public static DateTime AUtc(DateTime horaEcuador) =>
        DateTime.SpecifyKind(horaEcuador - Desfase, DateTimeKind.Utc);
}
