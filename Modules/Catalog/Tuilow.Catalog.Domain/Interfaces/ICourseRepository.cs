using Tuilow.SharedKernel.Domain.Interfaces;
using Tuilow.Catalog.Domain.Entities;
using Tuilow.Catalog.Domain.Enums;

namespace Tuilow.Catalog.Domain.Interfaces;

public interface ICourseRepository : IRepository<Course>
{
    Task<Course?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);
    Task<(IEnumerable<Course> Items, int Total)> ListPublishedAsync(
        CourseLevel? level, string? search, int page, int pageSize, CancellationToken ct = default);
    Task<IEnumerable<Course>> ListAllForAdminAsync(CancellationToken ct = default);

    /// <summary>Lista os produtos do criador (tela "Meus Produtos") — inclui todos os status.</summary>
    Task<IEnumerable<Course>> ListByInstructorAsync(Guid instructorId, CancellationToken ct = default);

    /// <summary>Busca em lote por Id — usado por outros módulos (ex.: Learning, tela "meus cursos
    /// matriculados") para resolver os dados de exibição de uma lista de CourseIds de uma vez.</summary>
    Task<IEnumerable<Course>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);

    /// <summary>
    /// Mesmo que <see cref="GetByIdsAsync"/>, mas com Modules/Lessons já carregados — usado pelo
    /// histórico de aulas assistidas (Learning), que precisa resolver o título da aula a partir
    /// do LessonId. Método separado para não pesar o GetByIdsAsync "leve" usado em telas que só
    /// precisam de título/slug/thumbnail do curso.
    /// </summary>
    Task<IEnumerable<Course>> GetByIdsWithLessonsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);

    /// <summary>Força EntityState.Added para o Module — evita DbUpdateConcurrencyException.</summary>
    Task AddModuleAsync(Module module, CancellationToken ct = default);

    /// <summary>Força EntityState.Added para a Lesson — evita DbUpdateConcurrencyException.</summary>
    Task AddLessonAsync(Lesson lesson, CancellationToken ct = default);

    /// <summary>Força EntityState.Added para o LessonAttachment — evita DbUpdateConcurrencyException.</summary>
    Task AddAttachmentAsync(LessonAttachment attachment, CancellationToken ct = default);

    /// <summary>Força EntityState.Added para o CourseFaqItem — evita DbUpdateConcurrencyException.</summary>
    Task AddFaqItemAsync(CourseFaqItem faqItem, CancellationToken ct = default);

    /// <summary>
    /// Remove explicitamente do DbContext (Course.ClearFaqItems só limpa a coleção em memória;
    /// sem isso as linhas antigas ficariam órfãs no banco em vez de serem excluídas).
    /// </summary>
    void RemoveFaqItem(CourseFaqItem faqItem);
}
