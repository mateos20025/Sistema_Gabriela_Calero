using Consultorio.Application.Pagos;
using Consultorio.Application.Tests.Pacientes;
using Consultorio.Domain.Pagos;

namespace Consultorio.Application.Tests.Pagos;

public class EstadosYConfiguracionPagosTests
{
    private readonly RepositorioPagosEnMemoria pagos = new();
    private readonly RelojFijo reloj = new(DatosPrueba.Ahora);
    private readonly UsuarioActualFalso asistente = UsuarioActualFalso.Asistente();

    private Pago Transferencia()
    {
        var comprobante = new ComprobanteTransferencia("comprobantes/2026/10/a.png", TipoArchivoComprobante.Png, 100, new string('b', 64));
        var pago = Pago.RegistrarTransferencia(Guid.NewGuid(), 35m, "Consulta", null, comprobante, Guid.NewGuid());
        pagos.Pagos.Add(pago);
        return pago;
    }

    private Pago Efectivo()
    {
        var pago = Pago.RegistrarEfectivo(Guid.NewGuid(), 35m, "Consulta", Guid.NewGuid());
        pagos.Pagos.Add(pago);
        return pago;
    }

    [Fact]
    public async Task La_asistente_valida_una_transferencia_y_queda_registrado_quien_y_cuando()
    {
        var pago = Transferencia();
        var sesion = (await asistente.ObtenerAsync())!;

        var resultado = await new ValidarPago(pagos, asistente, reloj).EjecutarAsync(pago.Id, CancellationToken.None);

        Assert.True(resultado.Exito);
        Assert.Equal(EstadoPago.Validado, pago.Estado);
        Assert.Equal(sesion.Id, pago.ResueltoPorId);
        Assert.Equal(DatosPrueba.Ahora, pago.ResueltoEn);

        var otraVez = await new ValidarPago(pagos, asistente, reloj).EjecutarAsync(pago.Id, CancellationToken.None);
        Assert.Equal([MensajesPagos.NoEstaPorValidar], otraVez.Errores);
    }

    [Fact]
    public async Task Rechazar_exige_motivo()
    {
        var pago = Transferencia();

        var sinMotivo = await new RechazarPago(pagos, asistente, reloj).EjecutarAsync(pago.Id, "  ", CancellationToken.None);
        Assert.Equal([MensajesPagos.MotivoObligatorio], sinMotivo.Errores);
        Assert.Equal(EstadoPago.PendienteValidacion, pago.Estado);

        var conMotivo = await new RechazarPago(pagos, asistente, reloj).EjecutarAsync(pago.Id, "No llegó al banco", CancellationToken.None);
        Assert.True(conMotivo.Exito);
        Assert.Equal(EstadoPago.Rechazado, pago.Estado);
    }

    [Fact]
    public async Task Solo_el_efectivo_se_marca_como_recibido()
    {
        var transferencia = Transferencia();
        var efectivo = Efectivo();

        var mal = await new MarcarPagoRecibido(pagos, asistente, reloj).EjecutarAsync(transferencia.Id, CancellationToken.None);
        var bien = await new MarcarPagoRecibido(pagos, asistente, reloj).EjecutarAsync(efectivo.Id, CancellationToken.None);

        Assert.Equal([MensajesPagos.NoEstaPorCobrar], mal.Errores);
        Assert.True(bien.Exito);
        Assert.Equal(EstadoPago.PagadoEnConsultorio, efectivo.Estado);
    }

    [Fact]
    public async Task Solo_se_anulan_pagos_pendientes()
    {
        var efectivo = Efectivo();
        var validado = Transferencia();
        validado.Validar(Guid.NewGuid(), DatosPrueba.Ahora);

        var anulado = await new AnularPago(pagos, asistente, reloj).EjecutarAsync(efectivo.Id, "No asistió", CancellationToken.None);
        var noPermitido = await new AnularPago(pagos, asistente, reloj).EjecutarAsync(validado.Id, "Error", CancellationToken.None);

        Assert.True(anulado.Exito);
        Assert.Equal(EstadoPago.Anulado, efectivo.Estado);
        Assert.Equal([MensajesPagos.NoEstaPendiente], noPermitido.Errores);
    }

    [Fact]
    public async Task Avisa_si_otra_persona_lo_cambio_o_si_no_existe()
    {
        var pago = Transferencia();
        pagos.LanzarConflictoAlActualizar = true;

        var conflicto = await new ValidarPago(pagos, asistente, reloj).EjecutarAsync(pago.Id, CancellationToken.None);
        var inexistente = await new ValidarPago(pagos, asistente, reloj).EjecutarAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal([MensajesPagos.ModificadoPorOtraPersona], conflicto.Errores);
        Assert.Equal([MensajesPagos.NoExiste], inexistente.Errores);
    }

