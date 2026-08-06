using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.SharedKernel.Domain.Common;
using Tuilow.IdentidadeAcesso.Domain.Entities;
using CatalogEntities = Tuilow.Catalog.Domain.Entities;
using LearningEntities = Tuilow.Learning.Domain.Entities;
using JourneyEntities = Tuilow.Journey.Domain.Entities;
using SalesEntities = Tuilow.Sales.Domain.Entities;
using StreamingEntities = Tuilow.Streaming.Domain.Entities;
using FinanceEntities = Tuilow.Finance.Domain.Entities;
using PayoutEntities = Tuilow.Payout.Domain.Entities;
using CreatorStudioEntities = Tuilow.CreatorStudio.Domain.Entities;
using ChannelEntities = Tuilow.Channel.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Tuilow.Host.Api.Data;

/// <summary>
/// DbContext único de composição do Host. Cada módulo contribui suas entidades via
/// ApplyConfigurationsFromAssembly (não via referência direta de tipo) — assim o Host
/// não precisa conhecer os detalhes internos de cada módulo, só a lista de assemblies.
///
/// IMPORTANTE (transição): todos os módulos de plataforma com código real já foram migrados
/// (IdentidadeAcesso, Catalog, Learning, Journey, Sales, Streaming, CreatorStudio, Channel).
/// Resta só Growth, que é um contexto novo sem código legado (ainda stub). O Tuilow.API antigo
/// (src/Tuilow.API) pode ser desligado depois que os dois lados forem validados lado a lado —
/// ver Task #18.
/// Os dois hosts apontam para bancos diferentes (tuilow_dev vs tuilow_modular_dev); não rode os
/// dois contra o mesmo banco de dev ao mesmo tempo.
/// </summary>
public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IMediator mediator,
    IServiceProvider serviceProvider,
    ILogger<AppDbContext> logger
) : DbContext(options), IUnitOfWork, IDataProtectionKeyContext
{
    // Chaves mestras do Data Protection API (ISecretProtector) -- persistidas no Postgres para
    // sobreviver a redeploys em containers efemeros (Railway). Ver Program.cs -- AddDataProtection().PersistKeysToDbContext.
    public DbSet<Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey> DataProtectionKeys => Set<Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey>();

    // IdentidadeAcesso
    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<SocialLogin> SocialLogins => Set<SocialLogin>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();
    public DbSet<MagicLinkToken> MagicLinkTokens => Set<MagicLinkToken>();

    // Catalog
    public DbSet<CatalogEntities.Course> Courses => Set<CatalogEntities.Course>();
    public DbSet<CatalogEntities.Module> Modules => Set<CatalogEntities.Module>();
    public DbSet<CatalogEntities.Lesson> Lessons => Set<CatalogEntities.Lesson>();
    public DbSet<CatalogEntities.LessonAttachment> LessonAttachments => Set<CatalogEntities.LessonAttachment>();
    public DbSet<CatalogEntities.LessonExercise> LessonExercises => Set<CatalogEntities.LessonExercise>();
    public DbSet<CatalogEntities.CourseFaqItem> CourseFaqItems => Set<CatalogEntities.CourseFaqItem>();

    // Learning
    public DbSet<LearningEntities.Enrollment> Enrollments => Set<LearningEntities.Enrollment>();
    public DbSet<LearningEntities.LessonProgress> LessonProgress => Set<LearningEntities.LessonProgress>();
    public DbSet<LearningEntities.Certificate> Certificates => Set<LearningEntities.Certificate>();

    // Learning — log mínimo de notificações (achado M12 da auditoria): correlaciona pagamento,
    // matrícula e tentativa de e-mail/WhatsApp pelo mesmo AsaasPaymentId/CorrelationId.
    public DbSet<LearningEntities.NotificationLog> NotificationLogs => Set<LearningEntities.NotificationLog>();

    // Journey
    public DbSet<JourneyEntities.LearnerProfile> LearnerProfiles => Set<JourneyEntities.LearnerProfile>();
    public DbSet<JourneyEntities.LearningGoal> LearningGoals => Set<JourneyEntities.LearningGoal>();

    // Sales
    public DbSet<SalesEntities.Plan> Plans => Set<SalesEntities.Plan>();
    public DbSet<SalesEntities.PlanFeature> PlanFeatures => Set<SalesEntities.PlanFeature>();
    public DbSet<SalesEntities.Subscription> Subscriptions => Set<SalesEntities.Subscription>();
    public DbSet<SalesEntities.SubscriptionPayment> SubscriptionPayments => Set<SalesEntities.SubscriptionPayment>();

    // Streaming
    public DbSet<StreamingEntities.Video> Videos => Set<StreamingEntities.Video>();

    // Sales — compra avulsa de curso (modelo principal de monetização)
    public DbSet<SalesEntities.CoursePurchase> CoursePurchases => Set<SalesEntities.CoursePurchase>();

    // Finance — comissão da plataforma e carteira do criador
    public DbSet<FinanceEntities.PlatformFeeConfiguration> PlatformFeeConfigurations => Set<FinanceEntities.PlatformFeeConfiguration>();
    public DbSet<FinanceEntities.CreatorWallet> CreatorWallets => Set<FinanceEntities.CreatorWallet>();
    public DbSet<FinanceEntities.WalletTransaction> WalletTransactions => Set<FinanceEntities.WalletTransaction>();

    // Finance -- marketplace de split (creator como emissor da cobranca, ver CreatorAsaasAccount)
    public DbSet<FinanceEntities.CreatorAsaasAccount> CreatorAsaasAccounts => Set<FinanceEntities.CreatorAsaasAccount>();
    public DbSet<FinanceEntities.CreatorAsaasCustomer> CreatorAsaasCustomers => Set<FinanceEntities.CreatorAsaasCustomer>();

    // Payout — saques do criador (ciclo de 15 dias)
    public DbSet<PayoutEntities.PayoutRequest> PayoutRequests => Set<PayoutEntities.PayoutRequest>();
    public DbSet<PayoutEntities.PayoutTransaction> PayoutTransactions => Set<PayoutEntities.PayoutTransaction>();

    // CreatorStudio — jornada guiada de criação de produtos (leads capturados na página de vendas)
    public DbSet<CreatorStudioEntities.Lead> Leads => Set<CreatorStudioEntities.Lead>();

    // CreatorStudio — Estúdio do Criador (nicho, roteiros gerados por IA, templates de gravação)
    public DbSet<CreatorStudioEntities.CreatorStyleProfile> CreatorStyleProfiles => Set<CreatorStudioEntities.CreatorStyleProfile>();
    public DbSet<CreatorStudioEntities.LessonScript> LessonScripts => Set<CreatorStudioEntities.LessonScript>();
    public DbSet<CreatorStudioEntities.RecordingTemplate> RecordingTemplates => Set<CreatorStudioEntities.RecordingTemplate>();

    // Channel — Canal do Criador (vitrine pública com @handle e redes sociais)
    public DbSet<ChannelEntities.CreatorChannel> CreatorChannels => Set<ChannelEntities.CreatorChannel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Cada módulo novo entra aqui com seu próprio assembly de configurations.
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(IdentidadeAcesso.Infrastructure.Data.Configurations.UserConfiguration).Assembly);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(Catalog.Infrastructure.Data.Configurations.CourseConfiguration).Assembly);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(Learning.Infrastructure.Data.Configurations.EnrollmentConfiguration).Assembly);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(Journey.Infrastructure.Data.Configurations.LearnerProfileConfiguration).Assembly);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(Sales.Infrastructure.Data.Configurations.PlanConfiguration).Assembly);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(Streaming.Infrastructure.Data.Configurations.VideoConfiguration).Assembly);

        // CoursePurchaseConfiguration vive no mesmo assembly de Sales.Infrastructure (PlanConfiguration
        // acima) — não precisa de outra chamada a ApplyConfigurationsFromAssembly.

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(Finance.Infrastructure.Data.Configurations.CreatorWalletConfiguration).Assembly);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(Payout.Infrastructure.Data.Configurations.PayoutRequestConfiguration).Assembly);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(CreatorStudio.Infrastructure.Data.Configurations.LeadConfiguration).Assembly);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(Channel.Infrastructure.Data.Configurations.CreatorChannelConfiguration).Assembly);

        modelBuilder.HasDefaultSchema("public");
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Salva primeiro — domain events são disparados depois para que handlers
        // possam consultar as entidades já persistidas no banco.
        var result = await base.SaveChangesAsync(ct);
        await DispatchDomainEventsAsync(ct);
        return result;
    }

    private async Task DispatchDomainEventsAsync(CancellationToken ct)
    {
        // Coleta e limpa ANTES de publicar para evitar re-dispatch em SaveChanges recursivo
        var events = ChangeTracker.Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .SelectMany(a =>
            {
                var domainEvents = a.DomainEvents.ToList();
                a.ClearDomainEvents();
                return domainEvents;
            })
            .ToList();

        foreach (var domainEvent in events)
            await PublishDomainEventAsync(domainEvent, ct);
    }

    private async Task PublishDomainEventAsync(IDomainEvent domainEvent, CancellationToken ct)
    {
        var handlerType = typeof(INotificationHandler<>).MakeGenericType(domainEvent.GetType());
        var handlers = serviceProvider.GetServices(handlerType).ToList();

        if (handlers.Count == 0)
        {
            await mediator.Publish(domainEvent, ct);
            return;
        }

        var handleMethod = handlerType.GetMethod("Handle")
            ?? throw new InvalidOperationException($"Handler {handlerType.Name} sem metodo Handle.");

        foreach (var handler in handlers)
        {
            try
            {
                if (handler is null) continue;

                var task = handleMethod.Invoke(handler, [domainEvent, ct]) as Task
                    ?? throw new InvalidOperationException(
                        $"Handler {handler.GetType().Name} retornou resultado invalido.");

                await task;
            }
            catch (Exception ex)
            {
                // Achado C2: o estado que originou o evento já foi commitado. Cada handler roda
                // isoladamente para que uma falha em Learning não impeça Finance, ou vice-versa.
                logger.LogCritical(ex,
                    "Falha ao processar domain event {EventType} no handler {HandlerType} " +
                    "(EventId {EventId}, ocorrido em {OccurredOn}) — efeito colateral pode não ter sido aplicado " +
                    "(matrícula/e-mail/comissão). Payload: {DomainEvent}",
                    domainEvent.GetType().Name, handler?.GetType().Name ?? handlerType.Name,
                    domainEvent.EventId, domainEvent.OccurredOn, domainEvent);
            }
        }
    }
}
