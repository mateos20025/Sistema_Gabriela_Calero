using Consultorio.Domain.Comun;

namespace Consultorio.Domain.Citas;

/// <summary>
/// Un intervalo de tiempo [Inicio, Fin) en UTC: incluye el inicio y excluye el fin, así una cita de
/// 9:00 a 9:30 y otra de 9:30 a 10:00 NO se cruzan.
/// </summary>
public readonly record struct IntervaloUtc
{
    public IntervaloUtc(DateTime inicio, DateTime fin)
    {
        if (inicio.Kind != DateTimeKind.Utc || fin.Kind != DateTimeKind.Utc)
        {
            throw new ExcepcionDominio("Las fechas del horario deben estar en UTC.");
        }

        if (fin <= inicio)
        {
            throw new ExcepcionDominio("El fin del horario debe ser posterior al inicio.");
        }

        Inicio = inicio;
        Fin = fin;
    }

    public DateTime Inicio { get; }

    public DateTime Fin { get; }

    public TimeSpan Duracion => Fin - Inicio;

    public bool SeCruzaCon(IntervaloUtc otro) => Inicio < otro.Fin && otro.Inicio < Fin;
}
