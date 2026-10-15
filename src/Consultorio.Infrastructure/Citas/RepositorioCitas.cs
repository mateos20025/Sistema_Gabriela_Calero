using Consultorio.Application.Citas;
using Consultorio.Application.Comun;
using Consultorio.Domain.Citas;
using Consultorio.Domain.Pacientes;
using Consultorio.Infrastructure.Persistencia;
using Consultorio.Infrastructure.Persistencia.Configuraciones;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Consultorio.Infrastructure.Citas;

/// <summary>
/// Repositorio de citas (EF Core + PostgreSQL), un DbContext por operación.
/// Cada cambio de estado se guarda junto con su fila de historial en el mismo SaveChanges.
/// Las consultas unen con los pacientes usando IgnoreQueryFilters: las citas de un paciente
/// deshabilitado deben seguir apareciendo.
/// </summary>
internal sealed class RepositorioCitas(FabricaContextos fabrica) : IRepositorioCitas
{
    private const int MaximoPorLista = 500;
    private const int MaximoPorPaciente = 200;
    private const string NombreSistema = "Sistema";
    private const string NombreWeb = "Solicitud web";

    public async Task<Cita?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);
        return await db.Citas.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<CitaDetalle?> ObtenerDetalleAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);

        var fila = await (
                from cita in db.Citas.AsNoTracking()
                where cita.Id == id
                join paciente in db.Pacientes.IgnoreQueryFilters() on cita.PacienteId equals paciente.Id
                select new
                {
                    cita,
                    PacienteNombre = paciente.Nombres + " " + paciente.Apellidos,
                    paciente.Cedula,
                    paciente.Telefono,
                    Deshabilitado = paciente.DeshabilitadoEn != null,
                })
            .FirstOrDefaultAsync(cancellationToken);

        if (fila is null)
        {
            return null;
        }

        var historial = await (
                from cambio in db.HistorialCitas.AsNoTracking()
                where cambio.CitaId == id
                join usuario in db.Users on cambio.UsuarioId equals usuario.Id into usuarios
                from usuario in usuarios.DefaultIfEmpty()
                orderby cambio.CreadoEn descending
                select new
                {
                    cambio.CreadoEn,
                    Usuario = usuario != null ? usuario.NombreCompleto : null,
                    cambio.EstadoAnterior,
                    cambio.EstadoNuevo,
                    cambio.Observacion,
                    cambio.InicioAnterior,
                    cambio.InicioNuevo,
                })
            .ToListAsync(cancellationToken);

        var c = fila.cita;
        return new CitaDetalle(
            c.Id, c.PacienteId, fila.PacienteNombre, fila.Cedula, fila.Telefono, fila.Deshabilitado,
            c.Inicio, c.Fin, c.Estado, c.Origen, c.ReservaExpiraEn, c.ContactoNombre, c.ContactoTelefono, c.ContactoCorreo,
            c.CreadoEn,
            [
                .. historial.Select(h => new CambioCita(
                    h.CreadoEn,
                    h.Usuario ?? (h.EstadoAnterior is null && c.Origen == OrigenRegistro.Web ? NombreWeb : NombreSistema),
                    h.EstadoAnterior,
                    h.EstadoNuevo,
                    h.Observacion,
                    h.InicioAnterior,
                    h.InicioNuevo)),
            ]);
    }

    public async Task<IReadOnlyList<IntervaloUtc>> ListarHorariosOcupadosAsync(
        DateTime desdeUtc, DateTime hastaUtc, DateTime ahoraUtc, Guid? excluirCitaId, CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);

        var horarios = await Activas(db.Citas.AsNoTracking(), ahoraUtc)
            .Where(c => c.Inicio < hastaUtc && desdeUtc < c.Fin)
            .Where(c => excluirCitaId == null || c.Id != excluirCitaId)
            .Select(c => new { c.Inicio, c.Fin })
            .ToListAsync(cancellationToken);

        return [.. horarios.Select(h => new IntervaloUtc(h.Inicio, h.Fin))];
    }

    public async Task<int> ContarActivasDePacienteAsync(Guid pacienteId, DateTime ahoraUtc, CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);
        return await Activas(db.Citas.AsNoTracking(), ahoraUtc)
            .CountAsync(c => c.PacienteId == pacienteId && c.Fin > ahoraUtc, cancellationToken);
    }

    public async Task<IReadOnlyList<CitaResumen>> ListarEnRangoAsync(DateTime desdeUtc, DateTime hastaUtc, CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);
        return await Resumenes(db, db.Citas.Where(c => c.Inicio < hastaUtc && desdeUtc < c.Fin))
            .Take(MaximoPorLista)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CitaResumen>> ListarPorEstadosAsync(IReadOnlyList<EstadoCita> estados, CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);
        var lista = estados.ToList();
        return await Resumenes(db, db.Citas.Where(c => lista.Contains(c.Estado)))
            .Take(MaximoPorLista)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CitaResumen>> ListarPorPacienteAsync(Guid pacienteId, CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);
        return await Resumenes(db, db.Citas.Where(c => c.PacienteId == pacienteId), masRecientesPrimero: true)
            .Take(MaximoPorPaciente)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Cita>> ListarReservasVencidasAsync(DateTime ahoraUtc, CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);
        return await db.Citas.AsNoTracking()
            .Where(c => c.Estado == EstadoCita.Pendiente && c.ReservaExpiraEn != null && c.ReservaExpiraEn <= ahoraUtc)
            .OrderBy(c => c.ReservaExpiraEn)
            .Take(MaximoPorLista)
            .ToListAsync(cancellationToken);
    }

    public async Task AgregarAsync(Cita cita, CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);
        db.Citas.Add(cita);
        db.HistorialCitas.AddRange(cita.CambiosNuevos);
        await GuardarAsync(db, cancellationToken);
        cita.MarcarCambiosGuardados();
    }

    public async Task ActualizarAsync(Cita cita, CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);

        var guardada = await db.Citas.FirstOrDefaultAsync(c => c.Id == cita.Id, cancellationToken)
            ?? throw new ConflictoConcurrenciaException();

        var entrada = db.Entry(guardada);
        entrada.Property(c => c.Version).OriginalValue = cita.Version;
        entrada.CurrentValues.SetValues(cita);
        db.HistorialCitas.AddRange(cita.CambiosNuevos);

        await GuardarAsync(db, cancellationToken);
        cita.MarcarCambiosGuardados();
    }

    /// <summary>Citas que ocupan su horario: estados activos, sin contar solicitudes cuya reserva ya venció.</summary>
    private static IQueryable<Cita> Activas(IQueryable<Cita> citas, DateTime ahoraUtc)
    {
        var activos = ReglasEstadoCita.Activos.ToList();
        return citas.Where(c => activos.Contains(c.Estado)
                                && !(c.Estado == EstadoCita.Pendiente && c.ReservaExpiraEn != null && c.ReservaExpiraEn <= ahoraUtc));
    }

    /// <summary>Une con el paciente y ordena por fecha de la cita (el orden va antes de crear el resumen, para que se traduzca a SQL).</summary>
    private static IQueryable<CitaResumen> Resumenes(ConsultorioDbContext db, IQueryable<Cita> citas, bool masRecientesPrimero = false)
    {
        var consulta =
            from cita in citas.AsNoTracking()
            join paciente in db.Pacientes.IgnoreQueryFilters() on cita.PacienteId equals paciente.Id
            select new { cita, paciente };

        consulta = masRecientesPrimero
            ? consulta.OrderByDescending(x => x.cita.Inicio)
            : consulta.OrderBy(x => x.cita.Inicio);

        return consulta.Select(x => new CitaResumen(
            x.cita.Id,
            x.cita.PacienteId,
            x.paciente.Nombres + " " + x.paciente.Apellidos,
            x.paciente.Cedula,
            x.paciente.Telefono,
            x.cita.ContactoTelefono,
            x.cita.Inicio,
            x.cita.Fin,
            x.cita.Estado,
            x.cita.Origen,
            x.cita.ReservaExpiraEn,
            x.cita.CreadoEn));
    }

    private static async Task GuardarAsync(ConsultorioDbContext db, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflictoConcurrenciaException(ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
                                           {
                                               SqlState: PostgresErrorCodes.ExclusionViolation,
                                               ConstraintName: CitasConfiguracion.RestriccionSinCruce,
                                           })
        {
            throw new HorarioOcupadoException(ex);
        }
    }
}
