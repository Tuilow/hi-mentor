using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.SharedKernel.Domain.Common;
using Tuilow.IdentidadeAcesso.Domain.Entities;
using CatalogEntities = Tuilow.Catalog.Domain.Entities;
using LearningEntities = Tuilow.Learning.Domain.Entities;
using JourneyEntities = Tuilow.Journey.Domain.Entities;
using SalesEntities = Tuilow.Sales.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Tuilow.Host.Api.Data;

/// <summary>
/// DbContext único de composição do Host. Cada módulo contribui suas entidades via
/// ApplyConfigurationsFromAssembly (não via referência direta de tipo) — assim o Host
/// não precisa conhecer os detalhes internos de cada módulo, só a lista de assemblies.
///
/// IMPORTANTE (transição): IdentidadeAcesso, Catalog, Learning, Journey e Sales já foram
/// migrados para a nova estrutura. Só Streaming (vídeo/Cloudflare) ainda vive no Tuilow.API
/// antigo (src/Tuilow.API) enquanto não é migrado (ver RELATORIO_REBRANDING / próxima fase).
/// Os dois hosts (antigo e este) apontam para bancos diferentes até a migração terminar —
/// não rode os dois contra o mesmo banco de dev ao mesmo tempo, ou rode o antigo até tudo migrar.
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
