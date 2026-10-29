using System.Globalization;
using Consultorio.Application.Citas;
using Consultorio.Domain.Pacientes;
using Consultorio.Web.Configuracion;
using MudBlazor;

namespace Consultorio.Web.Components.Sitio;

/// <summary>Íconos que se pueden usar en el contenido del sitio (Contenido/sitio-publico.json → "Icono").</summary>
public static class IconosSitio
{
    private static readonly Dictionary<string, string> Iconos = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ginecologia"] = Icons.Material.Outlined.HealthAndSafety,
        ["embarazo"] = Icons.Material.Outlined.PregnantWoman,
        ["colposcopia"] = Icons.Material.Outlined.Biotech,
        ["papanicolaou"] = Icons.Material.Outlined.Science,
        ["anticoncepcion"] = Icons.Material.Outlined.Medication,
        ["fertilidad"] = Icons.Material.Outlined.ChildFriendly,
        ["rejuvenecimiento"] = Icons.Material.Outlined.Spa,
        ["paquete"] = Icons.Material.Outlined.FactCheck,
        ["prevenir"] = Icons.Material.Outlined.Shield,
        ["escuchar"] = Icons.Material.Outlined.VolunteerActivism,
        ["acompanar"] = Icons.Material.Outlined.Diversity1,
        ["bienestar"] = Icons.Material.Outlined.FavoriteBorder,
    };

    public static string De(string? clave) =>
        clave is not null && Iconos.TryGetValue(clave, out var icono) ? icono : Icons.Material.Outlined.FavoriteBorder;
}

/// <summary>Enlaces del sitio armados a partir del contenido configurado.</summary>
public static class EnlacesSitio
{
    private const string BaseWhatsApp = "https://wa.me/";
    private const string BaseBusquedaMapa = "https://www.google.com/maps/search/?api=1&query=";

    /// <summary>Número en formato internacional (+593...), ya validado al arrancar.</summary>
    public static string WhatsAppE164(ContactoSitio contacto) => TelefonoEcuatoriano.Normalizar(contacto.WhatsApp)!;

    public static string WhatsApp(ContactoSitio contacto)
    {
        var numero = WhatsAppE164(contacto).TrimStart('+');
        return string.IsNullOrWhiteSpace(contacto.MensajeWhatsApp)
            ? $"{BaseWhatsApp}{numero}"
            : $"{BaseWhatsApp}{numero}?text={Uri.EscapeDataString(contacto.MensajeWhatsApp)}";
    }

    public static string Mapa(ConsultorioSitio consultorio)
    {
        if (!string.IsNullOrWhiteSpace(consultorio.UrlMapa))
        {
            return consultorio.UrlMapa;
        }

        var partes = new[] { consultorio.Lugar, consultorio.Direccion, consultorio.Ciudad, "Ecuador" }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        return BaseBusquedaMapa + Uri.EscapeDataString(string.Join(", ", partes));
    }
}

/// <summary>Convierte las franjas de la agenda en líneas legibles: "Lunes a viernes · 09:00 a 13:00".</summary>
public static class HorarioLegible
{
    private static readonly CultureInfo CulturaEcuador = CultureInfo.GetCultureInfo("es-EC");

    private static readonly DayOfWeek[] Semana =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday,
    ];

    public sealed record Linea(string Dias, string Horas);

    public static IReadOnlyList<Linea> Agrupar(IReadOnlyList<FranjaVista> franjas)
    {
        var horasPorDia = Semana
            .Select(dia => string.Join(" y ", franjas
                .Where(f => f.DiaSemana == dia)
                .OrderBy(f => f.Inicio)
                .Select(f => $"{f.Inicio.ToString("HH:mm", CulturaEcuador)} a {f.Fin.ToString("HH:mm", CulturaEcuador)}")))
            .ToArray();

        var lineas = new List<Linea>();
        var inicio = 0;
        while (inicio < Semana.Length)
        {
            var fin = inicio;
            while (fin + 1 < Semana.Length && horasPorDia[fin + 1] == horasPorDia[inicio])
            {
                fin++;
            }

            if (horasPorDia[inicio].Length > 0)
            {
                lineas.Add(new Linea(NombreRango(inicio, fin), horasPorDia[inicio]));
            }

            inicio = fin + 1;
        }

        return lineas;
    }

    private static string NombreRango(int desde, int hasta)
    {
        var primero = NombreDia(Semana[desde]);
        var texto = (hasta - desde) switch
        {
            0 => primero,
            1 => $"{primero} y {NombreDia(Semana[hasta])}",
            _ => $"{primero} a {NombreDia(Semana[hasta])}",
        };
        return char.ToUpper(texto[0], CulturaEcuador) + texto[1..];
    }

    private static string NombreDia(DayOfWeek dia) => CulturaEcuador.DateTimeFormat.GetDayName(dia);
}
