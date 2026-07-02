using Tuilow.Domain.Common.Abstractions;
using Tuilow.Domain.Common.Interfaces;
using Tuilow.Domain.Contexts.Catalog.Entities;
using Tuilow.Domain.Contexts.Profiles.Entities;
using Tuilow.Domain.Contexts.Identity.Entities;
using Tuilow.Domain.Contexts.Learning.Entities;
using Tuilow.Domain.Contexts.Streaming.Entities;
using Tuilow.Domain.Contexts.Subscription.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Tuilow.Infrastructure.Data;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IMediator mediator
) : DbContext(options), IUnitOfWork
{
    // Identity
    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<SocialLogin> SocialLogins => Set<SocialLogin>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();

    // Catalog
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonAttachment> LessonAttachments => Set<LessonAttachment>();
    public DbSet<LessonExercise> LessonExercises => Set<LessonExercise>();

    // Learning
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<LessonProgress> LessonProgress => Set<LessonProgress>();
    public DbSet<Certificate> Certificates => Set<Certificate>();

    // Streaming
    public DbSet<Video> Videos => Set<Video>();

    // Subscription
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<PlanFeature> PlanFeatures => Set<PlanFeature>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<SubscriptionPayment> SubscriptionPayments => Set<SubscriptionPayment>();

    // Learner Profiles
    public DbSet<LearnerProfile> LearnerProfiles => Set<LearnerProfile>();
    public DbSet<LearningGoal> LearningGoals => Set<LearningGoal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Schemas
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
