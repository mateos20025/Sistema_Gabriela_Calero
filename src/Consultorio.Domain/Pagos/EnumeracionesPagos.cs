namespace Consultorio.Domain.Pagos;

public enum MetodoPago
{
    /// <summary>El paciente transfiere y envía la captura; la asistente la valida.</summary>
    Transferencia = 1,

    /// <summary>El paciente paga en el consultorio.</summary>
    Efectivo = 2,
}

/// <summary>
/// Estado del pago (independiente del estado de la cita).
/// <list type="bullet">
/// <item>Transferencia: PendienteValidacion → Validado | Rechazado.</item>
/// <item>Efectivo: PendienteEnConsultorio → PagadoEnConsultorio.</item>
/// <item>Cualquier pago pendiente puede pasar a Anulado (registrado por error, paciente que no llegó...).</item>
/// </list>
/// Validado, Rechazado, PagadoEnConsultorio y Anulado son finales.
/// </summary>
public enum EstadoPago
{
    PendienteValidacion = 1,
    Validado = 2,
    Rechazado = 3,
    PendienteEnConsultorio = 4,
    PagadoEnConsultorio = 5,
    Anulado = 6,
}

public enum TipoArchivoComprobante
{
    Jpg = 1,
    Png = 2,
    Pdf = 3,
}

public enum TipoCuentaBancaria
{
    Ahorros = 1,
    Corriente = 2,
}
