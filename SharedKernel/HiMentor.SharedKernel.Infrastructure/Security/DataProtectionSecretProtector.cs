using HiMentor.SharedKernel.Application.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace HiMentor.SharedKernel.Infrastructure.Security;

/// <summary>Ver ISecretProtector para o racional completo (Data Protection + persistencia no Postgres).</summary>
public sealed class DataProtectionSecretProtector : ISecretProtector
{
    // Versionado (".v1") -- se um dia precisarmos trocar o esquema de protecao, uma nova purpose
    // string aposentaria esta sem quebrar a decriptacao de segredos ja gravados com a antiga
    // (o Data Protection API isola completamente protectors com purpose strings diferentes).
    private const string Purpose = "HiMentor.Finance.CreatorAsaasApiKey.v1";
    private readonly IDataProtector _protector;

    public DataProtectionSecretProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);
    public string Unprotect(string protectedValue) => _protector.Unprotect(protectedValue);
}
