using Consultorio.Application.Comun;
using Consultorio.Application.Pagos;
using Consultorio.Domain.Pagos;
using Consultorio.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Consultorio.Infrastructure.Pagos;

/// <summary>
/// Repositorio de pagos (EF Core + PostgreSQL), un DbContext por operación.
/// Las consultas unen con los pacientes usando IgnoreQueryFilters: los pagos de un paciente
/// deshabilitado deben seguir apareciendo.
/// </summary>
internal sealed class RepositorioPagos(FabricaContextos fabrica) : IRepositorioPagos
{
    private const string CaracterEscape = @"\";
    private const int MaximoPagosPorPaciente = 200;

    public async Task<Pago?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);
        return await db.Pagos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<PagoDetalle?> ObtenerDetalleAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);

        var fila = await (
                from pago in db.Pagos.AsNoTracking()
                where pago.Id == id
                join paciente in db.Pacientes.IgnoreQueryFilters() on pago.PacienteId equals paciente.Id
                join registrador in db.Users on pago.RegistradoPorId equals registrador.Id
                join resolutor in db.Users on pago.ResueltoPorId equals resolutor.Id into resolutores
                from resolutor in resolutores.DefaultIfEmpty()
                join cita in db.Citas on pago.CitaId equals cita.Id into citas
                from cita in citas.DefaultIfEmpty()
                select new
                {
                    pago,
                    CitaInicio = cita != null ? cita.Inicio : (DateTime?)null,
                    PacienteNombre = paciente.Nombres + " " + paciente.Apellidos,
                    paciente.Cedula,
                    RegistradoPor = registrador.NombreCompleto,
                    ResueltoPor = resolutor != null ? resolutor.NombreCompleto : null,
                })
            .FirstOrDefaultAsync(cancellationToken);

        if (fila is null)
        {
            return null;
        }

        var p = fila.pago;
        return new PagoDetalle(
            p.Id, p.PacienteId, fila.PacienteNombre, fila.Cedula, p.CitaId, fila.CitaInicio, p.Metodo, p.Estado, p.Monto, p.Concepto,
            p.NumeroComprobante, p.ComprobanteTipo, p.ComprobanteTamanoBytes, fila.RegistradoPor, p.CreadoEn,
            fila.ResueltoPor, p.ResueltoEn, p.MotivoResolucion, []);
    }

    public async Task<PagoConMismoComprobante?> BuscarPorComprobanteAsync(string hashSha256, CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);

        return await (
                from pago in db.Pagos.AsNoTracking()
                where pago.ComprobanteHash == hashSha256 && pago.Estado != EstadoPago.Anulado
                join paciente in db.Pacientes.IgnoreQueryFilters() on pago.PacienteId equals paciente.Id
                select new PagoConMismoComprobante(pago.CreadoEn, paciente.Nombres + " " + paciente.Apellidos))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PaginaResultado<PagoResumen>> BuscarAsync(FiltroPagos filtro, CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);

        var consulta =
            from pago in db.Pagos.AsNoTracking()
            join paciente in db.Pacientes.IgnoreQueryFilters() on pago.PacienteId equals paciente.Id
            select new { pago, paciente };

        var estados = EstadosDe(filtro.Estado);
        if (estados is not null)
        {
            consulta = consulta.Where(x => estados.Contains(x.pago.Estado));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var patron = $"%{EscaparLike(filtro.Texto)}%";
            var digitos = string.Concat(filtro.Texto.Where(char.IsAsciiDigit));
            var buscarPorCedula = digitos.Length >= 3;

            consulta = consulta.Where(x =>
                EF.Functions.ILike(x.paciente.Nombres + " " + x.paciente.Apellidos, patron, CaracterEscape)
                || EF.Functions.ILike(x.paciente.Apellidos + " " + x.paciente.Nombres, patron, CaracterEscape)
                || EF.Functions.ILike(x.pago.Concepto, patron, CaracterEscape)
                || (buscarPorCedula && x.paciente.Cedula.StartsWith(digitos)));
        }

        var total = await consulta.CountAsync(cancellationToken);

        var elementos = await consulta
            .OrderByDescending(x => x.pago.CreadoEn)
            .Skip(filtro.Pagina * filtro.TamanoPagina)
            .Take(filtro.TamanoPagina)
            .Select(x => new PagoResumen(
                x.pago.Id,
                x.pago.PacienteId,
                x.paciente.Nombres + " " + x.paciente.Apellidos,
                x.paciente.Cedula,
                x.pago.Metodo,
                x.pago.Estado,
                x.pago.Monto,
                x.pago.Concepto,
                x.pago.CreadoEn))
            .ToListAsync(cancellationToken);

        return new PaginaResultado<PagoResumen>(elementos, total);
    }

    public async Task<IReadOnlyList<PagoResumen>> ListarPorPacienteAsync(Guid pacienteId, CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);

        return await (
                from pago in db.Pagos.AsNoTracking()
                where pago.PacienteId == pacienteId
                join paciente in db.Pacientes.IgnoreQueryFilters() on pago.PacienteId equals paciente.Id
                orderby pago.CreadoEn descending
                select new PagoResumen(
                    pago.Id, pago.PacienteId, paciente.Nombres + " " + paciente.Apellidos, paciente.Cedula,
                    pago.Metodo, pago.Estado, pago.Monto, pago.Concepto, pago.CreadoEn))
            .Take(MaximoPagosPorPaciente)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PagoResumen>> ListarPorCitaAsync(Guid citaId, CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);

        return await (
                from pago in db.Pagos.AsNoTracking()
                where pago.CitaId == citaId
                join paciente in db.Pacientes.IgnoreQueryFilters() on pago.PacienteId equals paciente.Id
                orderby pago.CreadoEn descending
                select new PagoResumen(
                    pago.Id, pago.PacienteId, paciente.Nombres + " " + paciente.Apellidos, paciente.Cedula,
                    pago.Metodo, pago.Estado, pago.Monto, pago.Concepto, pago.CreadoEn))
            .Take(MaximoPagosPorPaciente)
            .ToListAsync(cancellationToken);
    }

    public async Task AgregarAsync(Pago pago, CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);
        db.Pagos.Add(pago);
        await GuardarAsync(db, cancellationToken);
    }

    public async Task ActualizarAsync(Pago pago, CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);

        var guardado = await db.Pagos.FirstOrDefaultAsync(p => p.Id == pago.Id, cancellationToken)
            ?? throw new ConflictoConcurrenciaException();

        var entrada = db.Entry(guardado);
        entrada.Property(p => p.Version).OriginalValue = pago.Version;
        entrada.CurrentValues.SetValues(pago);

        await GuardarAsync(db, cancellationToken);
    }

    /// <summary>Estados que incluye cada filtro; <c>null</c> = todos.</summary>
    private static EstadoPago[]? EstadosDe(FiltroEstadoPago filtro) => filtro switch
    {
        FiltroEstadoPago.Pendientes => [EstadoPago.PendienteValidacion, EstadoPago.PendienteEnConsultorio],
        FiltroEstadoPago.PorValidar => [EstadoPago.PendienteValidacion],
        FiltroEstadoPago.PorCobrar => [EstadoPago.PendienteEnConsultorio],
        FiltroEstadoPago.Completados => [EstadoPago.Validado, EstadoPago.PagadoEnConsultorio],
        FiltroEstadoPago.Rechazados => [EstadoPago.Rechazado],
        FiltroEstadoPago.Anulados => [EstadoPago.Anulado],
        _ => null,
    };

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
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new RegistroDuplicadoException(ex);
        }
    }

    private static string EscaparLike(string texto) =>
        texto
            .Replace(CaracterEscape, CaracterEscape + CaracterEscape, StringComparison.Ordinal)
            .Replace("%", CaracterEscape + "%", StringComparison.Ordinal)
            .Replace("_", CaracterEscape + "_", StringComparison.Ordinal);
}
