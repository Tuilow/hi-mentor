using Tuilow.Catalog.Application.Commands.AddLesson;
using Tuilow.Catalog.Application.Commands.AddLessonAttachment;
using Tuilow.Catalog.Application.Commands.AddModule;
using Tuilow.Catalog.Application.Commands.ArchiveCourse;
using Tuilow.Catalog.Application.Commands.CreateCourse;
using Tuilow.Catalog.Application.Commands.DeleteCourse;
using Tuilow.Catalog.Application.Commands.DuplicateCourse;
using Tuilow.Catalog.Application.Commands.PublishCourse;
using Tuilow.Catalog.Application.Commands.RecordCourseView;
using Tuilow.Catalog.Application.Commands.ReorderLessons;
using Tuilow.Catalog.Application.Commands.ReorderModules;
using Tuilow.Catalog.Application.Commands.SetCoursePrice;
using Tuilow.Catalog.Application.Commands.SetCourseSalesPage;
using Tuilow.Catalog.Application.Commands.UpdateCourseBasicInfo;
using Tuilow.Catalog.Application.Queries.GetCourseBySlug;
using Tuilow.Catalog.Application.Queries.GetOtherCoursesByInstructor;
using Tuilow.Catalog.Application.Queries.ListCourses;
using Tuilow.Catalog.Domain.Enums;
using Tuilow.SharedKernel.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tuilow.Catalog.Api.Controllers;

