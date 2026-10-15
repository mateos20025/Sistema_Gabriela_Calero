using Consultorio.Application.Abstracciones;
using Consultorio.Domain.Citas;

namespace Consultorio.Application.Citas;

/// <summary>
/// Reúne lo que necesita la <see cref="CalculadoraDisponibilidad"/> (configuración, horario semanal,
/// bloqueos y citas ocupadas) y la ejecuta. Lo comparten el panel, la página pública y las reprogramaciones.
/// </summary>
public sealed class MotorDisponibilidad(IRepositorioAgenda repositorioAgenda, IRepositorioCitas repositorioCitas, IRelojSistema reloj)
{
    /// <returns><c>null</c> si la agenda no está configurada.</returns>
    public async Task<IReadOnlyList<DiaDisponible>?> CalcularAsync(
        DateOnly desde, int dias, Guid? excluirCitaId, CancellationToken cancellationToken)
    {
        var agenda = await repositorioAgenda.ObtenerAsync(cancellationToken);
        if (agenda is null)
        {
            return null;
        }

        var ahora = reloj.AhoraUtc;
        var inicioUtc = CalculadoraDisponibilidad.InicioDelDiaUtc(desde);
        var finUtc = CalculadoraDisponibilidad.InicioDelDiaUtc(desde.AddDays(dias));

        var bloqueos = (await repositorioAgenda.ListarBloqueosAsync(inicioUtc, finUtc, cancellationToken))
            .Select(b => b.Intervalo)
            .ToList();
        var ocupados = await repositorioCitas.ListarHorariosOcupadosAsync(inicioUtc, finUtc, ahora, excluirCitaId, cancellationToken);

        return Enumerable.Range(0, dias)
            .Select(desde.AddDays)
            .Select(fecha => new DiaDisponible(
                fecha,
                CalculadoraDisponibilidad.HorariosLibres(fecha, agenda.Configuracion, agenda.Franjas, bloqueos, ocupados, ahora)))
            .ToList();
    }

    /// <summary>¿El horario es exactamente uno de los libres de su día?</summary>
    public async Task<bool> EstaLibreAsync(IntervaloUtc horario, Guid? excluirCitaId, CancellationToken cancellationToken)
    {
        var dia = (await CalcularAsync(CalculadoraDisponibilidad.FechaEcuador(horario.Inicio), 1, excluirCitaId, cancellationToken))?.Single();
        return dia is not null && dia.Horarios.Contains(horario);
    }

    public DateOnly HoyEnEcuador() => CalculadoraDisponibilidad.FechaEcuador(reloj.AhoraUtc);
}
