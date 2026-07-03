using Tuilow.CreatorStudio.Application.Interfaces;
using MediatR;

namespace Tuilow.CreatorStudio.Application.Commands.GenerateProductCopy;

/// <summary>
/// Passo 1 do assistente — botão "Gerar com IA". Não depende de o produto já existir (o
/// criador pode gerar a copy antes mesmo de salvar o rascunho) — só usa Nome/Categoria/
/// Subcategoria como contexto, como pedido na especificação.
/// </summary>
public sealed record GenerateProductCopyCommand(
    string ProductName,
    string? Category,
    string? Subcategory
) : IRequest<ProductCopySuggestion>;
