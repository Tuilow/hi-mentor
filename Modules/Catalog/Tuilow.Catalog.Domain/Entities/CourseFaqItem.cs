using Tuilow.SharedKernel.Domain.Common;

namespace Tuilow.Catalog.Domain.Entities;

/// <summary>
/// Item de FAQ da página de vendas do produto. Reaproveita o padrão de entidade-filha já usado
/// em LessonAttachment/PlanFeature — sem agregado próprio, ciclo de vida atrelado ao Course.
/// Pode ser gerado por IA (rascunho) ou editado manualmente pelo criador.
/// </summary>
public sealed class CourseFaqItem : Entity
{
    public Guid CourseId { get; private set; }
    public string Question { get; private set; } = string.Empty;
    public string Answer { get; private set; } = string.Empty;
    public int Order { get; private set; }

    private CourseFaqItem() { }

    public static CourseFaqItem Create(Guid courseId, string question, string answer, int order) =>
        new()
        {
            CourseId = courseId,
            Question = question.Trim(),
            Answer = answer.Trim(),
            Order = order
        };
}
