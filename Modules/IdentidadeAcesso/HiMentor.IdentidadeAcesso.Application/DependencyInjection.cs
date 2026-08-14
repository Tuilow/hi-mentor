using HiMentor.SharedKernel.Application.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace HiMentor.IdentidadeAcesso.Application;

public static class DependencyInjection
{
    /// <summary>Registra MediatR, validators e behaviors deste módulo. Chamar no Host.</summary>
    public static IServiceCollection AddIdentidadeAcessoApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }
}
