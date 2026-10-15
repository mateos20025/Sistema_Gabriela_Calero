using Consultorio.Domain.Pagos;
using FluentValidation;

namespace Consultorio.Application.Pagos;

public sealed class RegistrarPagoValidador : AbstractValidator<RegistrarPagoComando>
{
    public RegistrarPagoValidador()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(c => c.PacienteId)
            .NotEmpty().WithMessage("Elige el paciente.");

        RuleFor(c => c.Metodo)
            .IsInEnum().WithMessage("Elige el método de pago.");

        RuleFor(c => c.Monto)
            .NotEmpty().WithMessage("Escribe el monto.")
            .Must(m => MontoDolares.TryParse(m, out _))
            .WithMessage($"Escribe un monto válido, por ejemplo 35,00 (máximo {MontoDolares.Maximo:N0}).");

        RuleFor(c => c.Concepto)
            .NotEmpty().WithMessage("Escribe el concepto, por ejemplo \"Consulta médica\".")
            .Must(Pago.ConceptoEsValido).WithMessage($"Máximo {Pago.LongitudMaximaConcepto} caracteres.");

        RuleFor(c => c.NumeroComprobante)
            .Must(Pago.NumeroComprobanteEsValido)
            .WithMessage($"Usa solo letras, números y guiones (máximo {Pago.LongitudMaximaNumeroComprobante}).");
    }
}

public sealed class ConfiguracionPagosValidador : AbstractValidator<ConfiguracionPagosComando>
{
    public ConfiguracionPagosValidador()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(c => c.PrecioConsulta)
            .Must(p => string.IsNullOrWhiteSpace(p) || MontoDolares.TryParse(p, out _))
            .WithMessage("Escribe un precio válido, por ejemplo 35,00, o déjalo vacío.");

        RuleFor(c => c.ConceptoPredeterminado)
            .Must(c => string.IsNullOrWhiteSpace(c) || Pago.ConceptoEsValido(c))
            .WithMessage($"Máximo {Pago.LongitudMaximaConcepto} caracteres.");

        When(c => c.RecibeTransferencias, () =>
        {
            RuleFor(c => c.Banco)
                .Must(b => DatosBancarios.TextoValido(b) is not null)
                .WithMessage("Escribe el nombre del banco.");

            RuleFor(c => c.TipoCuenta)
                .IsInEnum().WithMessage("Elige el tipo de cuenta.");

            RuleFor(c => c.NumeroCuenta)
                .Must(n => DatosBancarios.NormalizarNumeroCuenta(n) is not null)
                .WithMessage($"El número de cuenta debe tener entre {DatosBancarios.LongitudMinimaNumeroCuenta} y {DatosBancarios.LongitudMaximaNumeroCuenta} dígitos.");

            RuleFor(c => c.Titular)
                .Must(t => DatosBancarios.TextoValido(t) is not null)
                .WithMessage("Escribe el nombre del titular de la cuenta.");

            RuleFor(c => c.IdentificacionTitular)
                .Must(i => DatosBancarios.NormalizarIdentificacion(i) is not null)
                .WithMessage("Escribe una cédula (10 dígitos) o un RUC (13 dígitos terminado en 001) válido.");
        });
    }
}
