using System.Security.Claims;
using Consultorio.Application.Cuenta;
using Consultorio.Domain.Usuarios;
using Microsoft.AspNetCore.Authorization;

namespace Consultorio.Web.Seguridad;

/// <summary>
/// Políticas de autorización del panel. Se usan con <c>[Authorize(Policy = Politicas.X)]</c>.
/// </summary>
public static class Politicas
{
    /// <summary>Doctora o asistente que ya cambió su contraseña temporal. Protege todo /admin.</summary>
    public const string Personal = nameof(Personal);

    /// <summary>Solo la doctora (usuarios, configuración y, más adelante, historia clínica).</summary>
    public const string SoloDoctora = nameof(SoloDoctora);

    public static AuthorizationBuilder AgregarPoliticasConsultorio(this AuthorizationBuilder builder) =>
        builder
            .AddPolicy(Personal, politica => politica
                .RequireAuthenticatedUser()
                .RequireRole(Roles.Doctora, Roles.Asistente)
                .RequireAssertion(contexto => !DebeCambiarPassword(contexto.User)))
            .AddPolicy(SoloDoctora, politica => politica
                .RequireAuthenticatedUser()
                .RequireRole(Roles.Doctora)
                .RequireAssertion(contexto => !DebeCambiarPassword(contexto.User)));

    public static bool DebeCambiarPassword(ClaimsPrincipal usuario) =>
        usuario.HasClaim(c => c.Type == ClaimsConsultorio.DebeCambiarPassword);
}
