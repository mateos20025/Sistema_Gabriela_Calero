using MudBlazor;

namespace Consultorio.Web.Components.Layout;

/// <summary>
/// Colores y formas del sistema, tomados de la marca de la doctora (turquesa y rosa).
/// Los tonos usados en textos y botones son un poco más oscuros que los de los flyers para que se lean bien.
/// Los mismos colores están como variables CSS en wwwroot/css/sitio.css (página pública).
/// </summary>
public static class TemaConsultorio
{
    private static readonly string[] FuentePrincipal = ["Poppins", "Segoe UI", "Helvetica", "Arial", "sans-serif"];

    public static MudTheme Tema { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#0D7F74",
            PrimaryDarken = "#0A665D",
            PrimaryLighten = "#14A89A",
            Secondary = "#C7337A",
            SecondaryDarken = "#A82A66",
            SecondaryLighten = "#E8478F",
            Tertiary = "#14A89A",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#0B7369",
            Background = "#FBF6F9",
            Surface = "#FFFFFF",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#2B3A40",
            DrawerIcon = "#0D7F74",
            TextPrimary = "#2B3A40",
            TextSecondary = "#5E6B72",
            LinesDefault = "#F0E2E9",
            TableLines = "#F0E2E9",
            Divider = "#F0E2E9",
        },
        Typography = new Typography
        {
            Default = new DefaultTypography { FontFamily = FuentePrincipal },
            H4 = new H4Typography { FontFamily = FuentePrincipal, FontWeight = "600" },
            H5 = new H5Typography { FontFamily = FuentePrincipal, FontWeight = "600" },
            H6 = new H6Typography { FontFamily = FuentePrincipal, FontWeight = "600" },
            Button = new ButtonTypography { FontFamily = FuentePrincipal, FontWeight = "500", TextTransform = "none" },
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "12px",
        },
    };
}
