using FluentValidation;

namespace HiMentor.IdentidadeAcesso.Application.Commands.ResendAccessLink;

public sealed class ResendAccessLinkCommandValidator : AbstractValidator<ResendAccessLinkCommand>
{
    public ResendAccessLinkCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail é obrigatório.")
            .EmailAddress().WithMessage("E-mail inválido.");
    }
}
