using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace HiMentor.Channel.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddChannelApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
