using Consultorio.Application.Comun;
using Consultorio.Domain.Pacientes;

namespace Consultorio.Application.Pacientes;

/// <summary>
/// Acceso a los pacientes guardados. Application no conoce EF Core ni PostgreSQL: solo esta interfaz.
/// </summary>
public interface IRepositorioPacientes
{
    /// <summary>Incluye pacientes deshabilitados.</summary>
    Task<Paciente?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Busca por cédula ya normalizada. Incluye pacientes deshabilitados.</summary>
    Task<Paciente?> ObtenerPorCedulaAsync(string cedula, CancellationToken cancellationToken);

    Task<PaginaResultado<PacienteResumen>> BuscarAsync(FiltroPacientes filtro, CancellationToken cancellationToken);

    /// <exception cref="RegistroDuplicadoException">La cédula ya existe.</exception>
    Task AgregarAsync(Paciente paciente, CancellationToken cancellationToken);

    /// <exception cref="ConflictoConcurrenciaException">Otra persona lo modificó antes.</exception>
    /// <exception cref="RegistroDuplicadoException">La nueva cédula ya existe.</exception>
    Task ActualizarAsync(Paciente paciente, CancellationToken cancellationToken);
}
