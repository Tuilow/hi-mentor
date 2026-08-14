using MediatR;

namespace HiMentor.Catalog.Application.Queries.GetCategories;

/// <summary>
/// Alimenta o autocomplete de Categoria/Subcategoria do passo 1 do assistente de criação
/// (ver GetCategoriesQueryHandler) — mescla a lista curada (CourseCategoryTaxonomy) com o que os
/// criadores já usaram em cursos existentes, sem exigir tabela nova nem migração de banco.
/// </summary>
public sealed record GetCategoriesQuery : IRequest<IEnumerable<CategoryResponse>>;

public sealed record CategoryResponse(string Name, IReadOnlyList<string> Subcategories);
