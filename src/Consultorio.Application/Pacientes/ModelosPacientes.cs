using Consultorio.Application.Auditoria;
using Consultorio.Domain.Pacientes;

namespace Consultorio.Application.Pacientes;

/// <summary>Filtro de la lista de pacientes. <see cref="Pagina"/> empieza en 0.</summary>
public sealed record FiltroPacientes(string? Texto, bool IncluirDeshabilitados, int Pagina, int TamanoPagina)
{
    public const int TamanoPaginaMaximo = 100;
    public const int LongitudMaximaTexto = 100;
}

/// <summary>Fila de la lista de pacientes.</summary>
public sealed record PacienteResumen(
    Guid Id,
    string Cedula,
    string Nombres,
    string Apellidos,
    string Telefono,
    string? CorreoElectronico,
    DateTime CreadoUtc,
    bool Deshabilitado);

/// <summary>Ficha completa de un paciente. Las fechas vienen en UTC.</summary>
public sealed record PacienteDetalle(
    Guid Id,
    string Cedula,
    string Nombres,
    string Apellidos,
    string Telefono,
    string? CorreoElectronico,
    DateTime ConsentimientoAceptadoUtc,
    string ConsentimientoVersion,
    OrigenRegistro ConsentimientoOrigen,
    DateTime? DeshabilitadoUtc,
    uint Version,
    DateTime CreadoUtc,
    DateTime ActualizadoUtc,
    IReadOnlyList<EventoAuditoria> Historial)
{
    public string NombreCompleto => $"{Nombres} {Apellidos}";

    public bool Deshabilitado => DeshabilitadoUtc is not null;
}

/// <summary>Mensajes de los casos de uso de pacientes (también los usan las pruebas).</summary>
public static class MensajesPacientes
{
    public const string NoExiste = "El paciente no existe.";
    public const string CedulaYaRegistrada = "Ya existe un paciente registrado con esta cédula.";
    public const string CedulaDePacienteDeshabilitado =
        "Esta cédula pertenece a un paciente deshabilitado. Búscalo activando «Mostrar deshabilitados» y habilítalo.";
    public const string ModificadoPorOtraPersona =
        "Otra persona modificó este paciente mientras lo editabas. Cierra y vuelve a abrirlo para ver los datos actuales.";
    public const string EstaDeshabilitado = "El paciente está deshabilitado. Habilítalo para poder modificar sus datos.";
    public const string YaEstabaDeshabilitado = "El paciente ya estaba deshabilitado.";
    public const string YaEstabaHabilitado = "El paciente ya está habilitado.";
}
