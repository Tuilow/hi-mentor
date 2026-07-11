using FluentValidation;

namespace Tuilow.Catalog.Application.Commands.UpdateCourseBasicInfo;

public sealed class UpdateCourseBasicInfoCommandValidator : AbstractValidator<UpdateCourseBasicInfoCommand>
{
    public UpdateCourseBasicInfoCommandValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.InstructorId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.Category).MaximumLength(100);
        RuleFor(x => x.Subcategory).MaximumLength(100);
        RuleFor(x => x.ShortDescription).MaximumLength(500);
    }
}
