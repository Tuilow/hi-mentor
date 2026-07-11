using MediatR;

namespace Tuilow.CreatorStudio.Application.Queries.GetMyRecordingTemplates;

public sealed record GetMyRecordingTemplatesQuery(Guid CreatorId) : IRequest<IEnumerable<RecordingTemplateResponse>>;

public sealed record RecordingTemplateResponse(
    Guid Id,
    string Name,
    IReadOnlyList<string> Sections,
    bool IsDefault
);
