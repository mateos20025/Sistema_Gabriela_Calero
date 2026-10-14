using Consultorio.Application.Auditoria;
using Consultorio.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Infrastructure.Auditoria;

internal sealed class ConsultaAuditoria(IDbContextFactory<ConsultorioDbContext> fabrica) : IConsultaAuditoria
{
    private const int MaximoEventos = 200;
    private const string NombreSistema = "Sistema";

    public async Task<IReadOnlyList<EventoAuditoria>> ObtenerHistorialAsync(
        string entidad, Guid entidadId, CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CreateDbContextAsync(cancellationToken);

        var filas = await (
                from registro in db.Auditoria.AsNoTracking()
                where registro.Entidad == entidad && registro.EntidadId == entidadId
                join usuario in db.Users on registro.UsuarioId equals usuario.Id into usuarios
                from usuario in usuarios.DefaultIfEmpty()
                orderby registro.CreadoEn descending
                select new
                {
                    registro.CreadoEn,
                    Usuario = usuario != null ? usuario.NombreCompleto : null,
                    registro.Accion,
                    registro.Campos,
                })
            .Take(MaximoEventos)
            .ToListAsync(cancellationToken);

        return filas
            .Select(f => new EventoAuditoria(
                f.CreadoEn,
                f.Usuario ?? NombreSistema,
                Enum.Parse<AccionAuditoria>(f.Accion),
                f.Campos))
            .ToList();
    }
}
