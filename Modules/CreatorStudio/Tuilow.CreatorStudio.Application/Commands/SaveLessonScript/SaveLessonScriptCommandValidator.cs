using FluentValidation;

namespace Tuilow.CreatorStudio.Application.Commands.SaveLessonScript;

public sealed class SaveLessonScriptCommandValidator : AbstractValidator<SaveLessonScriptCommand>
{
    public SaveLessonScriptCommandValidator()
    {
        RuleFor(x => x.LessonTitle).NotEmpty().MaximumLength(300);
    }
}
