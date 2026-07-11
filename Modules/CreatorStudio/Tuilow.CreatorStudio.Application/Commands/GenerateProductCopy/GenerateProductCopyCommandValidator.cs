using FluentValidation;

namespace Tuilow.CreatorStudio.Application.Commands.GenerateProductCopy;

public sealed class GenerateProductCopyCommandValidator : AbstractValidator<GenerateProductCopyCommand>
{
    public GenerateProductCopyCommandValidator()
    {
        RuleFor(x => x.ProductName).NotEmpty().MaximumLength(200);
    }
}
