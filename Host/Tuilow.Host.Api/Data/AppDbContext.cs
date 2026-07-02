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
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Tuilow.Host.Api.Data;

/// <summary>
/// DbContext único de composição do Host. Cada módulo contribui suas entidades via
/// ApplyConfigurationsFromAssembly (não via referência direta de tipo) — assim o Host
/// não precisa conhecer os detalhes internos de cada módulo, só a lista de assemblies.
///
/// IMPORTANTE (transição): todos os módulos de plataforma com código real já foram migrados
/// (IdentidadeAcesso, Catalog, Learning, Journey, Sales, Streaming). Restam só Channel e Growth,
/// que são contextos novos sem código legado (ainda stubs). O Tuilow.API antigo (src/Tuilow.API)
/// pode ser desligado depois que os dois lados forem validados lado a lado — ver Task #18.
/// Os dois hosts apontam para bancos diferentes (tuilow_dev vs tuilow_modular_dev); não rode os
/// dois contra o mesmo banco de dev ao mesmo tempo.
/// </summary>
public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IMediator mediator
) : DbContext(options), IUnitOfWork
{
    // IdentidadeAcesso
    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<SocialLogin> SocialLogins => Set<SocialLogin>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();

    // Catalog
    public DbSet<CatalogEntities.Course> Courses => Set<CatalogEntities.Course>();
    public DbSet<CatalogEntities.Module> Modules => Set<CatalogEntities.Module>();
    public DbSet<CatalogEntities.Lesson> Lessons => Set<CatalogEntities.Lesson>();
    public DbSet<CatalogEntities.LessonAttachment> LessonAttachments => Set<CatalogEntities.LessonAttachment>();
    public DbSet<CatalogEntities.LessonExercise> LessonExercises => Set<CatalogEntities.LessonExercise>();

    // Learning
    public DbSet<LearningEntities.Enrollment> Enrollments => Set<LearningEntities.Enrollment>();
    public DbSet<LearningEntities.LessonProgress> LessonProgress => Set<LearningEntities.LessonProgress>();
    public DbSet<LearningEntities.Certificate> Certificates => Set<LearningEntities.Certificate>();

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

    // Payout — saques do criador (ciclo de 15 dias)
    public DbSet<PayoutEntities.PayoutRequest> PayoutRequests => Set<PayoutEntities.PayoutRequest>();
    public DbSet<PayoutEntities.PayoutTransaction> PayoutTransactions => Set<PayoutEntities.PayoutTransaction>();

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
            await mediator.Publish(domainEvent, ct);
    }
}
