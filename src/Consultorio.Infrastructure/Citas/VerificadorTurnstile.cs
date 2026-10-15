using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Consultorio.Application.Citas;
using Microsoft.Extensions.Logging;

namespace Consultorio.Infrastructure.Citas;

/// <summary>
/// Configuración de Cloudflare Turnstile (sección "Turnstile"). La clave secreta va solo en
/// User Secrets o en la variable de entorno Turnstile__ClaveSecreta, nunca en el repositorio.
/// </summary>
public sealed class OpcionesTurnstile
{
    public const string Seccion = "Turnstile";

    public string? ClaveSitio { get; init; }

    public string? ClaveSecreta { get; init; }

    public bool EstaConfigurado => !string.IsNullOrWhiteSpace(ClaveSitio) && !string.IsNullOrWhiteSpace(ClaveSecreta);
}

/// <summary>
/// Verifica con Cloudflare Turnstile que el formulario público lo envió una persona.
/// Si Cloudflare no responde, se rechaza el envío (es más seguro que dejar pasar a un programa).
/// </summary>
internal sealed class VerificadorTurnstile(HttpClient http, OpcionesTurnstile opciones, ILogger<VerificadorTurnstile> logger)
    : IVerificadorHumano
{
    public const string UrlVerificacion = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    /// <summary>Tamaño máximo de la respuesta del widget según la documentación de Cloudflare.</summary>
    private const int LongitudMaximaRespuesta = 2048;

    public bool Habilitado => opciones.EstaConfigurado;

    public string? ClaveSitio => Habilitado ? opciones.ClaveSitio : null;

    public async Task<bool> VerificarAsync(string? respuesta, string? ipCliente, CancellationToken cancellationToken)
    {
        if (!Habilitado)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(respuesta) || respuesta.Length > LongitudMaximaRespuesta)
        {
            return false;
        }

        var datos = new Dictionary<string, string>
        {
            ["secret"] = opciones.ClaveSecreta!,
            ["response"] = respuesta,
        };
        if (!string.IsNullOrWhiteSpace(ipCliente))
        {
            datos["remoteip"] = ipCliente;
        }

        try
        {
            using var contenido = new FormUrlEncodedContent(datos);
            using var respuestaHttp = await http.PostAsync(UrlVerificacion, contenido, cancellationToken);
            if (!respuestaHttp.IsSuccessStatusCode)
            {
                logger.LogWarning("Turnstile respondió con el código HTTP {Codigo}.", (int)respuestaHttp.StatusCode);
                return false;
            }

            var resultado = await respuestaHttp.Content.ReadFromJsonAsync<RespuestaTurnstile>(cancellationToken);
            if (resultado?.Exito != true)
            {
                // Los códigos de error de Cloudflare no contienen datos personales.
                logger.LogInformation(
                    "Turnstile rechazó una verificación: {Codigos}.", string.Join(", ", resultado?.CodigosError ?? []));
                return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "No se pudo contactar a Turnstile para verificar una solicitud de cita.");
            return false;
        }
    }

    private sealed record RespuestaTurnstile(
        [property: JsonPropertyName("success")] bool Exito,
        [property: JsonPropertyName("error-codes")] string[]? CodigosError);
}
