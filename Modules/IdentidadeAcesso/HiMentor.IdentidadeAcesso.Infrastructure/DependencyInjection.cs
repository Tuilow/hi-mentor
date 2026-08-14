using HiMentor.IdentidadeAcesso.Application.Interfaces;
using HiMentor.IdentidadeAcesso.Domain.Interfaces;
using HiMentor.IdentidadeAcesso.Infrastructure.Repositories;
using HiMentor.IdentidadeAcesso.Infrastructure.Services;
using HiMentor.SharedKernel.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace HiMentor.IdentidadeAcesso.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registra repositórios e serviços do módulo IdentidadeAcesso. Chamar no Host, depois de
    /// registrar o DbContext concreto (os repositórios pedem só DbContext no construtor).
    /// </summary>
    public static IServiceCollection AddIdentidadeAcessoInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        // Auditoria da reemissao de link de acesso pelo painel administrativo ("Cursos e
        // acessos") -- ver AdminCourseAccessAuditLog / ReissueCourseAccessLinkCommandHandler.
        services.AddScoped<IAdminCourseAccessAuditLogRepository, AdminCourseAccessAuditLogRepository>();

        services.AddScoped<IJwtService, JwtService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }
}
