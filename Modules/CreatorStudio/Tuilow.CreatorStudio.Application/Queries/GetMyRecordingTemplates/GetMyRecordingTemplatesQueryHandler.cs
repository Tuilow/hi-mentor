using Tuilow.CreatorStudio.Domain.Interfaces;
using MediatR;

namespace Tuilow.CreatorStudio.Application.Queries.GetMyRecordingTemplates;

public sealed class GetMyRecordingTemplatesQueryHandler(
    IRecordingTemplateRepository templateRepository
) : IRequestHandler<GetMyRecordingTemplatesQuery, IEnumerable<RecordingTemplateResponse>>
{
    public async Task<IEnumerable<RecordingTemplateResponse>> Handle(GetMyRecordingTemplatesQuery request, CancellationToken ct)
    {
        var templates = await templateRepository.ListByCreatorAsync(request.CreatorId, ct);
        return templates.Select(t => new RecordingTemplateResponse(t.Id, t.Name, t.Sections, t.IsDefault));
    }
}
