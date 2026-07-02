using Tuilow.Catalog.Domain.Entities;
using Tuilow.Catalog.Domain.Enums;
using Tuilow.Catalog.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Tuilow.Catalog.Infrastructure.Repositories;

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
            .FirstOrDefaultAsync(c => c.Slug == slug, ct); // sem filtro de status — permite acesso a rascunhos pelo player

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default) =>
        await context.Set<Course>().AnyAsync(c => c.Slug == slug, ct);

    public async Task<IEnumerable<Course>> ListAllForAdminAsync(CancellationToken ct = default) =>
        await context.Set<Course>()
            .Include(c => c.Modules)
            .ThenInclude(m => m.Lessons)
            .OrderByDescending(c => c.CreatedAt)
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
}
