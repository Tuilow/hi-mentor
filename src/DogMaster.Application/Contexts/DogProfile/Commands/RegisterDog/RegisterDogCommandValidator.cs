using FluentValidation;

namespace DogMaster.Application.Contexts.DogProfile.Commands.RegisterDog;

public sealed class RegisterDogCommandValidator : AbstractValidator<RegisterDogCommand>
{
    public RegisterDogCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.WeightKg).GreaterThan(0).When(x => x.WeightKg.HasValue);
        RuleFor(x => x.BirthDate)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .When(x => x.BirthDate.HasValue);
    }
}
