using Consultorio.Application.Comun;
using Consultorio.Application.Pacientes;
using Consultorio.Domain.Pacientes;
using Consultorio.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Consultorio.Infrastructure.Pacientes;

/// <summary>
/// Implementación del repositorio de pacientes con EF Core + PostgreSQL.
/// Cada método usa su propio DbContext (ver <see cref="FabricaContextos"/>).
/// Las entidades se devuelven "desconectadas"; al actualizar se comparan con la versión guardada.
/// </summary>
internal sealed class RepositorioPacientes(FabricaContextos fabrica) : IRepositorioPacientes
{
    private const string CaracterEscape = @"\";

    public async Task<Paciente?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);
        return await db.Pacientes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Paciente?> ObtenerPorCedulaAsync(string cedula, CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);
        return await db.Pacientes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Cedula == cedula, cancellationToken);
    }

    public async Task<PaginaResultado<PacienteResumen>> BuscarAsync(FiltroPacientes filtro, CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);

        var consulta = filtro.IncluirDeshabilitados
            ? db.Pacientes.IgnoreQueryFilters().AsNoTracking()
            : db.Pacientes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            // ILIKE = búsqueda sin distinguir mayúsculas. Se escapan % y _ para que se busquen literalmente.
            var patron = $"%{EscaparLike(filtro.Texto)}%";
            var digitos = string.Concat(filtro.Texto.Where(char.IsAsciiDigit));
            var buscarPorNumero = digitos.Length >= 3;
            // En la base el teléfono está como +593..., así que se ignora el 0 inicial de lo que se escribió.
            var digitosTelefono = digitos.TrimStart('0');

            consulta = consulta.Where(p =>
                EF.Functions.ILike(p.Nombres + " " + p.Apellidos, patron, CaracterEscape)
                || EF.Functions.ILike(p.Apellidos + " " + p.Nombres, patron, CaracterEscape)
                || (buscarPorNumero && p.Cedula.StartsWith(digitos))
                || (buscarPorNumero && digitosTelefono.Length > 0 && p.Telefono.Contains(digitosTelefono)));
        }

        var total = await consulta.CountAsync(cancellationToken);

        var elementos = await consulta
            .OrderBy(p => p.Apellidos)
            .ThenBy(p => p.Nombres)
            .Skip(filtro.Pagina * filtro.TamanoPagina)
            .Take(filtro.TamanoPagina)
            .Select(p => new PacienteResumen(
                p.Id,
                p.Cedula,
                p.Nombres,
                p.Apellidos,
                p.Telefono,
                p.CorreoElectronico,
                p.CreadoEn,
                p.DeshabilitadoEn != null))
            .ToListAsync(cancellationToken);

        return new PaginaResultado<PacienteResumen>(elementos, total);
    }

    public async Task AgregarAsync(Paciente paciente, CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);
        db.Pacientes.Add(paciente);
        await GuardarAsync(db, cancellationToken);
    }

    public async Task ActualizarAsync(Paciente paciente, CancellationToken cancellationToken)
    {
        await using var db = await fabrica.CrearAsync(cancellationToken);

        var guardado = await db.Pacientes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == paciente.Id, cancellationToken)
            ?? throw new ConflictoConcurrenciaException();

        var entrada = db.Entry(guardado);

        // La versión que se leyó al empezar: si en la base ya es otra, PostgreSQL no actualiza nada
        // y EF lanza DbUpdateConcurrencyException.
        entrada.Property(p => p.Version).OriginalValue = paciente.Version;

        // Copia los valores nuevos; EF marca como modificados solo los campos que realmente cambiaron
        // (eso es lo que se registra en la auditoría).
        entrada.CurrentValues.SetValues(paciente);

        await GuardarAsync(db, cancellationToken);
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
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new RegistroDuplicadoException(ex);
        }
    }

    private static string EscaparLike(string texto) =>
        texto
            .Replace(CaracterEscape, $"{CaracterEscape}{CaracterEscape}", StringComparison.Ordinal)
            .Replace("%", $"{CaracterEscape}%", StringComparison.Ordinal)
            .Replace("_", $"{CaracterEscape}_", StringComparison.Ordinal);
}
