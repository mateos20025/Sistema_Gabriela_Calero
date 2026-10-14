using System.Security.Claims;
using Consultorio.Application.Abstracciones;
using Consultorio.Application.Cuenta;
using Consultorio.Domain.Usuarios;
using Microsoft.AspNetCore.Components.Authorization;

namespace Consultorio.Web.Seguridad;

/// <summary>
/// Lee el usuario de la sesión. Usa <see cref="AuthenticationStateProvider"/> (y no HttpContext)
/// porque funciona tanto en páginas estáticas como en pantallas interactivas de Blazor Server.
/// </summary>
internal sealed class UsuarioActual(AuthenticationStateProvider proveedorAutenticacion) : IUsuarioActual
{
    public async Task<UsuarioSesion?> ObtenerAsync()
    {
        var estado = await proveedorAutenticacion.GetAuthenticationStateAsync();
        var usuario = estado.User;

        if (usuario.Identity?.IsAuthenticated != true
            || !Guid.TryParse(usuario.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
        {
            return null;
        }

        var nombre = usuario.FindFirstValue(ClaimsConsultorio.NombreCompleto) ?? usuario.Identity.Name ?? "";
        return new UsuarioSesion(id, nombre, usuario.IsInRole(Roles.Doctora));
    }
}
