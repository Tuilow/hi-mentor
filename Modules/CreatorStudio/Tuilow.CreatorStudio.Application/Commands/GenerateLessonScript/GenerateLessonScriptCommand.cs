using Tuilow.CreatorStudio.Application.Interfaces;
using Tuilow.CreatorStudio.Domain.Enums;
using MediatR;

namespace Tuilow.CreatorStudio.Application.Commands.GenerateLessonScript;

/// <summary>Estúdio do Criador, passo 3 — "Gerador de Roteiros de Gravação" para uma aula específica.</summary>
public sealed record GenerateLessonScriptCommand(
    string LessonTitle,
    string Niche,
    string TargetAudience,
    AudienceLevel Level
) : IRequest<LessonScriptSuggestion>;
