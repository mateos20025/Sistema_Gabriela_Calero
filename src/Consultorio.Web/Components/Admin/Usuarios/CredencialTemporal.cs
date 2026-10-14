namespace Consultorio.Web.Components.Admin.Usuarios;

/// <summary>Datos que la doctora debe entregar a una persona con contraseña nueva o restablecida.</summary>
public sealed record CredencialTemporal(string NombreCompleto, string Email, string Password);
