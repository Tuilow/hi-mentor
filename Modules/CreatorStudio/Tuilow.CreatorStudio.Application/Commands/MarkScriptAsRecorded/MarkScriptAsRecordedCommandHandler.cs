using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.CreatorStudio.Domain.Interfaces;
using MediatR;

namespace Tuilow.CreatorStudio.Application.Commands.MarkScriptAsRecorded;

public sealed class MarkScriptAsRecordedCommandHandler(
    ILessonScriptRepository scriptRepository,
    IUnitOfWork uow
) : IRequestHandler<MarkScriptAsRecordedCommand>
{
    public async Task Handle(MarkScriptAsRecordedCommand request, CancellationToken ct)
    {
        var script = await scriptRepository.GetByIdAsync(request.ScriptId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.LessonScript), request.ScriptId);

        if (script.CreatorId != request.CreatorId)
            throw new UnauthorizedException("Este roteiro não pertence a você.");

        script.MarkAsRecorded();
        scriptRepository.Update(script);
        await uow.SaveChangesAsync(ct);
    }
}
