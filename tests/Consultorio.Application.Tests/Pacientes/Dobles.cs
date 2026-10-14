using Consultorio.Application.Abstracciones;
using Consultorio.Application.Auditoria;
using Consultorio.Application.Comun;
using Consultorio.Application.Pacientes;
using Consultorio.Domain.Pacientes;

namespace Consultorio.Application.Tests.Pacientes;

// "Dobles de prueba": implementaciones simples en memoria de los puertos, para probar los casos de uso
// sin base de datos ni navegador.

internal sealed class RepositorioPacientesEnMemoria : IRepositorioPacientes
{
    public List<Paciente> Pacientes { get; } = [];

    public FiltroPacientes? UltimoFiltro { get; private set; }

    public bool LanzarDuplicadoAlAgregar { get; set; }

    public bool LanzarConflictoAlActualizar { get; set; }

    public int Actualizaciones { get; private set; }

    public Task<Paciente?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Pacientes.FirstOrDefault(p => p.Id == id));

    public Task<Paciente?> ObtenerPorCedulaAsync(string cedula, CancellationToken cancellationToken) =>
        Task.FromResult(Pacientes.FirstOrDefault(p => p.Cedula == cedula));

    public Task<PaginaResultado<PacienteResumen>> BuscarAsync(FiltroPacientes filtro, CancellationToken cancellationToken)
    {
        UltimoFiltro = filtro;
        return Task.FromResult(new PaginaResultado<PacienteResumen>([], 0));
    }

    public Task AgregarAsync(Paciente paciente, CancellationToken cancellationToken)
    {
        if (LanzarDuplicadoAlAgregar)
        {
            throw new RegistroDuplicadoException();
        }

        Pacientes.Add(paciente);
        return Task.CompletedTask;
    }

    public Task ActualizarAsync(Paciente paciente, CancellationToken cancellationToken)
    {
        if (LanzarConflictoAlActualizar)
        {
            throw new ConflictoConcurrenciaException();
        }

        Actualizaciones++;
        return Task.CompletedTask;
    }
}

internal sealed class UsuarioActualFalso(UsuarioSesion? sesion) : IUsuarioActual
{
    public static UsuarioActualFalso Doctora() => new(new UsuarioSesion(Guid.NewGuid(), "Gabriela Calero", EsDoctora: true));

    public static UsuarioActualFalso Asistente() => new(new UsuarioSesion(Guid.NewGuid(), "María Pérez", EsDoctora: false));

    public static UsuarioActualFalso SinSesion() => new(null);

    public Task<UsuarioSesion?> ObtenerAsync() => Task.FromResult(sesion);
}

internal sealed class RelojFijo(DateTime ahoraUtc) : IRelojSistema
{
    public DateTime AhoraUtc { get; } = ahoraUtc;
}

internal sealed class ConsultaAuditoriaFalsa : IConsultaAuditoria
{
    public bool FueConsultada { get; private set; }

    public Task<IReadOnlyList<EventoAuditoria>> ObtenerHistorialAsync(string entidad, Guid entidadId, CancellationToken cancellationToken)
    {
        FueConsultada = true;
        IReadOnlyList<EventoAuditoria> historial = [new(DateTime.UtcNow, "Gabriela Calero", AccionAuditoria.Creado, [])];
        return Task.FromResult(historial);
    }
}

internal static class DatosPrueba
{
    public static readonly DateTime Ahora = new(2026, 10, 13, 15, 0, 0, DateTimeKind.Utc);

    public const string CedulaValida = "1710034065";
    public const string OtraCedulaValida = "0102030400";

    public static Paciente Paciente(string cedula = CedulaValida) =>
        Domain.Pacientes.Paciente.Registrar(
            cedula, "María", "Pérez", "0991234567", null, new ConsentimientoDatos(Ahora, "1.0", OrigenRegistro.Panel));

    public static RegistrarPacienteComando ComandoRegistro(string cedula = CedulaValida) => new()
    {
        Cedula = cedula,
        Nombres = "María José",
        Apellidos = "Pérez Ruiz",
        Telefono = "099 123 4567",
        CorreoElectronico = "",
        ConsentimientoAceptado = true,
    };
}
