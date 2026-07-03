using Tuilow.CreatorStudio.Application.Interfaces;
using MediatR;

namespace Tuilow.CreatorStudio.Application.Commands.GenerateSalesPageCopy;

/// <summary>Passo 6 do assistente — sugestão de Título/Subtítulo/Benefícios/FAQ/CTA da página de vendas.</summary>
public sealed record GenerateSalesPageCopyCommand(
    Guid CourseId,
    Guid InstructorId
) : IRequest<SalesPageSuggestion>;
