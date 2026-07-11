namespace Tuilow.Catalog.Domain.Entities;

/// <summary>
/// Depoimento de aluno exibido na página de vendas pública. Lista simples, sem entidade filha
/// própria — mapeada como JSON numa única coluna (ver CourseConfiguration), mesma técnica já
/// usada para SalesPageBenefits e para SocialLink (módulo Channel).
/// </summary>
public sealed record Testimonial(string AuthorName, string? AuthorRole, string Quote, string? AvatarUrl);
