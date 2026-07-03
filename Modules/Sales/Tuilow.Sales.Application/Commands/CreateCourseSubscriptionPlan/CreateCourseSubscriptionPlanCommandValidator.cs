using FluentValidation;

namespace Tuilow.Sales.Application.Commands.CreateCourseSubscriptionPlan;

public sealed class CreateCourseSubscriptionPlanCommandValidator : AbstractValidator<CreateCourseSubscriptionPlanCommand>
{
    public CreateCourseSubscriptionPlanCommandValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.InstructorId).NotEmpty();
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.TrialDays).GreaterThanOrEqualTo(0);
    }
}
