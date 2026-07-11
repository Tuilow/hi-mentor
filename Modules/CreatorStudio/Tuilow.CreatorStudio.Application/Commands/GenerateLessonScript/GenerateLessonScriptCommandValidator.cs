using FluentValidation;

namespace Tuilow.CreatorStudio.Application.Commands.GenerateLessonScript;

public sealed class GenerateLessonScriptCommandValidator : AbstractValidator<GenerateLessonScriptCommand>
{
    public GenerateLessonScriptCommandValidator()
    {
        RuleFor(x => x.LessonTitle).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Niche).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TargetAudience).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Level).IsInEnum();
    }
}
