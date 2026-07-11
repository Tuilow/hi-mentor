using FluentValidation;

namespace Tuilow.CreatorStudio.Application.Commands.SetCreatorNiche;

public sealed class SetCreatorNicheCommandValidator : AbstractValidator<SetCreatorNicheCommand>
{
    public SetCreatorNicheCommandValidator()
    {
        RuleFor(x => x.Niche).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TargetAudience).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Objective).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Level).IsInEnum();
    }
}
