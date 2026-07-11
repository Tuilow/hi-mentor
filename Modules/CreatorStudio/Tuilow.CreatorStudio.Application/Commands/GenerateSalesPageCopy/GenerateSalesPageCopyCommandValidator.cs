using FluentValidation;

namespace Tuilow.CreatorStudio.Application.Commands.GenerateSalesPageCopy;

public sealed class GenerateSalesPageCopyCommandValidator : AbstractValidator<GenerateSalesPageCopyCommand>
{
    public GenerateSalesPageCopyCommandValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.InstructorId).NotEmpty();
    }
}
