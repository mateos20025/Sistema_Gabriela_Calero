using Consultorio.Application.Citas;
using Consultorio.Application.Comun;
using Consultorio.Application.Tests.Pacientes;
using Consultorio.Domain.Citas;
using Consultorio.Domain.Pacientes;

namespace Consultorio.Application.Tests.Citas;

/// <summary>
/// Repositorio de citas en memoria. Imita la restricción de exclusión de PostgreSQL:
/// no deja guardar dos citas activas que se crucen.
/// </summary>
internal sealed class RepositorioCitasEnMemoria : IRepositorioCitas
{
    public List<Cita> Citas { get; } = [];

    public bool LanzarConflictoAlActualizar { get; set; }

    /// <summary>Simula que otra persona tomó el horario entre la comprobación y el guardado.</summary>
    public bool LanzarHorarioOcupadoAlAgregar { get; set; }

    public int Actualizaciones { get; private set; }

    public Task<Cita?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Citas.FirstOrDefault(c => c.Id == id));

    public Task<CitaDetalle?> ObtenerDetalleAsync(Guid id, CancellationToken cancellationToken)
    {
        var cita = Citas.FirstOrDefault(c => c.Id == id);
        return Task.FromResult(cita is null
            ? null
            : new CitaDetalle(
                cita.Id, cita.PacienteId, "María Pérez", DatosPrueba.CedulaValida, "+593991234567", false, cita.Inicio, cita.Fin,
                cita.Estado, cita.Origen, cita.ReservaExpiraEn, cita.ContactoNombre, cita.ContactoTelefono, cita.ContactoCorreo,
                cita.CreadoEn, []));
    }

    public Task<IReadOnlyList<IntervaloUtc>> ListarHorariosOcupadosAsync(
        DateTime desdeUtc, DateTime hastaUtc, DateTime ahoraUtc, Guid? excluirCitaId, CancellationToken cancellationToken)
    {
        IReadOnlyList<IntervaloUtc> ocupados =
        [
            .. Citas
                .Where(c => c.EstaActiva && !c.ReservaVencida(ahoraUtc) && c.Id != excluirCitaId)
                .Where(c => c.Inicio < hastaUtc && desdeUtc < c.Fin)
                .Select(c => c.Horario),
        ];
        return Task.FromResult(ocupados);
    }

    public Task<int> ContarActivasDePacienteAsync(Guid pacienteId, DateTime ahoraUtc, CancellationToken cancellationToken) =>
        Task.FromResult(Citas.Count(c => c.PacienteId == pacienteId && c.EstaActiva && !c.ReservaVencida(ahoraUtc) && c.Fin > ahoraUtc));

    public Task<IReadOnlyList<CitaResumen>> ListarEnRangoAsync(DateTime desdeUtc, DateTime hastaUtc, CancellationToken cancellationToken) =>
        Resumenes(c => c.Inicio < hastaUtc && desdeUtc < c.Fin);

    public Task<IReadOnlyList<CitaResumen>> ListarPorEstadosAsync(IReadOnlyList<EstadoCita> estados, CancellationToken cancellationToken) =>
        Resumenes(c => estados.Contains(c.Estado));

    public Task<IReadOnlyList<CitaResumen>> ListarPorPacienteAsync(Guid pacienteId, CancellationToken cancellationToken) =>
        Resumenes(c => c.PacienteId == pacienteId);

    public Task<IReadOnlyList<Cita>> ListarReservasVencidasAsync(DateTime ahoraUtc, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Cita>>([.. Citas.Where(c => c.ReservaVencida(ahoraUtc))]);

    public Task AgregarAsync(Cita cita, CancellationToken cancellationToken)
    {
        if (LanzarHorarioOcupadoAlAgregar || SeCruzaConOtraActiva(cita))
        {
            throw new HorarioOcupadoException();
        }

        Citas.Add(cita);
        return Task.CompletedTask;
    }

    public Task ActualizarAsync(Cita cita, CancellationToken cancellationToken)
    {
        if (LanzarConflictoAlActualizar)
        {
            throw new ConflictoConcurrenciaException();
        }

        if (SeCruzaConOtraActiva(cita))
        {
            throw new HorarioOcupadoException();
        }

        Actualizaciones++;
        return Task.CompletedTask;
    }

    private bool SeCruzaConOtraActiva(Cita cita) =>
        cita.EstaActiva && Citas.Any(o => o.Id != cita.Id && o.EstaActiva && o.Horario.SeCruzaCon(cita.Horario));

    private Task<IReadOnlyList<CitaResumen>> Resumenes(Func<Cita, bool> filtro)
    {
        IReadOnlyList<CitaResumen> lista =
        [
            .. Citas.Where(filtro).Select(c => new CitaResumen(
                c.Id, c.PacienteId, "María Pérez", DatosPrueba.CedulaValida, "+593991234567", c.ContactoTelefono,
                c.Inicio, c.Fin, c.Estado, c.Origen, c.ReservaExpiraEn, c.CreadoEn)),
        ];
        return Task.FromResult(lista);
    }
}

