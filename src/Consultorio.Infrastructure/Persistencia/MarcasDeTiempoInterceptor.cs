using Consultorio.Application.Abstracciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Consultorio.Infrastructure.Persistencia;

/// <summary>
/// Se ejecuta justo antes de cada SaveChanges y llena creado_en / actualizado_en en UTC,
/// para que ningún caso de uso tenga que acordarse de hacerlo.
/// </summary>
public sealed class MarcasDeTiempoInterceptor(IRelojSistema reloj) : SaveChangesInterceptor
{
    public const string CreadoEn = nameof(CreadoEn);
    public const string ActualizadoEn = nameof(ActualizadoEn);

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Marcar(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Marcar(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Marcar(DbContext? contexto)
    {
        if (contexto is null)
        {
            return;
        }

        var ahora = reloj.AhoraUtc;

        foreach (var entrada in contexto.ChangeTracker.Entries())
        {
            if (entrada.Metadata.FindProperty(CreadoEn) is null)
            {
                continue;
            }

            switch (entrada.State)
            {
                case EntityState.Added:
                    entrada.Property(CreadoEn).CurrentValue = ahora;
                    entrada.Property(ActualizadoEn).CurrentValue = ahora;
                    break;

                case EntityState.Modified:
                    entrada.Property(CreadoEn).IsModified = false;
                    entrada.Property(ActualizadoEn).CurrentValue = ahora;
                    break;
            }
        }
    }
}