/// <summary>
/// Endpoints públicos/instrutor do catálogo de cursos.
/// NOTA: o endpoint de playback de aula (GetLessonPlayUrl) vive em
/// Tuilow.Streaming.Api.Controllers.LessonPlaybackController (mesma rota, para não quebrar
/// o frontend) — depende do contexto Streaming, que fica em módulo separado.
/// </summary>
[ApiController]
[Route("api/v1/courses")]
[Produces("application/json")]
public sealed class CoursesController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Lista cursos publicados com filtros e paginação.</summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] CourseLevel? level,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new ListCoursesQuery(level, search, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>Retorna detalhes de um curso pelo slug.</summary>
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var course = await sender.Send(new GetCourseBySlugQuery(slug, currentUser.UserId), ct);
        return Ok(course);
    }

    /// <summary>Cria um novo curso (apenas Creator/Admin).</summary>
    [HttpPost]
    [Authorize(Roles = "Creator,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateCourseCommand command, CancellationToken ct)
    {
        var courseId = await sender.Send(
            command with { InstructorId = currentUser.UserId!.Value }, ct);
        return CreatedAtAction(nameof(GetBySlug), new { slug = "created" }, new { id = courseId });
    }

    /// <summary>Adiciona módulo ao curso.</summary>
    [HttpPost("{courseId:guid}/modules")]
    [Authorize(Roles = "Creator,Admin")]
    public async Task<IActionResult> AddModule(Guid courseId, [FromBody] AddModuleCommand command, CancellationToken ct)
    {
        var moduleId = await sender.Send(command with { CourseId = courseId, InstructorId = currentUser.UserId!.Value }, ct);
        return Ok(new { id = moduleId });
    }

    /// <summary>
    /// Achado B6 da avaliação: reordena os módulos do curso (arrastar-e-soltar no front chama
    /// este endpoint ao soltar o item — a UI de drag-and-drop em si fica fora do escopo deste
    /// achado). Exige a lista completa de IDs de módulo na nova ordem desejada.
    /// </summary>
    [HttpPut("{courseId:guid}/modules/reorder")]
    [Authorize(Roles = "Creator,Admin")]
    public async Task<IActionResult> ReorderModules(Guid courseId, [FromBody] ReorderModulesRequest request, CancellationToken ct)
    {
        await sender.Send(new ReorderModulesCommand(courseId, currentUser.UserId!.Value, request.OrderedModuleIds), ct);
        return Ok(new { message = "Módulos reordenados com sucesso." });
    }

    /// <summary>Adiciona aula ao módulo.</summary>
    [HttpPost("{courseId:guid}/modules/{moduleId:guid}/lessons")]
    [Authorize(Roles = "Creator,Admin")]
    public async Task<IActionResult> AddLesson(Guid courseId, Guid moduleId,
        [FromBody] AddLessonCommand command, CancellationToken ct)
    {
        var lessonId = await sender.Send(
            command with { CourseId = courseId, ModuleId = moduleId, InstructorId = currentUser.UserId!.Value }, ct);
        return Ok(new { id = lessonId });
    }

    /// <summary>
    /// Achado B6 da avaliação: reordena as aulas dentro de um módulo. Mesma lógica de
    /// ReorderModules, um nível abaixo — ver comentário lá.
    /// </summary>
    [HttpPut("{courseId:guid}/modules/{moduleId:guid}/lessons/reorder")]
    [Authorize(Roles = "Creator,Admin")]
    public async Task<IActionResult> ReorderLessons(Guid courseId, Guid moduleId,
        [FromBody] ReorderLessonsRequest request, CancellationToken ct)
    {
        await sender.Send(
            new ReorderLessonsCommand(courseId, moduleId, currentUser.UserId!.Value, request.OrderedLessonIds), ct);
        return Ok(new { message = "Aulas reordenadas com sucesso." });
    }

    /// <summary>Anexa um material (PDF/DOCX/PPTX/ZIP/imagem/planilha) à aula (passo 4 do assistente).</summary>
    [HttpPost("{courseId:guid}/modules/{moduleId:guid}/lessons/{lessonId:guid}/attachments")]
    [Authorize(Roles = "Creator,Admin")]
    public async Task<IActionResult> AddAttachment(Guid courseId, Guid moduleId, Guid lessonId,
        [FromBody] AddLessonAttachmentCommand command, CancellationToken ct)
    {
        var attachmentId = await sender.Send(
            command with { CourseId = courseId, ModuleId = moduleId, LessonId = lessonId, InstructorId = currentUser.UserId!.Value }, ct);
        return Ok(new { id = attachmentId });
    }

    /// <summary>Publica o curso (torna-o visível para alunos).</summary>
    [HttpPost("{courseId:guid}/publish")]
    [Authorize(Roles = "Creator,Admin")]
    public async Task<IActionResult> Publish(Guid courseId, CancellationToken ct)
    {
        await sender.Send(new PublishCourseCommand(courseId, currentUser.UserId!.Value), ct);
        return Ok(new { message = "Curso publicado com sucesso." });
    }

    /// <summary>Atualiza informações básicas do produto (passo 1 do assistente / edição posterior).</summary>
    [HttpPatch("{courseId:guid}")]
    [Authorize(Roles = "Creator,Admin")]
    public async Task<IActionResult> UpdateBasicInfo(Guid courseId,
        [FromBody] UpdateCourseBasicInfoCommand command, CancellationToken ct)
    {
        await sender.Send(command with { CourseId = courseId, InstructorId = currentUser.UserId!.Value }, ct);
        return Ok(new { message = "Produto atualizado com sucesso." });
    }

    /// <summary>Define o preço do produto — Grátis ou Pagamento único (passo 5 do assistente).</summary>
    [HttpPut("{courseId:guid}/price")]
    [Authorize(Roles = "Creator,Admin")]
    public async Task<IActionResult> SetPrice(Guid courseId,
        [FromBody] SetCoursePriceCommand command, CancellationToken ct)
    {
        await sender.Send(command with { CourseId = courseId, InstructorId = currentUser.UserId!.Value }, ct);
        return Ok(new { message = "Preço atualizado com sucesso." });
    }

    /// <summary>Define/atualiza o conteúdo da página de vendas (passo 6 do assistente).</summary>
    [HttpPut("{courseId:guid}/sales-page")]
    [Authorize(Roles = "Creator,Admin")]
    public async Task<IActionResult> SetSalesPage(Guid courseId,
        [FromBody] SetCourseSalesPageCommand command, CancellationToken ct)
    {
        await sender.Send(command with { CourseId = courseId, InstructorId = currentUser.UserId!.Value }, ct);
        return Ok(new { message = "Página de vendas atualizada com sucesso." });
    }

    /// <summary>Arquiva o produto (some do catálogo público, mas preserva histórico de alunos/vendas).</summary>
    [HttpPost("{courseId:guid}/archive")]
    [Authorize(Roles = "Creator,Admin")]
    public async Task<IActionResult> Archive(Guid courseId, CancellationToken ct)
    {
        await sender.Send(new ArchiveCourseCommand(courseId, currentUser.UserId!.Value), ct);
        return Ok(new { message = "Produto arquivado com sucesso." });
    }

    /// <summary>Duplica o produto (estrutura completa) como um novo rascunho.</summary>
    [HttpPost("{courseId:guid}/duplicate")]
    [Authorize(Roles = "Creator,Admin")]
    public async Task<IActionResult> Duplicate(Guid courseId, CancellationToken ct)
    {
        var newCourseId = await sender.Send(new DuplicateCourseCommand(courseId, currentUser.UserId!.Value), ct);
        return Ok(new { id = newCourseId });
    }

    /// <summary>Exclui o produto — só permitido para rascunhos que nunca foram publicados.</summary>
    [HttpDelete("{courseId:guid}")]
    [Authorize(Roles = "Creator,Admin")]
    public async Task<IActionResult> Delete(Guid courseId, CancellationToken ct)
    {
        await sender.Send(new DeleteCourseCommand(courseId, currentUser.UserId!.Value), ct);
        return Ok(new { message = "Produto excluído com sucesso." });
    }

    /// <summary>Registra uma visualização da página de vendas pública (anônimo).</summary>
    [HttpPost("{slug}/view")]
    [AllowAnonymous]
    public async Task<IActionResult> RecordView(string slug, CancellationToken ct)
    {
        await sender.Send(new RecordCourseViewCommand(slug), ct);
        return NoContent();
    }

    /// <summary>
    /// Cross-sell: outros cursos publicados do mesmo criador (anônimo) — usado na página do
    /// curso, na página de vendas pública e no Canal do Criador.
    /// </summary>
    [HttpGet("by-instructor/{instructorId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByInstructor(
        Guid instructorId, [FromQuery] Guid? excludeCourseId, CancellationToken ct)
    {
        var result = await sender.Send(new GetOtherCoursesByInstructorQuery(instructorId, excludeCourseId), ct);
        return Ok(result);
    }
}

/// <summary>Achado B6: lista completa dos IDs de módulo do curso, na nova ordem desejada.</summary>
public sealed record ReorderModulesRequest(IReadOnlyList<Guid> OrderedModuleIds);

/// <summary>Achado B6: lista completa dos IDs de aula do módulo, na nova ordem desejada.</summary>
public sealed record ReorderLessonsRequest(IReadOnlyList<Guid> OrderedLessonIds);
