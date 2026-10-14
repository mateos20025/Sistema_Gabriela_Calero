using Consultorio.Application.Abstracciones;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Infrastructure.Persistencia;

/// <summary>
/// Crea un DbContext nuevo para cada operación.
/// <para>
/// En Blazor Server una pantalla vive mucho tiempo; si compartiera un mismo DbContext, acumularía
/// datos viejos y fallaría con dos clics seguidos. Microsoft recomienda un contexto por operación.
/// </para>
/// <para>
/// Además le indica al contexto quién es el usuario actual, para que la auditoría lo registre.
/// </para>
/// </summary>
internal sealed class FabricaContextos(IDbContextFactory<ConsultorioDbContext> fabrica, IUsuarioActual usuarioActual)
{
    public async Task<ConsultorioDbContext> CrearAsync(CancellationToken cancellationToken)
    {
        var contexto = await fabrica.CreateDbContextAsync(cancellationToken);
        contexto.UsuarioAuditoriaId = (await usuarioActual.ObtenerAsync())?.Id;
        return contexto;
    }
}
