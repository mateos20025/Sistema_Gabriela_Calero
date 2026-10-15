using System.Security.Cryptography;
using Consultorio.Application.Pagos;
using Consultorio.Application.Tests.Citas;
using Consultorio.Application.Tests.Pacientes;
using Consultorio.Domain.Pagos;

namespace Consultorio.Application.Tests.Pagos;

public class RegistrarPagoTests
{
    private readonly RepositorioPagosEnMemoria pagos = new();
    private readonly RepositorioPacientesEnMemoria pacientes = new();
    private readonly AlmacenArchivosEnMemoria almacen = new();
    private readonly RepositorioCitasEnMemoria citas = new();
    private readonly Domain.Pacientes.Paciente paciente = DatosPrueba.Paciente();

    public RegistrarPagoTests() => pacientes.Pacientes.Add(paciente);

    private RegistrarPago CasoDeUso(UsuarioActualFalso? usuario = null) =>
        new(pagos, pacientes, citas, almacen, usuario ?? UsuarioActualFalso.Asistente(), new RegistrarPagoValidador());

    private RegistrarPagoComando Comando(MetodoPago metodo = MetodoPago.Transferencia, string monto = "35,50") => new()
    {
        PacienteId = paciente.Id,
        Metodo = metodo,
        Monto = monto,
        Concepto = "Consulta médica",
        NumeroComprobante = "",
    };

    private static ArchivoSubido Archivo(byte[] contenido) => new("captura.png", contenido);

