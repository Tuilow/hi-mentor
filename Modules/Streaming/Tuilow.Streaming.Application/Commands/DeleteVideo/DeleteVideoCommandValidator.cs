using FluentValidation;

namespace Tuilow.Streaming.Application.Commands.DeleteVideo;

public sealed class DeleteVideoCommandValidator : AbstractValidator<DeleteVideoCommand>
{
    public DeleteVideoCommandValidator()
    {
        RuleFor(x => x.VideoId).NotEmpty();
        RuleFor(x => x.InstructorId).NotEmpty();
    }
}
