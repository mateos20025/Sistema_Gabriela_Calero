using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Consultorio.Infrastructure.Identidad;

/// <summary>
/// SignInManager de Identity con una regla extra: un usuario desactivado no puede iniciar sesión.
/// Identity responde "NotAllowed" y la pantalla muestra el mismo mensaje que con una contraseña incorrecta.
/// </summary>
public sealed class SignInManagerConsultorio(
    UserManager<Usuario> userManager,
    IHttpContextAccessor contextAccessor,
    IUserClaimsPrincipalFactory<Usuario> claimsFactory,
    IOptions<IdentityOptions> optionsAccessor,
    ILogger<SignInManager<Usuario>> logger,
    IAuthenticationSchemeProvider schemes,
    IUserConfirmation<Usuario> confirmation)
    : SignInManager<Usuario>(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
{
    public override async Task<bool> CanSignInAsync(Usuario user) =>
        user.Activo && await base.CanSignInAsync(user);
}
