using MediatR;

namespace Tuilow.CreatorStudio.Application.Commands.DeleteRecordingTemplate;

public sealed record DeleteRecordingTemplateCommand(Guid TemplateId, Guid CreatorId) : IRequest;
