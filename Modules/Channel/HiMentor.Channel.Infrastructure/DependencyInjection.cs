using HiMentor.Channel.Application.Interfaces;
using HiMentor.Channel.Domain.Interfaces;
using HiMentor.Channel.Infrastructure.Repositories;
using HiMentor.Channel.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HiMentor.Channel.Infrastructure;

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
