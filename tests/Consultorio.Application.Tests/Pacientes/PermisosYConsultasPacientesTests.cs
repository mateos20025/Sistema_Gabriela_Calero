using Consultorio.Application.Pacientes;

namespace Consultorio.Application.Tests.Pacientes;

public class PermisosYConsultasPacientesTests
{
    private readonly RepositorioPacientesEnMemoria repositorio = new();
    private readonly RelojFijo reloj = new(DatosPrueba.Ahora);

    public static TheoryData<string> Personal => ["Doctora", "Asistente"];

    private static UsuarioActualFalso Usuario(string rol) =>
        rol == "Doctora" ? UsuarioActualFalso.Doctora() : UsuarioActualFalso.Asistente();

    [Theory]
    [MemberData(nameof(Personal))]
    public async Task El_personal_deshabilita_y_habilita_pacientes(string rol)
    {
        var paciente = DatosPrueba.Paciente();
        repositorio.Pacientes.Add(paciente);
        var usuario = Usuario(rol);

        var deshabilitar = await new DeshabilitarPaciente(repositorio, usuario, reloj).EjecutarAsync(paciente.Id, CancellationToken.None);
        Assert.True(deshabilitar.Exito);
        Assert.Equal(DatosPrueba.Ahora, paciente.DeshabilitadoEn);

        var otraVez = await new DeshabilitarPaciente(repositorio, usuario, reloj).EjecutarAsync(paciente.Id, CancellationToken.None);
        Assert.Equal([MensajesPacientes.YaEstabaDeshabilitado], otraVez.Errores);

        var habilitar = await new HabilitarPaciente(repositorio, usuario).EjecutarAsync(paciente.Id, CancellationToken.None);
        Assert.True(habilitar.Exito);
        Assert.False(paciente.EstaDeshabilitado);
    }

    [Fact]
    public async Task Sin_sesion_no_se_puede_deshabilitar()
    {
        var paciente = DatosPrueba.Paciente();
        repositorio.Pacientes.Add(paciente);
        var casoDeUso = new DeshabilitarPaciente(repositorio, UsuarioActualFalso.SinSesion(), reloj);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => casoDeUso.EjecutarAsync(paciente.Id, CancellationToken.None));
        Assert.False(paciente.EstaDeshabilitado);
    }

    [Fact]
    public async Task No_se_habilita_un_paciente_que_ya_esta_habilitado()
    {
        var paciente = DatosPrueba.Paciente();
        repositorio.Pacientes.Add(paciente);

        var resultado = await new HabilitarPaciente(repositorio, UsuarioActualFalso.Asistente()).EjecutarAsync(paciente.Id, CancellationToken.None);

        Assert.Equal([MensajesPacientes.YaEstabaHabilitado], resultado.Errores);
    }

    [Fact]
    public async Task Deshabilitar_avisa_si_otra_persona_lo_modifico()
    {
        var paciente = DatosPrueba.Paciente();
        repositorio.Pacientes.Add(paciente);
        repositorio.LanzarConflictoAlActualizar = true;

        var resultado = await new DeshabilitarPaciente(repositorio, UsuarioActualFalso.Asistente(), reloj)
            .EjecutarAsync(paciente.Id, CancellationToken.None);

        Assert.Equal([MensajesPacientes.ModificadoPorOtraPersona], resultado.Errores);
    }

    [Fact]
    public async Task La_busqueda_aplica_limites_del_lado_del_servidor()
    {
        var casoDeUso = new BuscarPacientes(repositorio, UsuarioActualFalso.Asistente());
        var textoLargo = "  " + new string('a', 300) + "  ";

        await casoDeUso.EjecutarAsync(new FiltroPacientes(textoLargo, IncluirDeshabilitados: true, Pagina: -3, TamanoPagina: 5000), CancellationToken.None);

        var filtro = repositorio.UltimoFiltro!;
        Assert.True(filtro.IncluirDeshabilitados); // la asistente también puede verlos para habilitarlos
        Assert.Equal(0, filtro.Pagina);
        Assert.Equal(FiltroPacientes.TamanoPaginaMaximo, filtro.TamanoPagina);
        Assert.Equal(FiltroPacientes.LongitudMaximaTexto, filtro.Texto!.Length);
    }

    [Fact]
    public async Task Un_texto_vacio_no_filtra()
    {
        await new BuscarPacientes(repositorio, UsuarioActualFalso.Doctora())
            .EjecutarAsync(new FiltroPacientes("   ", IncluirDeshabilitados: false, Pagina: 0, TamanoPagina: 20), CancellationToken.None);

        Assert.Null(repositorio.UltimoFiltro!.Texto);
    }

    [Fact]
    public async Task La_asistente_ve_pacientes_deshabilitados_pero_no_el_historial()
    {
        var deshabilitado = DatosPrueba.Paciente();
        deshabilitado.Deshabilitar(DatosPrueba.Ahora);
        repositorio.Pacientes.Add(deshabilitado);
        var auditoria = new ConsultaAuditoriaFalsa();

        var ficha = await new ObtenerPaciente(repositorio, auditoria, UsuarioActualFalso.Asistente())
            .EjecutarAsync(deshabilitado.Id, CancellationToken.None);

        Assert.NotNull(ficha);
        Assert.True(ficha.Deshabilitado);
        Assert.Empty(ficha.Historial);
        Assert.False(auditoria.FueConsultada);
    }

    [Fact]
    public async Task La_doctora_ve_el_historial_de_cambios()
    {
        var paciente = DatosPrueba.Paciente();
        repositorio.Pacientes.Add(paciente);

        var ficha = await new ObtenerPaciente(repositorio, new ConsultaAuditoriaFalsa(), UsuarioActualFalso.Doctora())
            .EjecutarAsync(paciente.Id, CancellationToken.None);

        Assert.Single(ficha!.Historial);
    }

    [Fact]
    public async Task Ficha_de_un_paciente_que_no_existe_es_nula()
    {
        var ficha = await new ObtenerPaciente(repositorio, new ConsultaAuditoriaFalsa(), UsuarioActualFalso.Doctora())
            .EjecutarAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(ficha);
    }
}
