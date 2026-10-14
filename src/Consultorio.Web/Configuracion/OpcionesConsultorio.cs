using System.ComponentModel.DataAnnotations;

namespace Consultorio.Web.Configuracion;

/// <summary>
/// Datos del consultorio que se muestran en pantalla (sección "Consultorio" de la configuración).
/// Se validan al arrancar: si faltan, la aplicación no inicia.
/// </summary>
public sealed class OpcionesConsultorio
{
    public const string Seccion = "Consultorio";

    /// <summary>Nombre completo, por ejemplo en la página pública y en el inicio de sesión.</summary>
    [Required, StringLength(100)]
    public string Nombre { get; init; } = "";

    /// <summary>Nombre corto para la barra del panel y los títulos de las pestañas del navegador.</summary>
    [Required, StringLength(40)]
    public string NombreCorto { get; init; } = "";
}
