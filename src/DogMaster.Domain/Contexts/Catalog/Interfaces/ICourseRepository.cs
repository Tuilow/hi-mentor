using DogMaster.Domain.Common.Interfaces;
using DogMaster.Domain.Contexts.Catalog.Entities;
using DogMaster.Domain.Contexts.Catalog.Enums;

namespace DogMaster.Domain.Contexts.Catalog.Interfaces;

public interface ICourseRepository : IRepository<Course>
{
    Task<Course?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);
    Task<(IEnumerable<Course> Items, int Total)> ListPublishedAsync(
        CourseLevel? level, string? search, int page, int pageSize, CancellationToken ct = default);
}
