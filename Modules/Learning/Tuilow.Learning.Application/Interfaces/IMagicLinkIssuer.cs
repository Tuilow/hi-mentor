namespace Tuilow.Learning.Application.Interfaces;

/// <summary>
/// Porta (anti-corruption layer) que emite um Magic Link de acesso sem senha para um usuário —
/// mesmo padrão de <see cref="IUserContactLookup"/>, só que grava (não só lê): usada pelo
/// EventHandler que libera acesso ao curso após pagamento confirmado, para o e-mail já entrar o
/// aluno direto na área do curso, sem precisar criar/lembrar senha.
/// </summary>
public interface IMagicLinkIssuer
{
    /// <summary>Retorna o token opaco do Magic Link recém-emitido, ou null se o usuário não existir.</summary>
    Task<string?> IssueAsync(Guid userId, CancellationToken ct = default);
}
