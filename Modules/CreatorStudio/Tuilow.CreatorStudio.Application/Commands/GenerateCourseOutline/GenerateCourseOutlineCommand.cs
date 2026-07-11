using Tuilow.CreatorStudio.Application.Interfaces;
using Tuilow.CreatorStudio.Domain.Enums;
using MediatR;

namespace Tuilow.CreatorStudio.Application.Commands.GenerateCourseOutline;

/// <summary>
/// Estúdio do Criador, passo 2 — "Gerar estrutura do curso". Não depende de o perfil de nicho
/// já estar salvo (o criador pode gerar antes de confirmar o nicho), mesmo espírito de
/// GenerateProductCopyCommand.
/// </summary>
public sealed record GenerateCourseOutlineCommand(
    string Niche,
    string TargetAudience,
    string Objective,
    AudienceLevel Level
) : IRequest<CourseOutlineSuggestion>;
