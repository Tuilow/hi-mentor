using FluentValidation;

namespace Tuilow.CreatorStudio.Application.Commands.CaptureLead;

public sealed class CaptureLeadCommandValidator : AbstractValidator<CaptureLeadCommand>
{
    public CaptureLeadCommandValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
    }
}
