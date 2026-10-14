using Consultorio.Application.Comun;
using Consultorio.Domain.Pacientes;

namespace Consultorio.Web.Components.Compartido;

/// <summary>Formatos de presentación. Las fechas se guardan en UTC y se muestran en hora de Ecuador.</summary>
public static class Formato
{
    public static string FechaHora(DateTime fechaUtc) =>
        HoraEcuador.DesdeUtc(fechaUtc).ToString("dd/MM/yyyy HH:mm");

    public static string FechaHora(DateTime? fechaUtc, string siVacio) =>
        fechaUtc is null ? siVacio : FechaHora(fechaUtc.Value);

    public static string Fecha(DateTime fechaUtc) =>
        HoraEcuador.DesdeUtc(fechaUtc).ToString("dd/MM/yyyy");

    public static string Telefono(string telefonoE164) => TelefonoEcuatoriano.Formatear(telefonoE164);
}
