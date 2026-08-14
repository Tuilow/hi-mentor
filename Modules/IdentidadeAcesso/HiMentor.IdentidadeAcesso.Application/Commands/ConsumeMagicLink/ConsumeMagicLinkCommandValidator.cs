using FluentValidation;

namespace HiMentor.IdentidadeAcesso.Application.Commands.ConsumeMagicLink;

public sealed class ConsumeMagicLinkCommandValidator : AbstractValidator<ConsumeMagicLinkCommand>
{
    public ConsumeMagicLinkCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
    }
}
