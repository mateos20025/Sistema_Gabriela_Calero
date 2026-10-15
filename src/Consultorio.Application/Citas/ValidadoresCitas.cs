using Consultorio.Application.Pacientes;
using Consultorio.Domain.Citas;
using Consultorio.Domain.Comun;
using FluentValidation;

namespace Consultorio.Application.Citas;

public sealed class ConfiguracionAgendaValidador : AbstractValidator<ConfiguracionAgendaComando>
{
    public ConfiguracionAgendaValidador()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(c => c.DuracionCitaMinutos)
            .InclusiveBetween(ConfiguracionAgenda.DuracionMinimaMinutos, ConfiguracionAgenda.DuracionMaximaMinutos)
            .WithMessage($"Entre {ConfiguracionAgenda.DuracionMinimaMinutos} y {ConfiguracionAgenda.DuracionMaximaMinutos} minutos.")
            .Must(d => d % 5 == 0).WithMessage("Usa múltiplos de 5 minutos.");

        RuleFor(c => c.AnticipacionMinimaHoras)
            .InclusiveBetween(0, ConfiguracionAgenda.AnticipacionMinimaMaximaHoras)
            .WithMessage($"Entre 0 y {ConfiguracionAgenda.AnticipacionMinimaMaximaHoras} horas.");

        RuleFor(c => c.AnticipacionMaximaDias)
            .InclusiveBetween(1, ConfiguracionAgenda.AnticipacionMaximaLimiteDias)
            .WithMessage($"Entre 1 y {ConfiguracionAgenda.AnticipacionMaximaLimiteDias} días.");

        RuleFor(c => c.HorasReservaSolicitud)
            .InclusiveBetween(1, ConfiguracionAgenda.HorasReservaMaximas)
            .WithMessage($"Entre 1 y {ConfiguracionAgenda.HorasReservaMaximas} horas.");

        RuleFor(c => c.MaximoSolicitudesActivasPorCedula)
            .InclusiveBetween(1, ConfiguracionAgenda.SolicitudesActivasLimite)
            .WithMessage($"Entre 1 y {ConfiguracionAgenda.SolicitudesActivasLimite}.");

        RuleForEach(c => c.Franjas).ChildRules(franja =>
        {
            franja.RuleFor(f => f.Inicio).NotNull().WithMessage("Indica la hora de inicio de cada franja.");
            franja.RuleFor(f => f.Fin).NotNull().WithMessage("Indica la hora de fin de cada franja.");
            franja.RuleFor(f => f)
                .Must(f => f.Inicio is null || f.Fin is null || f.Inicio < f.Fin)
                .WithMessage("En cada franja, el fin debe ser después del inicio.")
                .Must(f => EnMultiplosDe5(f.Inicio) && EnMultiplosDe5(f.Fin))
                .WithMessage("Las horas deben estar en múltiplos de 5 minutos.");
        });

        RuleFor(c => c.Franjas)
            .Must(f => f.Count <= HorarioAtencion.MaximoFranjas)
            .WithMessage($"Máximo {HorarioAtencion.MaximoFranjas} franjas.")
            .Must(NoSeCruzan).WithMessage("Hay franjas que se cruzan en el mismo día.");
    }

    private static bool EnMultiplosDe5(TimeSpan? hora) => hora is null || (hora.Value.Seconds == 0 && hora.Value.Minutes % 5 == 0);

    private static bool NoSeCruzan(List<FranjaComando> franjas)
    {
        var completas = franjas.Where(f => f.Inicio is not null && f.Fin is not null && f.Inicio < f.Fin).ToList();

        for (var i = 0; i < completas.Count; i++)
        {
            for (var j = i + 1; j < completas.Count; j++)
            {
                var (a, b) = (completas[i], completas[j]);
                if (a.DiaSemana == b.DiaSemana && a.Inicio < b.Fin && b.Inicio < a.Fin)
                {
                    return false;
                }
            }
        }

        return true;
    }
}

public sealed class CrearBloqueoValidador : AbstractValidator<CrearBloqueoComando>
{
    public CrearBloqueoValidador()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(c => c.Desde).NotNull().WithMessage("Elige la fecha de inicio.");
        RuleFor(c => c.Hasta).NotNull().WithMessage("Elige la fecha de fin.")
            .Must((c, hasta) => c.Desde is null || hasta >= c.Desde).WithMessage("La fecha de fin no puede ser anterior a la de inicio.")
            .Must((c, hasta) => c.Desde is null || hasta!.Value.Date - c.Desde.Value.Date < BloqueoAgenda.DuracionMaxima)
            .WithMessage("Un bloqueo no puede durar más de un año.");

        When(c => !c.DiaCompleto, () =>
        {
            RuleFor(c => c.HoraDesde).NotNull().WithMessage("Indica la hora de inicio.");
            RuleFor(c => c.HoraHasta).NotNull().WithMessage("Indica la hora de fin.")
                .Must((c, hasta) => c.Desde != c.Hasta || c.HoraDesde is null || hasta > c.HoraDesde)
                .WithMessage("La hora de fin debe ser después de la de inicio.");
        });

        RuleFor(c => c.Motivo)
            .NotEmpty().WithMessage("Escribe el motivo, por ejemplo \"Vacaciones\".")
            .MaximumLength(BloqueoAgenda.LongitudMaximaMotivo).WithMessage($"Máximo {BloqueoAgenda.LongitudMaximaMotivo} caracteres.");
    }
}

public sealed class AgendarCitaValidador : AbstractValidator<AgendarCitaComando>
{
    public AgendarCitaValidador()
    {
        RuleFor(c => c.PacienteId).NotEmpty().WithMessage("Elige el paciente.");
        RuleFor(c => c.InicioUtc).NotNull().WithMessage("Elige un horario.");
        RuleFor(c => c.Observacion).MaximumLength(Cita.LongitudMaximaObservacion)
            .WithMessage($"Máximo {Cita.LongitudMaximaObservacion} caracteres.");
    }
}

public sealed class SolicitarCitaValidador : AbstractValidator<SolicitarCitaComando>
{
    public SolicitarCitaValidador()
    {
        Include(new DatosPacienteValidador());

        RuleFor(c => c.Horario)
            .Must(h => SolicitarCitaComando.TryLeerHorario(h, out _)).WithMessage("Elige un horario.");

        RuleFor(c => c.ConsentimientoAceptado)
            .Equal(true).WithMessage("Para pedir la cita debes aceptar el tratamiento de tus datos personales.");
    }
}

/// <summary>Convierte fechas y horas de Ecuador del formulario de bloqueos en un intervalo UTC.</summary>
public static class ConversionBloqueo
{
    public static IntervaloUtc AIntervalo(CrearBloqueoComando comando)
    {
        if (comando.Desde is null || comando.Hasta is null)
        {
            throw new ExcepcionDominio("Faltan las fechas del bloqueo.");
        }

        var desde = DateOnly.FromDateTime(comando.Desde.Value);
        var hasta = DateOnly.FromDateTime(comando.Hasta.Value);

        if (comando.DiaCompleto)
        {
            return new IntervaloUtc(CalculadoraDisponibilidad.InicioDelDiaUtc(desde), CalculadoraDisponibilidad.InicioDelDiaUtc(hasta.AddDays(1)));
        }

        var inicio = CalculadoraDisponibilidad.InicioDelDiaUtc(desde) + (comando.HoraDesde ?? TimeSpan.Zero);
        var fin = CalculadoraDisponibilidad.InicioDelDiaUtc(hasta) + (comando.HoraHasta ?? TimeSpan.Zero);
        return new IntervaloUtc(inicio, fin);
    }
}
