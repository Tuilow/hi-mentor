using Tuilow.Catalog.Application.Common;
using Tuilow.Catalog.Domain.Interfaces;
using MediatR;

namespace Tuilow.Catalog.Application.Queries.GetCategories;

public sealed class GetCategoriesQueryHandler(ICourseRepository courseRepository)
    : IRequestHandler<GetCategoriesQuery, IEnumerable<CategoryResponse>>
{
    public async Task<IEnumerable<CategoryResponse>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        // Chave em minúsculo evita duplicidade por grafia ("Produtividade" x "produtividade");
        // o nome exibido é sempre a primeira grafia encontrada (lista curada tem prioridade,
        // pois é adicionada primeiro).
        var categories = new Dictionary<string, (string Name, Dictionary<string, string> Subcategories)>();

        void AddCategory(string name)
        {
            var key = name.Trim().ToLowerInvariant();
            if (!categories.ContainsKey(key))
                categories[key] = (name.Trim(), new Dictionary<string, string>());
        }

        void AddSubcategory(string categoryName, string subcategoryName)
        {
            AddCategory(categoryName);
            var key = categoryName.Trim().ToLowerInvariant();
            var subKey = subcategoryName.Trim().ToLowerInvariant();
            categories[key].Subcategories.TryAdd(subKey, subcategoryName.Trim());
        }

        // 1) Lista curada — ver CourseCategoryTaxonomy. Sempre aparece, mesmo em uma plataforma
        // ainda sem nenhum curso publicado.
        foreach (var (category, subcategories) in CourseCategoryTaxonomy.Seed)
        {
            AddCategory(category);
            foreach (var sub in subcategories)
                AddSubcategory(category, sub);
        }

        // 2) O que os criadores já digitaram de verdade nos cursos existentes — garante que uma
        // categoria real nunca "some" do autocomplete só por não estar na lista curada.
        var used = await courseRepository.GetDistinctCategoriesAsync(ct);
        foreach (var usage in used)
        {
            if (string.IsNullOrWhiteSpace(usage.Category)) continue;

            if (string.IsNullOrWhiteSpace(usage.Subcategory))
                AddCategory(usage.Category);
            else
                AddSubcategory(usage.Category, usage.Subcategory);
        }

        return categories.Values
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponse(c.Name, c.Subcategories.Values.OrderBy(s => s).ToList()))
            .ToList();
    }
}
