using Consultorio.Application.Comun;
using Consultorio.Application.Pagos;
using Consultorio.Domain.Pagos;
using Consultorio.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Consultorio.Infrastructure.Pagos;

/// <summary>Configuración de pagos: un único registro que se crea la primera vez que la doctora la guarda.</summary>
internal sealed class RepositorioConfiguracionPagos(FabricaContextos fabrica) : IRepositorioConfiguracionPagos
{
    public async Task<ConfiguracionPagos> ObtenerAsync(CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);
        return await db.ConfiguracionPagos.AsNoTracking()
                   .FirstOrDefaultAsync(c => c.Id == ConfiguracionPagos.IdUnico, cancellationToken)
               ?? ConfiguracionPagos.Nueva();
    }

    public async Task GuardarAsync(ConfiguracionPagos configuracion, CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);

        var guardada = await db.ConfiguracionPagos
            .FirstOrDefaultAsync(c => c.Id == ConfiguracionPagos.IdUnico, cancellationToken);

        if (guardada is null)
        {
            db.ConfiguracionPagos.Add(configuracion);
        }
        else
        {
            var entrada = db.Entry(guardada);
            entrada.Property(c => c.Version).OriginalValue = configuracion.Version;
            entrada.CurrentValues.SetValues(configuracion);
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
}
