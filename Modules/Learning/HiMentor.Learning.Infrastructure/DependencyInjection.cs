using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Learning.Application.Interfaces;
using HiMentor.Learning.Domain.Interfaces;
using HiMentor.Learning.Infrastructure.Repositories;
using HiMentor.Learning.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HiMentor.Learning.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra os repositórios/serviços do módulo Learning. Chamar no Host.</summary>
    public static IServiceCollection AddLearningInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
        // Achado A4 da avaliação: Certificate existia no domínio mas nunca era instanciado — ver
        // CourseCompletedEventHandler (Learning.Application/EventHandlers), que agora emite o
        // certificado reagindo à conclusão do curso.
        services.AddScoped<ICertificateRepository, CertificateRepository>();
        // Feature 12/08/2026 ("Baixar certificado"): geração do PDF sob demanda, ver
        // QuestPdfCertificateGenerator e ICertificatePdfGenerator (Application/Interfaces).
        services.AddScoped<ICertificatePdfGenerator, QuestPdfCertificateGenerator>();
        services.AddScoped<ICourseAccessChecker, SalesCourseAccessChecker>();
        // Serviço único de "o usuário tem acesso a este curso?" (SharedKernel — consumido por
        // Learning, Streaming e Channel). Ver IUserCourseAccessService para a regra completa.
        services.AddScoped<IUserCourseAccessService, LearningCourseAccessService>();
        services.AddScoped<IUserContactLookup, IdentidadeAcessoUserContactLookup>();
        services.AddScoped<IMagicLinkIssuer, IdentidadeAcessoMagicLinkIssuer>();
        // Achado B2 da avaliação de UX: matrícula anônima em curso grátis (localiza/cria a conta
        // pelo e-mail, sem senha) — ver EnrollFreeCourseAnonymousCommandHandler.
        services.AddScoped<IUserProvisioningService, IdentidadeAcessoUserProvisioningService>();
        // Ativação de acesso por código ("Tenho um código de acesso", dashboard do aluno sem
        // programas) e emissão pelo painel Admin — ver AccessCode (Domain).
        services.AddScoped<IAccessCodeRepository, AccessCodeRepository>();
        return services;
    }
}
