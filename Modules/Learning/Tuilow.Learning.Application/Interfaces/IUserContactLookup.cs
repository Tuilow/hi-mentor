namespace Tuilow.Learning.Application.Interfaces;

/// <summary>
/// Dados mínimos de contato necessários para notificar o usuário por e-mail/WhatsApp. LastName
/// (achado A4 da avaliação) é usado só pela emissão de certificado, que precisa do nome completo
/// do aluno para a verificação pública — default "" preserva os demais usos deste record.
/// </summary>
public sealed record UserContact(string Email, string FirstName, string? Phone = null, string LastName = "");

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
