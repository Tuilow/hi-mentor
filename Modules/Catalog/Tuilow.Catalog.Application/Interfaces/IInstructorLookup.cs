namespace Tuilow.Catalog.Application.Interfaces;

/// <summary>Dados públicos mínimos do instrutor, para exibir "Sobre o Professor" na página de vendas.</summary>
public sealed record InstructorProfile(string DisplayName, string? AvatarUrl, string? Bio);

/// <summary>
/// Porta (anti-corruption layer) que abstrai "quem é o instrutor deste curso?" sem o módulo
/// Catalog depender diretamente do domínio de IdentidadeAcesso — mesmo padrão de
/// Learning.Application.Interfaces.IUserContactLookup. Usada pela página de vendas pública
/// (GetCourseBySlugQuery) para compor o bloco "Sobre o Professor".
/// </summary>
public interface IInstructorLookup
{
    Task<InstructorProfile?> GetProfileAsync(Guid instructorId, CancellationToken ct = default);
}
