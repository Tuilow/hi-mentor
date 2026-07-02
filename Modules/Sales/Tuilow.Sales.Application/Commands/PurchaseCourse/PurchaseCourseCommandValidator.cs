using FluentValidation;

namespace Tuilow.Sales.Application.Commands.PurchaseCourse;

public sealed class PurchaseCourseCommandValidator : AbstractValidator<PurchaseCourseCommand>
{
    public PurchaseCourseCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress();
    }
}
