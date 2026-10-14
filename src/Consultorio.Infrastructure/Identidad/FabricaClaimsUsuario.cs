using System.Security.Claims;
using Consultorio.Application.Cuenta;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Consultorio.Infrastructure.Identidad;

/// <summary>
/// Arma la identidad que se guarda (cifrada) en la cookie de sesión.
/// Además de lo estándar (id, correo, roles, sello de seguridad) agrega el nombre completo y,
/// si corresponde, la marca de "debe cambiar la contraseña".
/// </summary>
public sealed class FabricaClaimsUsuario(
    UserManager<Usuario> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<Usuario, IdentityRole<Guid>>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(Usuario user)
    {
        var identidad = await base.GenerateClaimsAsync(user);

        identidad.AddClaim(new Claim(ClaimsConsultorio.NombreCompleto, user.NombreCompleto));

        if (user.DebeCambiarPassword)
        {
            identidad.AddClaim(new Claim(ClaimsConsultorio.DebeCambiarPassword, "true"));
        }

        return identidad;
    }
}
