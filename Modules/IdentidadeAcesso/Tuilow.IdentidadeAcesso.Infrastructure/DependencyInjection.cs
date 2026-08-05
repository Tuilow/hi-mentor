using Tuilow.IdentidadeAcesso.Application.Interfaces;
using Tuilow.IdentidadeAcesso.Domain.Interfaces;
using Tuilow.IdentidadeAcesso.Infrastructure.Repositories;
using Tuilow.IdentidadeAcesso.Infrastructure.Services;
using Tuilow.SharedKernel.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Tuilow.IdentidadeAcesso.Infrastructure;

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
