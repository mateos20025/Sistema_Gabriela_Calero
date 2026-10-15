using Consultorio.Application.Auditoria;
using Consultorio.Application.Comun;
using Consultorio.Domain.Pagos;

namespace Consultorio.Application.Pagos;

/// <summary>Grupos de estados para filtrar la lista de pagos.</summary>
public enum FiltroEstadoPago
{
    /// <summary>Por validar o por cobrar.</summary>
    Pendientes = 1,
    PorValidar = 2,
    PorCobrar = 3,

    /// <summary>Transferencias validadas o efectivo cobrado.</summary>
    Completados = 4,
    Rechazados = 5,
    Anulados = 6,
    Todos = 7,
}

/// <summary>Filtro de la lista de pagos. <see cref="Pagina"/> empieza en 0.</summary>
public sealed record FiltroPagos(string? Texto, FiltroEstadoPago Estado, int Pagina, int TamanoPagina)
{
    public const int TamanoPaginaMaximo = 100;
    public const int LongitudMaximaTexto = 100;
}

public sealed record PagoResumen(
    Guid Id,
    Guid PacienteId,
    string PacienteNombre,
    string PacienteCedula,
    MetodoPago Metodo,
    EstadoPago Estado,
    decimal Monto,
    string Concepto,
    DateTime CreadoUtc);

/// <summary>Ficha de un pago. Las fechas vienen en UTC.</summary>
public sealed record PagoDetalle(
    Guid Id,
    Guid PacienteId,
    string PacienteNombre,
    string PacienteCedula,
    Guid? CitaId,
    DateTime? CitaInicioUtc,
    MetodoPago Metodo,
    EstadoPago Estado,
    decimal Monto,
    string Concepto,
    string? NumeroComprobante,
    TipoArchivoComprobante? ComprobanteTipo,
    long? ComprobanteTamanoBytes,
    string RegistradoPor,
    DateTime CreadoUtc,
    string? ResueltoPor,
    DateTime? ResueltoUtc,
    string? MotivoResolucion,
    IReadOnlyList<EventoAuditoria> Historial)
{
    public bool EstaPendiente => Estado is EstadoPago.PendienteValidacion or EstadoPago.PendienteEnConsultorio;

    public bool TieneComprobante => ComprobanteTipo is not null;
}

/// <summary>Datos del pago que ya usa una captura, para avisar al personal.</summary>
public sealed record PagoConMismoComprobante(DateTime CreadoUtc, string PacienteNombre);

/// <summary>Archivo listo para enviarse al navegador. Quien lo recibe debe cerrar el <see cref="Contenido"/>.</summary>
public sealed record ArchivoDescargable(Stream Contenido, string TipoContenido);

/// <summary>Configuración de pagos para mostrar (todo el personal puede verla).</summary>
public sealed record ConfiguracionPagosVista(
    decimal? PrecioConsulta,
    string? ConceptoPredeterminado,
    string? Banco,
    TipoCuentaBancaria? TipoCuenta,
    string? NumeroCuenta,
    string? Titular,
    string? IdentificacionTitular,
    uint Version)
{
    public bool TieneDatosBancarios => Banco is not null;

    public static ConfiguracionPagosVista Desde(ConfiguracionPagos configuracion) => new(
        configuracion.PrecioConsulta,
        configuracion.ConceptoPredeterminado,
        configuracion.Banco,
        configuracion.TipoCuenta,
        configuracion.NumeroCuenta,
        configuracion.Titular,
        configuracion.IdentificacionTitular,
        configuracion.Version);
}

/// <summary>Mensajes de los casos de uso de pagos (también los usan las pruebas).</summary>
public static class MensajesPagos
{
    public const string NoExiste = "El pago no existe.";
    public const string PacienteNoExiste = "El paciente no existe.";
    public const string PacienteDeshabilitado = "El paciente está deshabilitado. Habilítalo para registrarle pagos.";
    public const string CitaNoCorresponde = "La cita no existe o no es de este paciente.";
    public const string ComprobanteObligatorio = "Adjunta la captura de la transferencia.";
    public const string ComprobanteMuyGrande = "La captura no puede pesar más de 5 MB.";
    public const string TipoNoPermitido = "La captura debe ser una imagen JPG o PNG, o un PDF.";
    public const string EfectivoSinComprobante = "Un pago en efectivo no lleva captura.";
    public const string ComprobanteYaRegistrado = "Esta captura ya está registrada en otro pago.";
    public const string NoEstaPorValidar = "Este pago ya no está pendiente de validación.";
    public const string NoEstaPorCobrar = "Este pago ya no está pendiente de cobro.";
    public const string NoEstaPendiente = "Este pago ya no está pendiente.";
    public const string MotivoObligatorio = "Escribe el motivo (máximo 300 caracteres).";
    public const string ModificadoPorOtraPersona =
        "Otra persona modificó este registro mientras lo revisabas. Vuelve a abrirlo para ver los datos actuales.";

    public static string ComprobanteRepetido(PagoConMismoComprobante otro) =>
        $"Esta captura ya se registró el {HoraEcuador.DesdeUtc(otro.CreadoUtc):dd/MM/yyyy} en un pago de {otro.PacienteNombre}.";
}