    [Fact]
    public async Task Registra_una_transferencia_y_guarda_la_captura()
    {
        var contenido = ArchivosPrueba.Png();

        var resultado = await CasoDeUso().EjecutarAsync(Comando(), Archivo(contenido), CancellationToken.None);

        Assert.True(resultado.Exito);
        var pago = Assert.Single(pagos.Pagos);
        Assert.Equal(resultado.Valor, pago.Id);
        Assert.Equal(35.50m, pago.Monto);
        Assert.Equal(EstadoPago.PendienteValidacion, pago.Estado);
        Assert.Equal(TipoArchivoComprobante.Png, pago.ComprobanteTipo);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(contenido)), pago.ComprobanteHash);
        Assert.True(almacen.Archivos.ContainsKey(pago.ComprobanteArchivoId!));
    }

    [Fact]
    public async Task Registra_un_pago_en_efectivo_sin_captura()
    {
        var resultado = await CasoDeUso().EjecutarAsync(Comando(MetodoPago.Efectivo, "40"), null, CancellationToken.None);

        Assert.True(resultado.Exito);
        Assert.Equal(EstadoPago.PendienteEnConsultorio, Assert.Single(pagos.Pagos).Estado);
        Assert.Empty(almacen.Archivos);
    }

    [Fact]
    public async Task Un_pago_en_efectivo_no_admite_captura()
    {
        var resultado = await CasoDeUso().EjecutarAsync(Comando(MetodoPago.Efectivo), Archivo(ArchivosPrueba.Png()), CancellationToken.None);

        Assert.Equal([MensajesPagos.EfectivoSinComprobante], resultado.Errores);
    }

    [Fact]
    public async Task La_transferencia_exige_captura()
    {
        var resultado = await CasoDeUso().EjecutarAsync(Comando(), null, CancellationToken.None);

        Assert.Equal([MensajesPagos.ComprobanteObligatorio], resultado.Errores);
    }

    [Fact]
    public async Task Rechaza_archivos_que_no_son_jpg_png_ni_pdf_aunque_se_llamen_png()
    {
        var resultado = await CasoDeUso().EjecutarAsync(Comando(), Archivo(ArchivosPrueba.Gif()), CancellationToken.None);

        Assert.Equal([MensajesPagos.TipoNoPermitido], resultado.Errores);
        Assert.Empty(almacen.Archivos);
    }

    [Fact]
    public async Task Rechaza_capturas_de_mas_de_5_MB()
    {
        var grande = new byte[ArchivosComprobante.TamanoMaximoBytes + 1];
        ArchivosPrueba.Png().CopyTo(grande, 0);

        var resultado = await CasoDeUso().EjecutarAsync(Comando(), Archivo(grande), CancellationToken.None);

        Assert.Equal([MensajesPagos.ComprobanteMuyGrande], resultado.Errores);
    }

    [Fact]
    public async Task Detecta_una_captura_repetida()
    {
        await CasoDeUso().EjecutarAsync(Comando(), Archivo(ArchivosPrueba.Pdf()), CancellationToken.None);

        var resultado = await CasoDeUso().EjecutarAsync(Comando(), Archivo(ArchivosPrueba.Pdf()), CancellationToken.None);

        Assert.False(resultado.Exito);
        Assert.StartsWith("Esta captura ya se registró el", resultado.Errores[0]);
        Assert.Single(pagos.Pagos);
        Assert.Single(almacen.Archivos);
    }

    [Fact]
    public async Task Una_captura_de_un_pago_anulado_se_puede_volver_a_usar()
    {
        await CasoDeUso().EjecutarAsync(Comando(), Archivo(ArchivosPrueba.Pdf()), CancellationToken.None);
        pagos.Pagos[0].Anular(Guid.NewGuid(), DatosPrueba.Ahora, "Se registró al paciente equivocado");

        var resultado = await CasoDeUso().EjecutarAsync(Comando(), Archivo(ArchivosPrueba.Pdf()), CancellationToken.None);

        Assert.True(resultado.Exito);
    }

    [Fact]
    public async Task Si_la_base_rechaza_el_duplicado_elimina_el_archivo()
    {
        pagos.LanzarDuplicadoAlAgregar = true;

        var resultado = await CasoDeUso().EjecutarAsync(Comando(), Archivo(ArchivosPrueba.Png()), CancellationToken.None);

        Assert.Equal([MensajesPagos.ComprobanteYaRegistrado], resultado.Errores);
        Assert.Empty(almacen.Archivos);
        Assert.Equal(1, almacen.Eliminados);
    }

    [Fact]
    public async Task Si_falla_el_guardado_no_deja_archivos_huerfanos()
    {
        pagos.ExcepcionAlAgregar = new InvalidOperationException("Base de datos caída");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CasoDeUso().EjecutarAsync(Comando(), Archivo(ArchivosPrueba.Png()), CancellationToken.None));

        Assert.Empty(almacen.Archivos);
    }

    [Fact]
    public async Task No_registra_pagos_de_pacientes_deshabilitados()
    {
        paciente.Deshabilitar(DatosPrueba.Ahora);

        var resultado = await CasoDeUso().EjecutarAsync(Comando(MetodoPago.Efectivo), null, CancellationToken.None);

        Assert.Equal([MensajesPagos.PacienteDeshabilitado], resultado.Errores);
    }

    [Fact]
    public async Task Avisa_si_el_paciente_no_existe()
    {
        var comando = Comando(MetodoPago.Efectivo);
        comando.PacienteId = Guid.NewGuid();

        var resultado = await CasoDeUso().EjecutarAsync(comando, null, CancellationToken.None);

        Assert.Equal([MensajesPagos.PacienteNoExiste], resultado.Errores);
    }

    [Theory]
    [InlineData("1.234")]
    [InlineData("0")]
    [InlineData("abc")]
    public async Task Rechaza_montos_invalidos(string monto)
    {
        var resultado = await CasoDeUso().EjecutarAsync(Comando(MetodoPago.Efectivo, monto), null, CancellationToken.None);

        Assert.False(resultado.Exito);
        Assert.Empty(pagos.Pagos);
    }

    [Fact]
    public async Task Asocia_el_pago_a_una_cita_del_mismo_paciente()
    {
        var cita = DatosCitas.Confirmada(paciente.Id, DatosCitas.HorarioLunes(9));
        citas.Citas.Add(cita);
        var comando = Comando(MetodoPago.Efectivo);
        comando.CitaId = cita.Id;

        var resultado = await CasoDeUso().EjecutarAsync(comando, null, CancellationToken.None);

        Assert.True(resultado.Exito);
        Assert.Equal(cita.Id, Assert.Single(pagos.Pagos).CitaId);
    }

    [Fact]
    public async Task No_acepta_una_cita_de_otro_paciente()
    {
        var cita = DatosCitas.Confirmada(Guid.NewGuid(), DatosCitas.HorarioLunes(9));
        citas.Citas.Add(cita);
        var comando = Comando(MetodoPago.Efectivo);
        comando.CitaId = cita.Id;

        var resultado = await CasoDeUso().EjecutarAsync(comando, null, CancellationToken.None);

        Assert.Equal([MensajesPagos.CitaNoCorresponde], resultado.Errores);
        Assert.Empty(pagos.Pagos);
    }

    [Fact]
    public async Task No_acepta_una_cita_que_no_existe()
    {
        var comando = Comando(MetodoPago.Efectivo);
        comando.CitaId = Guid.NewGuid();

        var resultado = await CasoDeUso().EjecutarAsync(comando, null, CancellationToken.None);

        Assert.Equal([MensajesPagos.CitaNoCorresponde], resultado.Errores);
    }

    [Fact]
    public async Task Sin_sesion_lanza_excepcion()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CasoDeUso(UsuarioActualFalso.SinSesion()).EjecutarAsync(Comando(MetodoPago.Efectivo), null, CancellationToken.None));
    }
}
