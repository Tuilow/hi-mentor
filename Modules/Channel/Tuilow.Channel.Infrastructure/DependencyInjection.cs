using Tuilow.Channel.Application.Interfaces;
using Tuilow.Channel.Domain.Interfaces;
using Tuilow.Channel.Infrastructure.Repositories;
using Tuilow.Channel.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Tuilow.Channel.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra os repositórios/serviços do módulo Channel. Chamar no Host.</summary>
    public static IServiceCollection AddChannelInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ICreatorChannelRepository, CreatorChannelRepository>();
        services.AddScoped<ICreatorProfileLookup, IdentidadeAcessoCreatorProfileLookup>();
        return services;
    }
}
