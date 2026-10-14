using Consultorio.Application.Comun;
using Consultorio.Application.Pacientes;
using Consultorio.Domain.Pacientes;

namespace Consultorio.Application.Tests.Pacientes;

public class RegistrarPacienteTests
{
    private readonly RepositorioPacientesEnMemoria repositorio = new();

    private RegistrarPaciente CasoDeUso(UsuarioActualFalso? usuario = null) => new(
        repositorio,
        usuario ?? UsuarioActualFalso.Asistente(),
        new RelojFijo(DatosPrueba.Ahora),
        new OpcionesProteccionDatos { VersionPoliticaPrivacidad = "2026-10" },
        new RegistrarPacienteValidador());

    [Fact]
    public async Task Registra_al_paciente_con_su_consentimiento()
    {
        var resultado = await CasoDeUso().EjecutarAsync(DatosPrueba.ComandoRegistro("171003406-5"), CancellationToken.None);

        Assert.True(resultado.Exito);
        var paciente = Assert.Single(repositorio.Pacientes);
        Assert.Equal(resultado.Valor, paciente.Id);
        Assert.Equal("1710034065", paciente.Cedula);
        Assert.Equal("+593991234567", paciente.Telefono);
        Assert.Equal(DatosPrueba.Ahora, paciente.ConsentimientoAceptadoEn);
        Assert.Equal("2026-10", paciente.ConsentimientoVersion);
        Assert.Equal(OrigenRegistro.Panel, paciente.ConsentimientoOrigen);
    }

    [Fact]
    public async Task Sin_consentimiento_no_registra()
    {
        var comando = DatosPrueba.ComandoRegistro();
        comando.ConsentimientoAceptado = false;

        var resultado = await CasoDeUso().EjecutarAsync(comando, CancellationToken.None);

        Assert.False(resultado.Exito);
        Assert.Contains(resultado.Errores, e => e.Contains("autorización"));
        Assert.Empty(repositorio.Pacientes);
    }

    [Fact]
    public async Task Con_datos_invalidos_devuelve_los_errores()
    {
        var comando = DatosPrueba.ComandoRegistro("1710034064");
        comando.Telefono = "123";

        var resultado = await CasoDeUso().EjecutarAsync(comando, CancellationToken.None);

        Assert.False(resultado.Exito);
        Assert.Equal(2, resultado.Errores.Count);
        Assert.Empty(repositorio.Pacientes);
    }

    [Fact]
    public async Task Rechaza_una_cedula_ya_registrada()
    {
        repositorio.Pacientes.Add(DatosPrueba.Paciente());

        var resultado = await CasoDeUso().EjecutarAsync(DatosPrueba.ComandoRegistro(), CancellationToken.None);

        Assert.False(resultado.Exito);
        Assert.Equal([MensajesPacientes.CedulaYaRegistrada], resultado.Errores);
    }

    [Fact]
    public async Task Avisa_si_la_cedula_es_de_un_paciente_deshabilitado()
    {
        var deshabilitado = DatosPrueba.Paciente();
        deshabilitado.Deshabilitar(DatosPrueba.Ahora);
        repositorio.Pacientes.Add(deshabilitado);

        var resultado = await CasoDeUso().EjecutarAsync(DatosPrueba.ComandoRegistro(), CancellationToken.None);

        Assert.Equal([MensajesPacientes.CedulaDePacienteDeshabilitado], resultado.Errores);
    }

    [Fact]
    public async Task Si_otra_persona_registra_la_misma_cedula_al_mismo_tiempo_devuelve_error_claro()
    {
        repositorio.LanzarDuplicadoAlAgregar = true;

        var resultado = await CasoDeUso().EjecutarAsync(DatosPrueba.ComandoRegistro(), CancellationToken.None);

        Assert.Equal([MensajesPacientes.CedulaYaRegistrada], resultado.Errores);
    }

    [Fact]
    public async Task Sin_sesion_lanza_excepcion()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CasoDeUso(UsuarioActualFalso.SinSesion()).EjecutarAsync(DatosPrueba.ComandoRegistro(), CancellationToken.None));
    }
}
