namespace Tuilow.Learning.Application.Interfaces;

/// <summary>Dados mínimos de contato necessários para notificar o usuário por e-mail.</summary>
public sealed record UserContact(string Email, string FirstName);

/// <summary>
/// Porta (anti-corruption layer) que abstrai "qual o e-mail/nome deste usuário?" sem o módulo
/// Learning depender diretamente do domínio de IdentidadeAcesso — mesmo padrão de
/// <see cref="ICourseAccessChecker"/> (que abstrai o módulo Sales). Usado pelos EventHandlers
/// que liberam acesso ao curso e precisam avisar o aluno por e-mail.
/// </summary>
public interface IUserContactLookup
{
    Task<UserContact?> GetContactAsync(Guid userId, CancellationToken ct = default);
}
