using FluentValidation;

namespace HiMentor.IdentidadeAcesso.Application.Commands.ConfirmEmail;

public sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail é obrigatório.")
            .EmailAddress().WithMessage("E-mail inválido.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Código é obrigatório.")
            .Length(6).WithMessage("O código deve ter 6 dígitos.");
    }
}
