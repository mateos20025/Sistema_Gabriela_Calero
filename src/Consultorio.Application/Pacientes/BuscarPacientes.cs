using Consultorio.Application.Abstracciones;
using Consultorio.Application.Comun;

namespace Consultorio.Application.Pacientes;

/// <summary>Caso de uso: lista paginada de pacientes con búsqueda por nombre, cédula o teléfono.</summary>
public sealed class BuscarPacientes(IRepositorioPacientes repositorio, IUsuarioActual usuarioActual)
{
    public async Task<PaginaResultado<PacienteResumen>> EjecutarAsync(FiltroPacientes filtro, CancellationToken cancellationToken)
    {
        await usuarioActual.ExigirSesionAsync();

        var texto = filtro.Texto?.Trim();
        if (texto?.Length > FiltroPacientes.LongitudMaximaTexto)
        {
            texto = texto[..FiltroPacientes.LongitudMaximaTexto];
        }

        // Límites del lado del servidor: la pantalla no puede pedir páginas enormes ni textos larguísimos.
        var filtroSeguro = filtro with
        {
            Texto = string.IsNullOrEmpty(texto) ? null : texto,
            Pagina = Math.Max(0, filtro.Pagina),
            TamanoPagina = Math.Clamp(filtro.TamanoPagina, 1, FiltroPacientes.TamanoPaginaMaximo),
        };

        return await repositorio.BuscarAsync(filtroSeguro, cancellationToken);
    }
}
