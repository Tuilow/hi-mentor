using Tuilow.SharedKernel.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Tuilow.SharedKernel.Infrastructure.Frontend;

/// <summary>Implementacao real de <see cref="IFrontendUrlProvider"/> -- mesma configuracao "FrontendUrl"
/// ja usada por EmailService (default preserva o comportamento se a variavel nao estiver setada).</summary>
public sealed class FrontendUrlProvider(IConfiguration configuration) : IFrontendUrlProvider
{
    private readonly string _frontendUrl = configuration["FrontendUrl"] ?? "https://app.tuilow.com.br";

    public string BuildUrl(string path) => $"{_frontendUrl}{path}";
}
