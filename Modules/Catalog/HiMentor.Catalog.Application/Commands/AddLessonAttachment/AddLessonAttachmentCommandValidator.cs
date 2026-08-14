using FluentValidation;

namespace HiMentor.Catalog.Application.Commands.AddLessonAttachment;

public sealed class AddLessonAttachmentCommandValidator : AbstractValidator<AddLessonAttachmentCommand>
{
    public AddLessonAttachmentCommandValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.ModuleId).NotEmpty();
        RuleFor(x => x.LessonId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.FileUrl).NotEmpty().MaximumLength(1000);
    }
}
