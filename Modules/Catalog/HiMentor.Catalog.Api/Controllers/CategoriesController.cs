using HiMentor.Catalog.Application.Queries.GetCategories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HiMentor.Catalog.Api.Controllers;

/// <summary>
/// Alimenta o autocomplete de Categoria/Subcategoria do passo 1 do assistente de criação de
/// produtos (ver GetCategoriesQueryHandler) — não é uma tabela nova, só uma lista curada mesclada
/// com o que os criadores já usaram em cursos existentes.
/// </summary>
[ApiController]
[Route("api/v1/categories")]
[Produces("application/json")]
[Authorize]
public sealed class CategoriesController(ISender sender) : ControllerBase
{
    /// <summary>Lista categorias com suas subcategorias, para busca/autocomplete.</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await sender.Send(new GetCategoriesQuery(), ct);
        return Ok(result);
    }
}
