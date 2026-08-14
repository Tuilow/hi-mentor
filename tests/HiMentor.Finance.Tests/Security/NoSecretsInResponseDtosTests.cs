using System.Reflection;
using Xunit;

namespace HiMentor.Finance.Tests.Security;

/// <summary>
/// Item 8 do briefing de onboarding financeiro: "a API Key nunca pode aparecer em DTO de
/// Application/Api, nunca ser logada, nunca sair de Infrastructure". Este teste automatiza a
/// verificação final descrita no plano ("grep no diff inteiro por ApiKey/AccessToken fora de
/// Domain/Infrastructure") para os tipos que realmente cruzam a fronteira Application -&gt; Api -&gt;
/// cliente HTTP: qualquer record "*Result"/"*Response"/"*Item" público em
/// HiMentor.Finance.Application.Commands/Queries.
///
/// Escopo deliberadamente EXCLUI:
/// - HiMentor.Finance.Application.Interfaces (IAsaasSubaccountClient e seus DTOs, ex.
///   CreateAsaasSubaccountResult.ApiKey) -- esse é o contrato entre Infrastructure (que fala HTTP
///   com a Asaas) e o Handler, que protege o valor imediatamente após recebê-lo (ver
///   StartCreatorFinancialOnboardingCommandHandler) e nunca o repassa adiante. Já documentado na
///   própria XML doc da interface.
/// - Tipos "*Command" (ex. ConnectCreatorAsaasAccountCommand.ApiKey) -- são ENTRADA do creator no
///   fluxo legado de "cole sua API Key" (ver CreatorAsaasAccount), não saída do sistema. Esse
///   fluxo está sendo substituído pelo novo onboarding, mas o teste de segurança aqui trata da
///   direção que importa: o que a HiMentor devolve para fora, nunca o que o usuário manda pra
///   dentro de um fluxo que já existia antes desta mudança.
/// </summary>
public class NoSecretsInResponseDtosTests
{
    private static readonly string[] ForbiddenSubstrings = ["apikey", "accesstoken", "access_token"];

    [Fact]
    public void ResponseDtos_NeverExposeApiKeyOrAccessTokenProperties()
    {
        var assembly = typeof(HiMentor.Finance.Application.Commands.StartCreatorFinancialOnboarding.StartCreatorFinancialOnboardingCommand).Assembly;

        var responseTypes = assembly.GetTypes()
            .Where(t => t.IsPublic)
            .Where(t => t.Namespace is not null &&
                        (t.Namespace.StartsWith("HiMentor.Finance.Application.Commands", StringComparison.Ordinal) ||
                         t.Namespace.StartsWith("HiMentor.Finance.Application.Queries", StringComparison.Ordinal)))
            .Where(t => !t.Namespace!.StartsWith("HiMentor.Finance.Application.Interfaces", StringComparison.Ordinal))
            .Where(t => t.Name.EndsWith("Result", StringComparison.Ordinal) ||
                        t.Name.EndsWith("Response", StringComparison.Ordinal) ||
                        t.Name.EndsWith("Item", StringComparison.Ordinal))
            .ToList();

        // Falha alto se a lista ficar vazia -- sinal de que a convenção de nomes mudou e o filtro
        // acima parou de encontrar os DTOs de verdade (falso-negativo silencioso é pior que nada).
        Assert.True(responseTypes.Count >= 5, $"Esperava encontrar ao menos 5 tipos de resposta em Finance.Application, encontrou {responseTypes.Count} -- filtro pode estar desatualizado.");

        var offenders = new List<string>();
        foreach (var type in responseTypes)
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var normalized = property.Name.Replace("_", "", StringComparison.Ordinal).ToLowerInvariant();
                if (ForbiddenSubstrings.Any(forbidden => normalized.Contains(forbidden, StringComparison.Ordinal)))
                    offenders.Add($"{type.FullName}.{property.Name}");
            }
        }

        Assert.True(offenders.Count == 0,
            "DTO(s) de resposta do módulo Finance expõem um campo de segredo (ApiKey/AccessToken): " + string.Join(", ", offenders));
    }
}
