using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.CreatorStudio.Domain.Entities;
using HiMentor.CreatorStudio.Domain.Interfaces;
using MediatR;

namespace HiMentor.CreatorStudio.Application.Commands.DeleteRecordingTemplate;

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
