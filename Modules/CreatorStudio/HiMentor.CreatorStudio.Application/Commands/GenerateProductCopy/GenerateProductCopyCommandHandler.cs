using HiMentor.CreatorStudio.Application.Interfaces;
using MediatR;

namespace HiMentor.CreatorStudio.Application.Commands.GenerateProductCopy;

public sealed class GenerateProductCopyCommandHandler(
    IAiContentGenerator aiContentGenerator
) : IRequestHandler<GenerateProductCopyCommand, ProductCopySuggestion>
{
    public Task<ProductCopySuggestion> Handle(GenerateProductCopyCommand request, CancellationToken ct) =>
        aiContentGenerator.GenerateProductCopyAsync(request.ProductName, request.Category, request.Subcategory, ct);
}
