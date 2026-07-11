using Tuilow.IdentidadeAcesso.Application;
using Tuilow.IdentidadeAcesso.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Tuilow.IdentidadeAcesso.Api;

public static class DependencyInjection
{
    /// <summary>
    /// Registra Application + Infrastructure do módulo. Chamar no Host junto com
    /// AddApplicationPart(typeof(DependencyInjection).Assembly) no AddControllers()
    /// para o Host descobrir os Controllers deste módulo.
    /// </summary>
    public static IServiceCollection AddIdentidadeAcessoModule(this IServiceCollection services)
    {
        services.AddIdentidadeAcessoApplication();
        services.AddIdentidadeAcessoInfrastructure();
        return services;
    }
}
