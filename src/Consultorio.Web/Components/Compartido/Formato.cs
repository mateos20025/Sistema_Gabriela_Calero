using System.Globalization;
using Consultorio.Application.Citas;
using Consultorio.Application.Comun;
using Consultorio.Domain.Citas;
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

    /// <summary>"09:30" en hora de Ecuador.</summary>
    public static string Hora(DateTime fechaUtc) => HoraEcuador.DesdeUtc(fechaUtc).ToString("HH:mm", CulturaEcuador);

    /// <summary>"lunes 19 de octubre de 2026".</summary>
    public static string FechaLarga(DateOnly fecha) => fecha.ToString("dddd d 'de' MMMM 'de' yyyy", CulturaEcuador);

    /// <summary>"lun 19/10".</summary>
    public static string FechaCorta(DateOnly fecha) => fecha.ToString("ddd dd/MM", CulturaEcuador);

    /// <summary>"lunes 19 de octubre de 2026, 09:30" en hora de Ecuador.</summary>
    public static string FechaHoraLarga(DateTime fechaUtc) =>
        $"{FechaLarga(DateOnly.FromDateTime(HoraEcuador.DesdeUtc(fechaUtc)))}, {Hora(fechaUtc)}";

    public static string DiaSemana(DayOfWeek dia) => CulturaEcuador.DateTimeFormat.GetDayName(dia);

    public static string HoraDelDia(TimeOnly hora) => hora.ToString("HH:mm", CulturaEcuador);

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

    public static string Estado(EstadoCita estado) => NombresEstadoCita.De(estado);

    public static Color ColorEstado(EstadoCita estado) => estado switch
    {
        EstadoCita.Pendiente => Color.Warning,
        EstadoCita.EnCola => Color.Info,
        EstadoCita.NoContesta => Color.Secondary,
        EstadoCita.Confirmada => Color.Success,
        EstadoCita.Reprogramada => Color.Primary,
        EstadoCita.Atendida => Color.Dark,
        EstadoCita.Cancelada or EstadoCita.NoAsistio => Color.Error,
        _ => Color.Default,
    };

    /// <summary>Texto del botón que lleva la cita a ese estado.</summary>
    public static string AccionHacia(EstadoCita estado) => estado switch
    {
        EstadoCita.EnCola => "Pasar a la cola de llamadas",
        EstadoCita.Confirmada => "Confirmar",
        EstadoCita.NoContesta => "No contestó",
        EstadoCita.Cancelada => "Cancelar cita",
        EstadoCita.Atendida => "Marcar atendida",
        EstadoCita.NoAsistio => "Marcar no asistió",
        _ => Estado(estado),
    };

    public static string Origen(OrigenRegistro origen) => origen switch
    {
        OrigenRegistro.Web => "Página web",
        OrigenRegistro.Panel => "Panel",
        _ => origen.ToString(),
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
