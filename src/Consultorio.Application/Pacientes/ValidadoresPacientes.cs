using Consultorio.Domain.Pacientes;
using FluentValidation;

namespace Consultorio.Application.Pacientes;

/// <summary>
/// Reglas de los datos del paciente. Usan las mismas funciones del Domain que la entidad,
/// así el formulario y la entidad nunca se contradicen.
/// </summary>
public sealed class DatosPacienteValidador : AbstractValidator<DatosPacienteComando>
{
    public DatosPacienteValidador()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(c => c.Cedula)
            .NotEmpty().WithMessage("Escribe la cédula.")
            .Must(CedulaEcuatoriana.EsValida).WithMessage("La cédula no es válida. Revisa los 10 dígitos.");

        RuleFor(c => c.Nombres)
            .NotEmpty().WithMessage("Escribe los nombres.")
            .MaximumLength(NombrePersona.LongitudMaxima).WithMessage($"Máximo {NombrePersona.LongitudMaxima} caracteres.")
            .Must(NombrePersona.EsValido).WithMessage("Usa solo letras, espacios, apóstrofos o guiones.");

        RuleFor(c => c.Apellidos)
            .NotEmpty().WithMessage("Escribe los apellidos.")
            .MaximumLength(NombrePersona.LongitudMaxima).WithMessage($"Máximo {NombrePersona.LongitudMaxima} caracteres.")
            .Must(NombrePersona.EsValido).WithMessage("Usa solo letras, espacios, apóstrofos o guiones.");

        RuleFor(c => c.Telefono)
            .NotEmpty().WithMessage("Escribe el teléfono.")
            .Must(TelefonoEcuatoriano.EsValido)
            .WithMessage("Escribe un celular (09XXXXXXXX) o un fijo con código de provincia (02XXXXXXX).");

        RuleFor(c => c.CorreoElectronico)
            .MaximumLength(DireccionCorreo.LongitudMaxima).WithMessage("El correo electrónico es demasiado largo.")
            .Must(DireccionCorreo.EsValidoOVacio).WithMessage("El correo electrónico no es válido.");
    }
}

public sealed class RegistrarPacienteValidador : AbstractValidator<RegistrarPacienteComando>
{
    public RegistrarPacienteValidador()
    {
        Include(new DatosPacienteValidador());

        RuleFor(c => c.ConsentimientoAceptado)
            .Equal(true).WithMessage("Para registrar al paciente se necesita su autorización para tratar sus datos.");
    }
}

public sealed class ActualizarPacienteValidador : AbstractValidator<ActualizarPacienteComando>
{
    public ActualizarPacienteValidador()
    {
        Include(new DatosPacienteValidador());

        RuleFor(c => c.Id).NotEmpty().WithMessage(MensajesPacientes.NoExiste);
    }
}
