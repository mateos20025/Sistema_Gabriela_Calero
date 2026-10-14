namespace Consultorio.Web.Navegacion;

/// <summary>Rutas del panel en un solo lugar (deben coincidir con las directivas @page).</summary>
public static class RutasPanel
{
    public const string Inicio = "/admin";
    public const string Pacientes = "/admin/pacientes";
    public const string Usuarios = "/admin/usuarios";

    public static string Paciente(Guid id) => $"{Pacientes}/{id}";
}
