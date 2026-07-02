using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionEntity = Tuilow.Sales.Domain.Entities.Subscription;

namespace Tuilow.Sales.Infrastructure.Data.Configurations;

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<SubscriptionEntity>
{
    public void Configure(EntityTypeBuilder<SubscriptionEntity> builder)
    {
        builder.ToTable("subscriptions", "subscription");
        builder.HasKey(s => s.Id);

        builder.HasIndex(s => s.AsaasSubscriptionId)
            .IsUnique().HasDatabaseName("ix_subscriptions_asaas_id");

        builder.HasIndex(s => new { s.UserId, s.Status })
            .HasDatabaseName("ix_subscriptions_user_status");

        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(s => s.BillingCycle).HasConversion<string>().HasMaxLength(50);

        builder.HasMany(s => s.Payments)
            .WithOne()
            .HasForeignKey(p => p.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(s => s.DomainEvents);
        builder.Ignore(s => s.IsActive);
    }
}
