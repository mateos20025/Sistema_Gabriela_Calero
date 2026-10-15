namespace Consultorio.Web.Navegacion;

/// <summary>Rutas del panel en un solo lugar (deben coincidir con las directivas @page).</summary>
public static class RutasPanel
{
    public const string Inicio = "/admin";
    public const string Agenda = "/admin/agenda";
    public const string AjustesAgenda = "/admin/agenda/configuracion";
    public const string Citas = "/admin/citas";
    public const string Pacientes = "/admin/pacientes";
    public const string Pagos = "/admin/pagos";
    public const string AjustesPagos = "/admin/pagos/configuracion";
    public const string Usuarios = "/admin/usuarios";

    /// <summary>Endpoint que entrega la captura de una transferencia (solo con sesión iniciada).</summary>
    public const string PatronComprobante = "/archivos/comprobantes/{pagoId:guid}";

    public static string Paciente(Guid id) => $"{Pacientes}/{id}";

    public static string Cita(Guid id) => $"{Citas}/{id}";

    public static string Pago(Guid id) => $"{Pagos}/{id}";

    public static string Comprobante(Guid pagoId) => $"/archivos/comprobantes/{pagoId}";
}

/// <summary>Rutas de las páginas públicas (sin iniciar sesión).</summary>
public static class RutasPublicas
{
    public const string SolicitarCita = "/citas/solicitar";
    public const string Privacidad = "/privacidad";

    public static string SolicitarCitaEnDia(DateOnly fecha) => $"{SolicitarCita}?fecha={fecha:yyyy-MM-dd}";
}
