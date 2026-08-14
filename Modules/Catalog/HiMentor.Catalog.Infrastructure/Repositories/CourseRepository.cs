using HiMentor.Catalog.Domain.Entities;
using HiMentor.Catalog.Domain.Enums;
using HiMentor.Catalog.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HiMentor.Catalog.Infrastructure.Repositories;

/// <summary>
/// Recebe o DbContext genérico (não o concreto do Host) — mantém o módulo desacoplado.
/// O Host resolve "DbContext" para o AppDbContext real via DI.
/// </summary>
public sealed class CourseRepository(DbContext context) : ICourseRepository
{
    public async Task<Course?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Set<Course>()
            .Include(c => c.Modules)
            .ThenInclude(m => m.Lessons)
            .ThenInclude(l => l.Attachments)
            .Include(c => c.Modules)
            .ThenInclude(m => m.Lessons)
            .ThenInclude(l => l.Exercises)
            .Include(c => c.FaqItems)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IEnumerable<Course>> GetAllAsync(CancellationToken ct = default) =>
        await context.Set<Course>().ToListAsync(ct);

    public async Task AddAsync(Course entity, CancellationToken ct = default) =>
        await context.Set<Course>().AddAsync(entity, ct);

    public void Update(Course entity) => context.Set<Course>().Update(entity);
    public void Delete(Course entity) => context.Set<Course>().Remove(entity);

    public async Task<Course?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        await context.Set<Course>()
            .Include(c => c.Modules)
            .ThenInclude(m => m.Lessons)
            .ThenInclude(l => l.Attachments)
            .Include(c => c.Modules)
            .ThenInclude(m => m.Lessons)
            .ThenInclude(l => l.Exercises)
            .Include(c => c.FaqItems)
            .FirstOrDefaultAsync(c => c.Slug == slug, ct); // sem filtro de status — permite acesso a rascunhos pelo player

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default) =>
        await context.Set<Course>().AnyAsync(c => c.Slug == slug, ct);

    public async Task<IEnumerable<Course>> ListAllForAdminAsync(CancellationToken ct = default) =>
        await context.Set<Course>()
            .Include(c => c.Modules)
            .ThenInclude(m => m.Lessons)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    /// <summary>Tela "Meus Produtos" — todos os status, ordenados por mais recente.</summary>
    public async Task<IEnumerable<Course>> ListByInstructorAsync(Guid instructorId, CancellationToken ct = default) =>
        await context.Set<Course>()
            .Where(c => c.InstructorId == instructorId)
            .Include(c => c.Modules)
            .ThenInclude(m => m.Lessons)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    /// <summary>Alimenta o autocomplete de categorias (GetCategoriesQueryHandler) com o que os
    /// criadores já digitaram de verdade — sem isso, uma categoria fora da lista curada nunca
    /// apareceria como sugestão para os próximos cursos.</summary>
    public async Task<IEnumerable<CourseCategoryUsage>> GetDistinctCategoriesAsync(CancellationToken ct = default)
    {
        // Projeta pra um tipo anônimo e materializa antes de mapear pro record — evita depender
        // do provider EF conseguir traduzir "new CourseCategoryUsage(...)" (chamada de construtor)
        // dentro da árvore de expressão do SELECT.
        var pairs = await context.Set<Course>()
            .Where(c => c.Category != null)
            .Select(c => new { c.Category, c.Subcategory })
            .Distinct()
            .ToListAsync(ct);

        return pairs.Select(p => new CourseCategoryUsage(p.Category!, p.Subcategory));
    }

    public async Task<IEnumerable<Course>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default) =>
        await context.Set<Course>()
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(ct);

    public async Task<IEnumerable<Course>> GetByIdsWithLessonsAsync(IEnumerable<Guid> ids, CancellationToken ct = default) =>
        await context.Set<Course>()
            .Where(c => ids.Contains(c.Id))
            .Include(c => c.Modules)
            .ThenInclude(m => m.Lessons)
            .ToListAsync(ct);

    /// <summary>
    /// Registra o Module explicitamente como Added no DbContext.
    /// Necessário porque DetectChanges marca entidades filhas com Guid novo como Modified.
    /// </summary>
    public async Task AddModuleAsync(Module module, CancellationToken ct = default) =>
        await context.Set<Module>().AddAsync(module, ct);

    /// <summary>
    /// Registra a Lesson explicitamente como Added no DbContext.
    /// </summary>
    public async Task AddLessonAsync(Lesson lesson, CancellationToken ct = default) =>
        await context.Set<Lesson>().AddAsync(lesson, ct);

    /// <summary>
    /// Registra o LessonAttachment explicitamente como Added no DbContext.
    /// </summary>
    public async Task AddAttachmentAsync(LessonAttachment attachment, CancellationToken ct = default) =>
        await context.Set<LessonAttachment>().AddAsync(attachment, ct);

    /// <summary>
    /// Registra o CourseFaqItem explicitamente como Added no DbContext.
    /// </summary>
    public async Task AddFaqItemAsync(CourseFaqItem faqItem, CancellationToken ct = default) =>
        await context.Set<CourseFaqItem>().AddAsync(faqItem, ct);

    /// <summary>Remove explicitamente — evita deixar linhas órfãs ao substituir a lista de FAQ.</summary>
    public void RemoveFaqItem(CourseFaqItem faqItem) => context.Set<CourseFaqItem>().Remove(faqItem);

    public async Task<(IEnumerable<Course> Items, int Total)> ListPublishedAsync(
        CourseLevel? level, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.Set<Course>()
            .Where(c => c.Status == CourseStatus.Published)
            .AsQueryable();

        if (level.HasValue)
            query = query.Where(c => c.Level == level.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => EF.Functions.ILike(c.Title, $"%{search}%"));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(c => c.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    /// <summary>
    /// Achado M8: junção leve Attachment -> Lesson -> Module -> Course por FileUrl, sem carregar
    /// o agregado inteiro (evita os múltiplos Include de GetByIdAsync só para checar acesso a
    /// um anexo). Cada FileUrl é gerada com um Guid novo por MaterialsUploadController.Upload,
    /// então o filtro por igualdade é seletivo o bastante mesmo sem índice dedicado.
    /// </summary>
    public async Task<MaterialAccessInfo?> GetMaterialAccessInfoAsync(string fileUrl, CancellationToken ct = default)
    {
        var result = await (
            from attachment in context.Set<LessonAttachment>()
            where attachment.FileUrl == fileUrl
            join lesson in context.Set<Lesson>() on attachment.LessonId equals lesson.Id
            join module in context.Set<Module>() on lesson.ModuleId equals module.Id
            join course in context.Set<Course>() on module.CourseId equals course.Id
            select new { course.Id, course.InstructorId, lesson.IsPreview }
        ).FirstOrDefaultAsync(ct);

        return result is null ? null : new MaterialAccessInfo(result.Id, result.InstructorId, result.IsPreview);
    }
}
