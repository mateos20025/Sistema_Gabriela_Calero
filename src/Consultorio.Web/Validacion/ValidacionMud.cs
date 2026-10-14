using FluentValidation;

namespace Consultorio.Web.Validacion;

public static class ValidacionMud
{
    /// <summary>
    /// Adapta un validador de FluentValidation al formato que espera <c>MudForm.Validation</c>.
    /// MudForm lo llama campo por campo, por eso solo se valida la propiedad indicada.
    /// </summary>
    public static Func<object, string, Task<IEnumerable<string>>> ParaMudForm<T>(this IValidator<T> validador) =>
        async (modelo, propiedad) =>
        {
            var contexto = ValidationContext<T>.CreateWithOptions((T)modelo, o => o.IncludeProperties(propiedad));
            var resultado = await validador.ValidateAsync(contexto);
            return resultado.IsValid ? [] : resultado.Errors.Select(e => e.ErrorMessage);
        };
}
