using Consultorio.Application.Pacientes;

namespace Consultorio.Application.Tests.Pacientes;

public class ActualizarPacienteTests
{
    private readonly RepositorioPacientesEnMemoria repositorio = new();

    private ActualizarPaciente CasoDeUso() =>
        new(repositorio, UsuarioActualFalso.Asistente(), new ActualizarPacienteValidador());

    private static ActualizarPacienteComando Comando(Domain.Pacientes.Paciente paciente, string cedula = DatosPrueba.CedulaValida) => new()
    {
        Id = paciente.Id,
        Version = paciente.Version,
        Cedula = cedula,
        Nombres = "Ana",
        Apellidos = "Ruiz",
        Telefono = "022345678",
        CorreoElectronico = "ana@correo.com",
    };

    [Fact]
    public async Task Actualiza_los_datos()
    {
        var paciente = DatosPrueba.Paciente();
        repositorio.Pacientes.Add(paciente);

        var resultado = await CasoDeUso().EjecutarAsync(Comando(paciente), CancellationToken.None);

        Assert.True(resultado.Exito);
        Assert.Equal(1, repositorio.Actualizaciones);
        Assert.Equal("Ana Ruiz", paciente.NombreCompleto);
        Assert.Equal("+59322345678", paciente.Telefono);
        Assert.Equal("ana@correo.com", paciente.CorreoElectronico);
    }

    [Fact]
    public async Task Detecta_que_la_pantalla_tenia_una_version_anterior()
    {
        var paciente = DatosPrueba.Paciente();
        repositorio.Pacientes.Add(paciente);
        var comando = Comando(paciente);
        comando.Version = paciente.Version + 1;

        var resultado = await CasoDeUso().EjecutarAsync(comando, CancellationToken.None);

        Assert.Equal([MensajesPacientes.ModificadoPorOtraPersona], resultado.Errores);
        Assert.Equal(0, repositorio.Actualizaciones);
    }

    [Fact]
    public async Task Detecta_un_cambio_simultaneo_al_guardar()
    {
        var paciente = DatosPrueba.Paciente();
        repositorio.Pacientes.Add(paciente);
        repositorio.LanzarConflictoAlActualizar = true;

        var resultado = await CasoDeUso().EjecutarAsync(Comando(paciente), CancellationToken.None);

        Assert.Equal([MensajesPacientes.ModificadoPorOtraPersona], resultado.Errores);
    }

    [Fact]
    public async Task No_permite_usar_la_cedula_de_otro_paciente()
    {
        var paciente = DatosPrueba.Paciente();
        repositorio.Pacientes.Add(paciente);
        repositorio.Pacientes.Add(DatosPrueba.Paciente(DatosPrueba.OtraCedulaValida));

        var resultado = await CasoDeUso().EjecutarAsync(Comando(paciente, DatosPrueba.OtraCedulaValida), CancellationToken.None);

        Assert.Equal([MensajesPacientes.CedulaYaRegistrada], resultado.Errores);
    }

    [Fact]
    public async Task No_modifica_pacientes_deshabilitados()
    {
        var paciente = DatosPrueba.Paciente();
        paciente.Deshabilitar(DatosPrueba.Ahora);
        repositorio.Pacientes.Add(paciente);

        var resultado = await CasoDeUso().EjecutarAsync(Comando(paciente), CancellationToken.None);

        Assert.Equal([MensajesPacientes.EstaDeshabilitado], resultado.Errores);
    }

    [Fact]
    public async Task Avisa_si_el_paciente_no_existe()
    {
        var resultado = await CasoDeUso().EjecutarAsync(Comando(DatosPrueba.Paciente()), CancellationToken.None);

        Assert.Equal([MensajesPacientes.NoExiste], resultado.Errores);
    }
}
