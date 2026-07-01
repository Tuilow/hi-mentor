using Tuilow.Domain.Common.Interfaces;
using Tuilow.Domain.Contexts.Catalog.Entities;
using Tuilow.Domain.Contexts.Catalog.Enums;

namespace Tuilow.Domain.Contexts.Catalog.Interfaces;

public interface ICourseRepository : IRepository<Course>
{
    Task<Course?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);
    Task<(IEnumerable<Course> Items, int Total)> ListPublishedAsync(
        CourseLevel? level, string? search, int page, int pageSize, CancellationToken ct = default);
    Task<IEnumerable<Course>> ListAllForAdminAsync(CancellationToken ct = default);

    /// <summary>Força EntityState.Added para o Module — evita DbUpdateConcurrencyException.</summary>
    Task AddModuleAsync(Module module, CancellationToken ct = default);

    /// <summary>Força EntityState.Added para a Lesson — evita DbUpdateConcurrencyException.</summary>
    Task AddLessonAsync(Lesson lesson, CancellationToken ct = default);
}