    [Fact]
    public async Task El_comprobante_solo_se_abre_con_sesion_y_si_existe()
    {
        var almacen = new AlmacenArchivosEnMemoria();
        var id = await almacen.GuardarAsync("comprobantes", ".png", ArchivosPrueba.Png(), CancellationToken.None);
        var comprobante = new ComprobanteTransferencia(id, TipoArchivoComprobante.Png, 108, new string('c', 64));
        var pago = Pago.RegistrarTransferencia(Guid.NewGuid(), 35m, "Consulta", null, comprobante, Guid.NewGuid());
        pagos.Pagos.Add(pago);
        var efectivo = Efectivo();

        var archivo = await new ObtenerComprobantePago(pagos, almacen, asistente).EjecutarAsync(pago.Id, CancellationToken.None);
        var sinArchivo = await new ObtenerComprobantePago(pagos, almacen, asistente).EjecutarAsync(efectivo.Id, CancellationToken.None);

        Assert.NotNull(archivo);
        Assert.Equal("image/png", archivo.TipoContenido);
        Assert.Null(sinArchivo);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            new ObtenerComprobantePago(pagos, almacen, UsuarioActualFalso.SinSesion()).EjecutarAsync(pago.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Solo_la_doctora_ve_el_historial_de_un_pago()
    {
        var pago = Transferencia();
        var auditoria = new ConsultaAuditoriaFalsa();

        var paraAsistente = await new ObtenerPago(pagos, auditoria, asistente).EjecutarAsync(pago.Id, CancellationToken.None);
        Assert.Empty(paraAsistente!.Historial);
        Assert.False(auditoria.FueConsultada);

        var paraDoctora = await new ObtenerPago(pagos, auditoria, UsuarioActualFalso.Doctora()).EjecutarAsync(pago.Id, CancellationToken.None);
        Assert.Single(paraDoctora!.Historial);
    }

    [Fact]
    public async Task Solo_la_doctora_cambia_la_configuracion()
    {
        var repositorio = new RepositorioConfiguracionPagosEnMemoria();
        var comando = new ConfiguracionPagosComando { PrecioConsulta = "40" };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            new ActualizarConfiguracionPagos(repositorio, asistente, new ConfiguracionPagosValidador()).EjecutarAsync(comando, CancellationToken.None));
        Assert.Equal(0, repositorio.Guardados);
    }

    [Fact]
    public async Task La_doctora_guarda_precio_y_cuenta_bancaria()
    {
        var repositorio = new RepositorioConfiguracionPagosEnMemoria();
        var comando = new ConfiguracionPagosComando
        {
            PrecioConsulta = "40,00",
            ConceptoPredeterminado = "Consulta médica",
            RecibeTransferencias = true,
            Banco = "Banco del Pichincha",
            TipoCuenta = TipoCuentaBancaria.Ahorros,
            NumeroCuenta = "2201234567",
            Titular = "Gabriela Calero",
            IdentificacionTitular = "1710034065",
        };

        var resultado = await new ActualizarConfiguracionPagos(repositorio, UsuarioActualFalso.Doctora(), new ConfiguracionPagosValidador())
            .EjecutarAsync(comando, CancellationToken.None);

        Assert.True(resultado.Exito);
        Assert.Equal(1, repositorio.Guardados);
        Assert.Equal(40m, repositorio.Configuracion.PrecioConsulta);
        Assert.Equal("2201234567", repositorio.Configuracion.NumeroCuenta);
    }

    [Fact]
    public async Task Si_recibe_transferencias_exige_los_datos_bancarios()
    {
        var repositorio = new RepositorioConfiguracionPagosEnMemoria();
        var comando = new ConfiguracionPagosComando { RecibeTransferencias = true };

        var resultado = await new ActualizarConfiguracionPagos(repositorio, UsuarioActualFalso.Doctora(), new ConfiguracionPagosValidador())
            .EjecutarAsync(comando, CancellationToken.None);

        Assert.False(resultado.Exito);
        Assert.Equal(4, resultado.Errores.Count); // banco, número, titular e identificación
        Assert.Equal(0, repositorio.Guardados);
    }

    [Fact]
    public async Task Detecta_cambios_simultaneos_en_la_configuracion()
    {
        var repositorio = new RepositorioConfiguracionPagosEnMemoria();
        var comando = new ConfiguracionPagosComando { Version = 99 };

        var resultado = await new ActualizarConfiguracionPagos(repositorio, UsuarioActualFalso.Doctora(), new ConfiguracionPagosValidador())
            .EjecutarAsync(comando, CancellationToken.None);

        Assert.Equal([MensajesPagos.ModificadoPorOtraPersona], resultado.Errores);
    }
}