internal sealed class RepositorioAgendaEnMemoria : IRepositorioAgenda
{
    public ConfiguracionAgenda? Configuracion { get; set; }

    public List<FranjaAtencion> Franjas { get; set; } = [];

    public List<BloqueoAgenda> Bloqueos { get; } = [];

    public int Guardados { get; private set; }

    public bool LanzarConflictoAlGuardar { get; set; }

    public Task<AgendaConfigurada?> ObtenerAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Configuracion is null ? null : new AgendaConfigurada(Configuracion, Franjas));

    public Task GuardarAsync(ConfiguracionAgenda configuracion, IReadOnlyList<FranjaAtencion> franjas, CancellationToken cancellationToken)
    {
        if (LanzarConflictoAlGuardar)
        {
            throw new ConflictoConcurrenciaException();
        }

        Configuracion = configuracion;
        Franjas = [.. franjas];
        Guardados++;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<BloqueoAgenda>> ListarBloqueosAsync(DateTime desdeUtc, DateTime hastaUtc, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<BloqueoAgenda>>([.. Bloqueos.Where(b => b.Inicio < hastaUtc && desdeUtc < b.Fin)]);

    public Task AgregarBloqueoAsync(BloqueoAgenda bloqueo, CancellationToken cancellationToken)
    {
        Bloqueos.Add(bloqueo);
        return Task.CompletedTask;
    }

    public Task<bool> EliminarBloqueoAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Bloqueos.RemoveAll(b => b.Id == id) > 0);
}

internal sealed class VerificadorHumanoFalso(bool habilitado = true, bool aprueba = true) : IVerificadorHumano
{
    public bool Habilitado { get; } = habilitado;

    public string? ClaveSitio => Habilitado ? "clave-de-prueba" : null;

    public int Verificaciones { get; private set; }

    public Task<bool> VerificarAsync(string? respuesta, string? ipCliente, CancellationToken cancellationToken)
    {
        Verificaciones++;
        return Task.FromResult(aprueba && !string.IsNullOrEmpty(respuesta));
    }
}

/// <summary>
/// Escenario común: hoy es jueves 15/10/2026 07:00 en Ecuador; se atiende los lunes de 09:00 a 12:00,
/// citas de 30 minutos, sin anticipación mínima, hasta 30 días adelante, reserva web de 24 horas.
/// </summary>
internal static class DatosCitas
{
    public static readonly DateTime Ahora = new(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);

    public static readonly DateOnly Lunes = new(2026, 10, 19);

    /// <summary>Hora de Ecuador del lunes de prueba, en UTC.</summary>
    public static DateTime LunesA(int hora, int minuto = 0) => new(2026, 10, 19, hora + 5, minuto, 0, DateTimeKind.Utc);

    public static IntervaloUtc HorarioLunes(int hora, int minuto = 0) =>
        new(LunesA(hora, minuto), LunesA(hora, minuto).AddMinutes(30));

    public static ConfiguracionAgenda Configuracion(bool web = true, int maximoActivas = 2) =>
        ConfiguracionAgenda.Crear(30, 0, 30, 24, maximoActivas, web);

    public static List<FranjaAtencion> Franjas() =>
        [FranjaAtencion.Crear(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0))];

    public static RepositorioAgendaEnMemoria AgendaConfigurada(bool web = true, int maximoActivas = 2) =>
        new() { Configuracion = Configuracion(web, maximoActivas), Franjas = Franjas() };

    public static Cita Confirmada(Guid pacienteId, IntervaloUtc horario) =>
        Cita.AgendarDesdePanel(pacienteId, horario, Guid.NewGuid(), Ahora, null);

    public static Cita Solicitada(Guid pacienteId, IntervaloUtc horario, DateTime? creadaUtc = null) =>
        Cita.SolicitarDesdeWeb(
            pacienteId, horario, creadaUtc ?? Ahora, 24, new ContactoSolicitud("Ana", "Torres", "0987654321", null));

    public static Paciente Paciente(string cedula = DatosPrueba.CedulaValida) => DatosPrueba.Paciente(cedula);
}
