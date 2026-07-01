using FluentValidation;

namespace Tuilow.Application.Contexts.Profiles.Commands.RegisterLearnerProfile;

public sealed class RegisterLearnerProfileCommandValidator : AbstractValidator<RegisterLearnerProfileCommand>
{
    public RegisterLearnerProfileCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BirthDate)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .When(x => x.BirthDate.HasValue);
    }
}
