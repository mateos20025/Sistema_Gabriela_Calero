using Consultorio.Domain.Comun;

namespace Consultorio.Domain.Pagos;

/// <summary>
/// Pago de un paciente. Su estado es independiente del de la cita.
/// <para>
/// Por ahora se asocia al paciente; <see cref="CitaId"/> se usará cuando exista el módulo de Citas.
/// Cada cambio de estado guarda quién lo hizo y cuándo; los estados finales no se pueden cambiar.
/// </para>
/// </summary>
public sealed class Pago
{
    public const int LongitudMaximaConcepto = 120;
    public const int LongitudMaximaNumeroComprobante = 40;
    public const int LongitudMaximaMotivo = 300;

    private Pago()
    {
    }

    public Guid Id { get; private set; }

    public Guid PacienteId { get; private set; }

    /// <summary>Cita a la que corresponde (opcional: un pago puede registrarse solo para el paciente).</summary>
    public Guid? CitaId { get; private set; }

    public MetodoPago Metodo { get; private set; }

    public EstadoPago Estado { get; private set; }

    public decimal Monto { get; private set; }

    public string Concepto { get; private set; } = "";

    /// <summary>Número de referencia de la transferencia que da el banco (opcional).</summary>
    public string? NumeroComprobante { get; private set; }

    public string? ComprobanteArchivoId { get; private set; }

    public TipoArchivoComprobante? ComprobanteTipo { get; private set; }

    public long? ComprobanteTamanoBytes { get; private set; }

    public string? ComprobanteHash { get; private set; }

    public Guid RegistradoPorId { get; private set; }

    /// <summary>Quién validó, rechazó, marcó como pagado o anuló.</summary>
    public Guid? ResueltoPorId { get; private set; }

    public DateTime? ResueltoEn { get; private set; }

    /// <summary>Motivo del rechazo o de la anulación.</summary>
    public string? MotivoResolucion { get; private set; }

    public uint Version { get; private set; }

    public DateTime CreadoEn { get; private set; }

    public DateTime ActualizadoEn { get; private set; }

    public bool EstaPendiente => Estado is EstadoPago.PendienteValidacion or EstadoPago.PendienteEnConsultorio;

    public static Pago RegistrarTransferencia(
        Guid pacienteId,
        decimal monto,
        string concepto,
        string? numeroComprobante,
        ComprobanteTransferencia comprobante,
        Guid registradoPorId,
        Guid? citaId = null)
    {
        ArgumentNullException.ThrowIfNull(comprobante);

        var pago = Nuevo(pacienteId, MetodoPago.Transferencia, EstadoPago.PendienteValidacion, monto, concepto, registradoPorId, citaId);
        pago.NumeroComprobante = NormalizarNumeroComprobante(numeroComprobante);
        pago.ComprobanteArchivoId = comprobante.ArchivoId;
        pago.ComprobanteTipo = comprobante.Tipo;
        pago.ComprobanteTamanoBytes = comprobante.TamanoBytes;
        pago.ComprobanteHash = comprobante.HashSha256;
        return pago;
    }

    public static Pago RegistrarEfectivo(Guid pacienteId, decimal monto, string concepto, Guid registradoPorId, Guid? citaId = null) =>
        Nuevo(pacienteId, MetodoPago.Efectivo, EstadoPago.PendienteEnConsultorio, monto, concepto, registradoPorId, citaId);

    public void Validar(Guid usuarioId, DateTime ahoraUtc)
    {
        ExigirEstado(EstadoPago.PendienteValidacion, "Solo se puede validar una transferencia pendiente de validación.");
        Resolver(EstadoPago.Validado, usuarioId, ahoraUtc, motivo: null);
    }

    public void Rechazar(Guid usuarioId, DateTime ahoraUtc, string motivo)
    {
        ExigirEstado(EstadoPago.PendienteValidacion, "Solo se puede rechazar una transferencia pendiente de validación.");
        Resolver(EstadoPago.Rechazado, usuarioId, ahoraUtc, NormalizarMotivo(motivo));
    }

    public void MarcarPagado(Guid usuarioId, DateTime ahoraUtc)
    {
        ExigirEstado(EstadoPago.PendienteEnConsultorio, "Solo se puede marcar como pagado un pago en efectivo pendiente.");
        Resolver(EstadoPago.PagadoEnConsultorio, usuarioId, ahoraUtc, motivo: null);
    }

    public void Anular(Guid usuarioId, DateTime ahoraUtc, string motivo)
    {
        if (!EstaPendiente)
        {
            throw new ExcepcionDominio("Solo se puede anular un pago pendiente.");
        }

        Resolver(EstadoPago.Anulado, usuarioId, ahoraUtc, NormalizarMotivo(motivo));
    }

    public static bool MotivoEsValido(string? motivo) =>
        !string.IsNullOrWhiteSpace(motivo) && motivo.Trim().Length <= LongitudMaximaMotivo;

    public static bool ConceptoEsValido(string? concepto) =>
        !string.IsNullOrWhiteSpace(concepto) && concepto.Trim().Length <= LongitudMaximaConcepto;

    /// <summary>Opcional: letras, números y guiones (así lo imprimen los bancos).</summary>
    public static bool NumeroComprobanteEsValido(string? numero) =>
        string.IsNullOrWhiteSpace(numero)
        || (numero.Trim().Length <= LongitudMaximaNumeroComprobante && numero.Trim().All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or ' '));

    private static Pago Nuevo(
        Guid pacienteId, MetodoPago metodo, EstadoPago estado, decimal monto, string concepto, Guid registradoPorId, Guid? citaId)
    {
        if (pacienteId == Guid.Empty)
        {
            throw new ExcepcionDominio("El pago debe pertenecer a un paciente.");
        }

        if (registradoPorId == Guid.Empty)
        {
            throw new ExcepcionDominio("Se debe saber quién registra el pago.");
        }

        if (!MontoDolares.EsValido(monto))
        {
            throw new ExcepcionDominio("El monto no es válido.");
        }

        if (!ConceptoEsValido(concepto))
        {
            throw new ExcepcionDominio("El concepto no es válido.");
        }

        return new Pago
        {
            Id = Guid.CreateVersion7(),
            PacienteId = pacienteId,
            CitaId = citaId == Guid.Empty ? null : citaId,
            Metodo = metodo,
            Estado = estado,
            Monto = monto,
            Concepto = concepto.Trim(),
            RegistradoPorId = registradoPorId,
        };
    }

    private void ExigirEstado(EstadoPago esperado, string mensaje)
    {
        if (Estado != esperado)
        {
            throw new ExcepcionDominio(mensaje);
        }
    }

    private void Resolver(EstadoPago nuevoEstado, Guid usuarioId, DateTime ahoraUtc, string? motivo)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new ExcepcionDominio("Se debe saber quién cambia el estado del pago.");
        }

        if (ahoraUtc.Kind != DateTimeKind.Utc)
        {
            throw new ExcepcionDominio("La fecha debe estar en UTC.");
        }

        Estado = nuevoEstado;
        ResueltoPorId = usuarioId;
        ResueltoEn = ahoraUtc;
        MotivoResolucion = motivo;
    }

    private static string NormalizarMotivo(string motivo) =>
        MotivoEsValido(motivo) ? motivo.Trim() : throw new ExcepcionDominio("Escribe el motivo.");

    private static string? NormalizarNumeroComprobante(string? numero)
    {
        if (!NumeroComprobanteEsValido(numero))
        {
            throw new ExcepcionDominio("El número de comprobante no es válido.");
        }

        return string.IsNullOrWhiteSpace(numero) ? null : numero.Trim().ToUpperInvariant();
    }
}
