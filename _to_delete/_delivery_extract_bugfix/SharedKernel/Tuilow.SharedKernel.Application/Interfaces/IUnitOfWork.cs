namespace Tuilow.SharedKernel.Application.Interfaces;

/// <summary>Reaproveitado de Tuilow.Domain.Common.Interfaces.IUnitOfWork — movido para o SharedKernel.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Tenta salvar as alterações pendentes. Se a falha for causada por uma violação de restrição
    /// única (duas requisições concorrentes descobrindo e tentando inserir o mesmo registro ao
    /// mesmo tempo — ex.: dois GET simultâneos ao mesmo documento novo de onboarding financeiro),
    /// devolve <c>false</c> em vez de propagar a exceção, e descarta o rastreamento de entidades
    /// desta unidade de trabalho (equivalente a <c>ChangeTracker.Clear()</c>) para que quem
    /// chamou possa reconsultar o estado atual do banco e tentar de novo. Qualquer outra falha
    /// (erro de conexão, violação de regra de banco não relacionada a concorrência, etc.) continua
    /// sendo propagada normalmente — nunca é engolida silenciosamente.
    /// </summary>
    Task<bool> TrySaveChangesAsync(CancellationToken ct = default);
}
