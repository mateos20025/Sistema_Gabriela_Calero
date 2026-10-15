using Consultorio.Application.Citas;
using Consultorio.Application.Comun;
using Consultorio.Domain.Citas;
using Consultorio.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Consultorio.Infrastructure.Citas;

/// <summary>
/// Configuración de la agenda (un único registro), horario semanal y bloqueos.
/// La configuración y el horario se guardan juntos en un solo SaveChanges (una transacción).
/// </summary>
internal sealed class RepositorioAgenda(FabricaContextos fabrica) : IRepositorioAgenda
{
    private const int MaximoBloqueos = 500;

    public async Task<AgendaConfigurada?> ObtenerAsync(CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);

        var configuracion = await db.ConfiguracionAgenda.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == ConfiguracionAgenda.IdUnico, cancellationToken);
        if (configuracion is null)
        {
            return null;
        }

        var franjas = await db.FranjasAtencion.AsNoTracking()
            .OrderBy(f => f.DiaSemana)
            .ThenBy(f => f.Inicio)
            .ToListAsync(cancellationToken);

        return new AgendaConfigurada(configuracion, franjas);
    }

    public async Task GuardarAsync(ConfiguracionAgenda configuracion, IReadOnlyList<FranjaAtencion> franjas, CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);

        var guardada = await db.ConfiguracionAgenda
            .FirstOrDefaultAsync(c => c.Id == ConfiguracionAgenda.IdUnico, cancellationToken);

        if (guardada is null)
        {
            db.ConfiguracionAgenda.Add(configuracion);
        }
        else
        {
            var entrada = db.Entry(guardada);
            entrada.Property(c => c.Version).OriginalValue = configuracion.Version;
            entrada.CurrentValues.SetValues(configuracion);

            // Siempre se actualiza la fila: así PostgreSQL comprueba la versión aunque solo cambie
            // el horario semanal, y dos personas no pueden pisarse los cambios.
            entrada.Property(c => c.ActualizadoEn).IsModified = true;
        }

        var actuales = await db.FranjasAtencion.ToListAsync(cancellationToken);
        if (!MismoHorario(actuales, franjas))
        {
            db.FranjasAtencion.RemoveRange(actuales);
            db.FranjasAtencion.AddRange(franjas);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflictoConcurrenciaException(ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Dos personas guardaron la primera configuración a la vez.
            throw new ConflictoConcurrenciaException(ex);
        }
    }

    public async Task<IReadOnlyList<BloqueoAgenda>> ListarBloqueosAsync(DateTime desdeUtc, DateTime hastaUtc, CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);
        return await db.BloqueosAgenda.AsNoTracking()
            .Where(b => b.Inicio < hastaUtc && desdeUtc < b.Fin)
            .OrderBy(b => b.Inicio)
            .Take(MaximoBloqueos)
            .ToListAsync(cancellationToken);
    }

    public async Task AgregarBloqueoAsync(BloqueoAgenda bloqueo, CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);
        db.BloqueosAgenda.Add(bloqueo);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> EliminarBloqueoAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);

        // Se carga y se elimina con SaveChanges (no con ExecuteDelete) para que quede en la auditoría.
        var bloqueo = await db.BloqueosAgenda.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (bloqueo is null)
        {
            return false;
        }

        db.BloqueosAgenda.Remove(bloqueo);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static bool MismoHorario(IReadOnlyList<FranjaAtencion> actuales, IReadOnlyList<FranjaAtencion> nuevas)
    {
        static IEnumerable<(DayOfWeek, TimeOnly, TimeOnly)> Clave(IEnumerable<FranjaAtencion> franjas) =>
            franjas.Select(f => (f.DiaSemana, f.Inicio, f.Fin)).Order();

        return actuales.Count == nuevas.Count && Clave(actuales).SequenceEqual(Clave(nuevas));
    }
}
