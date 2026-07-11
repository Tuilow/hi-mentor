using MediatR;

namespace Tuilow.CreatorStudio.Application.Commands.PublishProduct;

/// <summary>Passo 7 do assistente — botão "Publicar Produto".</summary>
public sealed record PublishProductCommand(Guid CourseId, Guid InstructorId) : IRequest;
