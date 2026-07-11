using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.CreatorStudio.Domain.Entities;
using Tuilow.CreatorStudio.Domain.Interfaces;
using MediatR;

namespace Tuilow.CreatorStudio.Application.Commands.DeleteRecordingTemplate;

public sealed class DeleteRecordingTemplateCommandHandler(
    IRecordingTemplateRepository templateRepository,
    IUnitOfWork uow
) : IRequestHandler<DeleteRecordingTemplateCommand>
{
    public async Task Handle(DeleteRecordingTemplateCommand request, CancellationToken ct)
    {
        var template = await templateRepository.GetByIdAsync(request.TemplateId, ct)
            ?? throw new NotFoundException(nameof(RecordingTemplate), request.TemplateId);

        if (template.CreatorId != request.CreatorId)
            throw new UnauthorizedException("Este template não pertence a você.");

        templateRepository.Delete(template);
        await uow.SaveChangesAsync(ct);
    }
}
