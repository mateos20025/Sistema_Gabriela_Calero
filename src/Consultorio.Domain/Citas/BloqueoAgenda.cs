using Consultorio.Domain.Comun;

namespace Consultorio.Domain.Citas;

/// <summary>Período en que no se atiende (vacaciones, feriado, congreso...). En esas horas no se ofrecen citas.</summary>
public sealed class BloqueoAgenda
{
    public const int LongitudMaximaMotivo = 120;
    public static readonly TimeSpan DuracionMaxima = TimeSpan.FromDays(366);

    private BloqueoAgenda()
    {
    }

    public Guid Id { get; private set; }

    public DateTime Inicio { get; private set; }

    public DateTime Fin { get; private set; }

    public string Motivo { get; private set; } = "";

    public DateTime CreadoEn { get; private set; }

    public DateTime ActualizadoEn { get; private set; }

    public IntervaloUtc Intervalo => new(Inicio, Fin);

    public static BloqueoAgenda Crear(IntervaloUtc intervalo, string motivo)
    {
        if (intervalo.Duracion > DuracionMaxima)
        {
            throw new ExcepcionDominio("Un bloqueo no puede durar más de un año.");
        }

        if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > LongitudMaximaMotivo)
        {
            throw new ExcepcionDominio($"Escribe el motivo del bloqueo (máximo {LongitudMaximaMotivo} caracteres).");
        }

        return new BloqueoAgenda
        {
            Id = Guid.CreateVersion7(),
            Inicio = intervalo.Inicio,
            Fin = intervalo.Fin,
            Motivo = motivo.Trim(),
        };
    }
}
