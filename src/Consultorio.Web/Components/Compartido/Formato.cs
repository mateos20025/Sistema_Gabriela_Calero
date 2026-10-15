using System.Globalization;
using Consultorio.Application.Comun;
using Consultorio.Domain.Pacientes;
using Consultorio.Domain.Pagos;
using MudBlazor;

namespace Consultorio.Web.Components.Compartido;

/// <summary>Formatos de presentación. Las fechas se guardan en UTC y se muestran en hora de Ecuador.</summary>
public static class Formato
{
    private static readonly CultureInfo CulturaEcuador = CultureInfo.GetCultureInfo("es-EC");

    public static string FechaHora(DateTime fechaUtc) =>
        HoraEcuador.DesdeUtc(fechaUtc).ToString("dd/MM/yyyy HH:mm", CulturaEcuador);

    public static string FechaHora(DateTime? fechaUtc, string siVacio) =>
        fechaUtc is null ? siVacio : FechaHora(fechaUtc.Value);

    public static string Fecha(DateTime fechaUtc) =>
        HoraEcuador.DesdeUtc(fechaUtc).ToString("dd/MM/yyyy", CulturaEcuador);

    public static string Telefono(string telefonoE164) => TelefonoEcuatoriano.Formatear(telefonoE164);

    /// <summary>"$1.234,50" (formato de Ecuador).</summary>
    public static string Moneda(decimal monto) => monto.ToString("C2", CulturaEcuador);

    /// <summary>"35,00": para llenar un campo de texto que luego se interpreta con <see cref="MontoDolares"/>.</summary>
    public static string MontoEditable(decimal monto) => monto.ToString("0.00", CulturaEcuador);

    public static string Tamano(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:0} KB",
        _ => $"{bytes / (1024.0 * 1024.0):0.0} MB",
    };

    public static string Estado(EstadoPago estado) => estado switch
    {
        EstadoPago.PendienteValidacion => "Por validar",
        EstadoPago.Validado => "Validado",
        EstadoPago.Rechazado => "Rechazado",
        EstadoPago.PendienteEnConsultorio => "Por cobrar",
        EstadoPago.PagadoEnConsultorio => "Pagado",
        EstadoPago.Anulado => "Anulado",
        _ => estado.ToString(),
    };

    public static Color ColorEstado(EstadoPago estado) => estado switch
    {
        EstadoPago.PendienteValidacion => Color.Warning,
        EstadoPago.PendienteEnConsultorio => Color.Info,
        EstadoPago.Validado or EstadoPago.PagadoEnConsultorio => Color.Success,
        EstadoPago.Rechazado => Color.Error,
        _ => Color.Default,
    };

    public static string Metodo(MetodoPago metodo) => metodo switch
    {
        MetodoPago.Transferencia => "Transferencia",
        MetodoPago.Efectivo => "Efectivo",
        _ => metodo.ToString(),
    };

    public static string TipoCuenta(TipoCuentaBancaria? tipo) => tipo switch
    {
        TipoCuentaBancaria.Ahorros => "Ahorros",
        TipoCuentaBancaria.Corriente => "Corriente",
        _ => "",
    };
}
