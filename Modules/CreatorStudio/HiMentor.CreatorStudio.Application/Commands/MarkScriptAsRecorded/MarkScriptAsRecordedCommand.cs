using MediatR;

namespace HiMentor.CreatorStudio.Application.Commands.MarkScriptAsRecorded;

/// <summary>Marca que o criador gravou usando este roteiro — conta para o progresso do Clone do Professor.</summary>
public sealed record MarkScriptAsRecordedCommand(Guid ScriptId, Guid CreatorId) : IRequest;
