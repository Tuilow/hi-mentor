using FluentValidation;

namespace Tuilow.Catalog.Application.Commands.SetCoursePrice;

public sealed class SetCoursePriceCommandValidator : AbstractValidator<SetCoursePriceCommand>
{
    public SetCoursePriceCommandValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.InstructorId).NotEmpty();
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}
