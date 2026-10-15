using Consultorio.Domain.Comun;
using Consultorio.Domain.Pagos;

namespace Consultorio.Domain.Tests.Pagos;

public class PagoTests
{
    private static readonly DateTime Ahora = new(2026, 10, 14, 15, 0, 0, DateTimeKind.Utc);
    private static readonly Guid PacienteId = Guid.NewGuid();
    private static readonly Guid Asistente = Guid.NewGuid();
    private static readonly Guid Doctora = Guid.NewGuid();

    private static ComprobanteTransferencia Comprobante() =>
        new("comprobantes/2026/10/abc.png", TipoArchivoComprobante.Png, 2048, new string('a', 64));

    private static Pago Transferencia() =>
        Pago.RegistrarTransferencia(PacienteId, 35m, " Consulta médica ", "ab-123", Comprobante(), Asistente);

    private static Pago Efectivo() => Pago.RegistrarEfectivo(PacienteId, 35m, "Consulta médica", Asistente);

    [Fact]
    public void Una_transferencia_empieza_pendiente_de_validacion_con_su_comprobante()
    {
        var pago = Transferencia();

        Assert.Equal(MetodoPago.Transferencia, pago.Metodo);
        Assert.Equal(EstadoPago.PendienteValidacion, pago.Estado);
        Assert.Equal("Consulta médica", pago.Concepto);
        Assert.Equal("AB-123", pago.NumeroComprobante);
        Assert.Equal(new string('a', 64), pago.ComprobanteHash);
        Assert.Equal(TipoArchivoComprobante.Png, pago.ComprobanteTipo);
        Assert.Null(pago.CitaId);
        Assert.True(pago.EstaPendiente);
    }

    [Fact]
    public void Un_pago_en_efectivo_empieza_pendiente_en_consultorio_y_sin_comprobante()
    {
        var pago = Efectivo();

        Assert.Equal(EstadoPago.PendienteEnConsultorio, pago.Estado);
        Assert.Null(pago.ComprobanteArchivoId);
    }

    [Fact]
    public void Validar_guarda_quien_y_cuando()
    {
        var pago = Transferencia();

        pago.Validar(Doctora, Ahora);

        Assert.Equal(EstadoPago.Validado, pago.Estado);
        Assert.Equal(Doctora, pago.ResueltoPorId);
        Assert.Equal(Ahora, pago.ResueltoEn);
        Assert.False(pago.EstaPendiente);
    }

    [Fact]
    public void Rechazar_exige_motivo()
    {
        var pago = Transferencia();

        Assert.Throws<ExcepcionDominio>(() => pago.Rechazar(Asistente, Ahora, "  "));

        pago.Rechazar(Asistente, Ahora, " El monto no coincide ");
        Assert.Equal(EstadoPago.Rechazado, pago.Estado);
        Assert.Equal("El monto no coincide", pago.MotivoResolucion);
    }

    [Fact]
    public void El_efectivo_se_marca_como_pagado()
    {
        var pago = Efectivo();

        pago.MarcarPagado(Asistente, Ahora);

        Assert.Equal(EstadoPago.PagadoEnConsultorio, pago.Estado);
    }

    [Fact]
    public void No_se_puede_validar_un_pago_en_efectivo_ni_marcar_pagada_una_transferencia()
    {
        Assert.Throws<ExcepcionDominio>(() => Efectivo().Validar(Doctora, Ahora));
        Assert.Throws<ExcepcionDominio>(() => Transferencia().MarcarPagado(Doctora, Ahora));
    }

    [Fact]
    public void Los_estados_finales_no_cambian()
    {
        var validado = Transferencia();
        validado.Validar(Doctora, Ahora);

        Assert.Throws<ExcepcionDominio>(() => validado.Rechazar(Doctora, Ahora, "otro motivo"));
        Assert.Throws<ExcepcionDominio>(() => validado.Anular(Doctora, Ahora, "error"));
        Assert.Throws<ExcepcionDominio>(() => validado.Validar(Doctora, Ahora));
    }

    [Fact]
    public void Un_pago_pendiente_se_puede_anular_con_motivo()
    {
        var pago = Efectivo();

        pago.Anular(Asistente, Ahora, "El paciente no asistió");

        Assert.Equal(EstadoPago.Anulado, pago.Estado);
        Assert.Equal("El paciente no asistió", pago.MotivoResolucion);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10000.01)]
    [InlineData(35.555)]
    public void Rechaza_montos_invalidos(decimal monto)
    {
        Assert.Throws<ExcepcionDominio>(() => Pago.RegistrarEfectivo(PacienteId, monto, "Consulta", Asistente));
    }

    [Fact]
    public void Exige_paciente_concepto_y_usuario()
    {
        Assert.Throws<ExcepcionDominio>(() => Pago.RegistrarEfectivo(Guid.Empty, 35m, "Consulta", Asistente));
        Assert.Throws<ExcepcionDominio>(() => Pago.RegistrarEfectivo(PacienteId, 35m, " ", Asistente));
        Assert.Throws<ExcepcionDominio>(() => Pago.RegistrarEfectivo(PacienteId, 35m, "Consulta", Guid.Empty));
    }

    [Fact]
    public void Exige_fecha_UTC_al_cambiar_de_estado()
    {
        var local = new DateTime(2026, 10, 14, 10, 0, 0, DateTimeKind.Local);

        Assert.Throws<ExcepcionDominio>(() => Transferencia().Validar(Doctora, local));
    }

    [Fact]
    public void El_comprobante_valida_tamano_y_huella()
    {
        Assert.Throws<ExcepcionDominio>(() => new ComprobanteTransferencia("a.png", TipoArchivoComprobante.Png, ArchivosComprobante.TamanoMaximoBytes + 1, new string('a', 64)));
        Assert.Throws<ExcepcionDominio>(() => new ComprobanteTransferencia("a.png", TipoArchivoComprobante.Png, 10, "ABC"));
        Assert.Throws<ExcepcionDominio>(() => new ComprobanteTransferencia("a.png", TipoArchivoComprobante.Png, 10, new string('G', 64)));
    }
}
