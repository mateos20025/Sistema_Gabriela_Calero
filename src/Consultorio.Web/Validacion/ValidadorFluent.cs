using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Consultorio.Web.Validacion;

/// <summary>
/// Conecta un <c>EditForm</c> de Blazor con el validador de FluentValidation de su modelo.
/// Se usa en las páginas estáticas de cuenta: <c>&lt;EditForm Model="..."&gt;&lt;ValidadorFluent /&gt;...</c>
/// </summary>
public sealed class ValidadorFluent : ComponentBase, IDisposable
{
    private ValidationMessageStore? mensajes;
    private IValidator? validador;

    [CascadingParameter]
    private EditContext EditContext { get; set; } = default!;

    [Inject]
    private IServiceProvider Servicios { get; set; } = default!;

    protected override void OnInitialized()
    {
        if (EditContext is null)
        {
            throw new InvalidOperationException($"{nameof(ValidadorFluent)} debe ir dentro de un EditForm.");
        }

        var tipoValidador = typeof(IValidator<>).MakeGenericType(EditContext.Model.GetType());
        validador = (IValidator)Servicios.GetRequiredService(tipoValidador);
        mensajes = new ValidationMessageStore(EditContext);
        EditContext.OnValidationRequested += AlSolicitarValidacion;
    }

    private void AlSolicitarValidacion(object? sender, ValidationRequestedEventArgs e)
    {
        mensajes!.Clear();

        var resultado = validador!.Validate(new ValidationContext<object>(EditContext.Model));
        foreach (var error in resultado.Errors)
        {
            mensajes.Add(EditContext.Field(error.PropertyName), error.ErrorMessage);
        }

        EditContext.NotifyValidationStateChanged();
    }

    public void Dispose()
    {
        if (EditContext is not null)
        {
            EditContext.OnValidationRequested -= AlSolicitarValidacion;
        }
    }
}
