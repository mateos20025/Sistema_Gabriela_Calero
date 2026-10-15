using Consultorio.Domain.Comun;

namespace Consultorio.Domain.Citas;

/// <summary>
/// Una franja de atención semanal en hora de Ecuador, por ejemplo "lunes de 09:00 a 13:00".
/// Un día puede tener varias franjas (mañana y tarde).
/// </summary>
public sealed class FranjaAtencion
{
    private FranjaAtencion()
    {
    }

    public Guid Id { get; private set; }

    public DayOfWeek DiaSemana { get; private set; }

    public TimeOnly Inicio { get; private set; }

    public TimeOnly Fin { get; private set; }

    public DateTime CreadoEn { get; private set; }

    public DateTime ActualizadoEn { get; private set; }

    public static FranjaAtencion Crear(DayOfWeek diaSemana, TimeOnly inicio, TimeOnly fin)
    {
        if (!Enum.IsDefined(diaSemana))
        {
            throw new ExcepcionDominio("El día de la semana no es válido.");
        }

        if (fin <= inicio)
        {
            throw new ExcepcionDominio("La hora de fin debe ser posterior a la de inicio.");
        }

        if (inicio.Second != 0 || fin.Second != 0 || inicio.Minute % 5 != 0 || fin.Minute % 5 != 0)
        {
            throw new ExcepcionDominio("Las horas deben estar en múltiplos de 5 minutos.");
        }

        return new FranjaAtencion { Id = Guid.CreateVersion7(), DiaSemana = diaSemana, Inicio = inicio, Fin = fin };
    }

    public bool SeCruzaCon(FranjaAtencion otra) =>
        DiaSemana == otra.DiaSemana && Inicio < otra.Fin && otra.Inicio < Fin;
}

/// <summary>Reglas del horario semanal completo.</summary>
public static class HorarioAtencion
{
    public const int MaximoFranjas = 40;

    /// <summary>Devuelve el problema encontrado, o <c>null</c> si el horario es válido.</summary>
    public static string? Problema(IReadOnlyList<FranjaAtencion> franjas)
    {
        if (franjas.Count > MaximoFranjas)
        {
            return $"El horario puede tener como máximo {MaximoFranjas} franjas.";
        }

        for (var i = 0; i < franjas.Count; i++)
        {
            for (var j = i + 1; j < franjas.Count; j++)
            {
                if (franjas[i].SeCruzaCon(franjas[j]))
                {
                    return "Hay franjas que se cruzan en el mismo día.";
                }
            }
        }

        return null;
    }

    public static void Validar(IReadOnlyList<FranjaAtencion> franjas)
    {
        if (Problema(franjas) is { } problema)
        {
            throw new ExcepcionDominio(problema);
        }
    }
}
