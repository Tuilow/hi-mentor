using FluentValidation;

namespace HiMentor.CreatorStudio.Application.Commands.SaveRecordingTemplate;

public sealed class SaveRecordingTemplateCommandValidator : AbstractValidator<SaveRecordingTemplateCommand>
{
    public SaveRecordingTemplateCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Sections).NotEmpty().WithMessage("Adicione ao menos uma seção ao template.");
    }
}
