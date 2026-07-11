using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.CreatorStudio.Domain.Entities;
using Tuilow.CreatorStudio.Domain.Interfaces;
using MediatR;

namespace Tuilow.CreatorStudio.Application.Commands.SaveRecordingTemplate;

public sealed class SaveRecordingTemplateCommandHandler(
    IRecordingTemplateRepository templateRepository,
    IUnitOfWork uow
) : IRequestHandler<SaveRecordingTemplateCommand, Guid>
{
    public async Task<Guid> Handle(SaveRecordingTemplateCommand request, CancellationToken ct)
    {
        RecordingTemplate template;

        if (request.TemplateId is null)
        {
            template = RecordingTemplate.Create(request.CreatorId, request.Name, request.Sections);
            await templateRepository.AddAsync(template, ct);
        }
        else
        {
            template = await templateRepository.GetByIdAsync(request.TemplateId.Value, ct)
                ?? throw new NotFoundException(nameof(RecordingTemplate), request.TemplateId.Value);

            if (template.CreatorId != request.CreatorId)
                throw new UnauthorizedException("Este template não pertence a você.");

            template.Update(request.Name, request.Sections);
            templateRepository.Update(template);
        }

        if (request.IsDefault && !template.IsDefault)
        {
            // Só pode haver um template padrão por criador — desmarca os demais antes de marcar este.
            var others = await templateRepository.ListByCreatorAsync(request.CreatorId, ct);
            foreach (var other in others.Where(t => t.Id != template.Id && t.IsDefault))
            {
                other.UnsetAsDefault();
                templateRepository.Update(other);
            }
            template.SetAsDefault();
            templateRepository.Update(template);
        }
        else if (!request.IsDefault && template.IsDefault)
        {
            template.UnsetAsDefault();
            templateRepository.Update(template);
        }

        await uow.SaveChangesAsync(ct);
        return template.Id;
    }
}
