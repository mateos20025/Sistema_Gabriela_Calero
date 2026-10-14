namespace Consultorio.Application.Usuarios;

/// <summary>Fila de la lista de usuarios. Las fechas vienen en UTC; la pantalla las muestra en hora de Ecuador.</summary>
public sealed record UsuarioResumen(
    Guid Id,
    string NombreCompleto,
    string Email,
    string Rol,
    bool Activo,
    bool Bloqueado,
    bool DebeCambiarPassword,
    DateTime? UltimoAccesoUtc,
    DateTime CreadoUtc);
