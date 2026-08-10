using Tuilow.SharedKernel.Application.Interfaces;

namespace Tuilow.Finance.Tests.Fakes;

/// <summary>
/// Fake determinístico (prefixo + Base64) só para os testes conseguirem afirmar que o valor
/// persistido NUNCA é igual ao texto puro original — a implementação real usa o Data Protection
/// API do ASP.NET Core (ver ISecretProtector), que não é usável fora de um host web sem
/// infraestrutura extra.
/// </summary>
public sealed class FakeSecretProtector : ISecretProtector
{
    private const string Prefix = "fake-protected:";

    public string Protect(string plaintext) => Prefix + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plaintext));

    public string Unprotect(string protectedValue)
    {
        if (!protectedValue.StartsWith(Prefix, StringComparison.Ordinal))
            throw new InvalidOperationException("Valor não foi protegido por este fake.");
        return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(protectedValue[Prefix.Length..]));
    }
}
