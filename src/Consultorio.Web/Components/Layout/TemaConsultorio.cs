using MudBlazor;

namespace Consultorio.Web.Components.Layout;

/// <summary>Colores y formas del sistema. Cambiar aquí cambia todo el sitio.</summary>
public static class TemaConsultorio
{
    public static MudTheme Tema { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#0F766E",
            Secondary = "#475569",
            AppbarBackground = "#0F766E",
            Background = "#F4F7F8",
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px",
        },
    };
}
