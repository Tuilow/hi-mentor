namespace Tuilow.SharedKernel.Application.Interfaces;

/// <summary>
/// Abstrai criptografia de segredos sensiveis persistidos no banco -- hoje usado so pela API Key
/// da conta Asaas externa que cada creator conecta para o marketplace de split de pagamentos
/// (ver Tuilow.Finance.Domain.Entities.CreatorAsaasAccount). Nunca persistir esse tipo de valor
/// em texto puro, nunca logar o valor decriptado, nunca devolver em DTO de Application/Api.
///
/// Implementacao (SharedKernel.Infrastructure) usa o Data Protection API nativo do ASP.NET Core.
/// As chaves mestras do Data Protection sao persistidas no proprio Postgres (ver Program.cs --
/// AddDataProtection().PersistKeysToDbContext&lt;AppDbContext&gt;()) para sobreviver a redeploys
/// em containers efemeros (Railway apaga o filesystem local a cada deploy -- sem isso, toda API
/// Key protegida ficaria irrecuperavel no primeiro redeploy). Ver "pontos de atencao" no relatorio
/// final: sem um KMS/HSM dedicado (Azure Key Vault, AWS KMS) ou um certificado proprio via
/// ProtectKeysWithCertificate, a chave mestra fica no mesmo banco que o valor protegido -- ainda
/// assim muito melhor que texto puro, mas nao e defesa em profundidade completa.
/// </summary>
public interface ISecretProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedValue);
}
