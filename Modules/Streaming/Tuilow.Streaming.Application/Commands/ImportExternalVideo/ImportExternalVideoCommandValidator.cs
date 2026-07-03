using FluentValidation;

namespace Tuilow.Streaming.Application.Commands.ImportExternalVideo;

public sealed class ImportExternalVideoCommandValidator : AbstractValidator<ImportExternalVideoCommand>
{
    public ImportExternalVideoCommandValidator()
    {
        RuleFor(x => x.Url).NotEmpty().MaximumLength(1000)
            .Must(u => Uri.TryCreate(u, UriKind.Absolute, out _))
            .WithMessage("URL inválida.");
    }
}
