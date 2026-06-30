using DogMaster.Domain.Contexts.Subscription.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogMaster.Infrastructure.Data.Configurations.Subscription;

public sealed class SubscriptionPaymentConfiguration : IEntityTypeConfiguration<SubscriptionPayment>
{
    public void Configure(EntityTypeBuilder<SubscriptionPayment> builder)
    {
        builder.ToTable("subscription_payments", "subscription");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.AsaasPaymentId).HasMaxLength(100).IsRequired();
        builder.HasIndex(p => p.AsaasPaymentId).IsUnique();
        builder.Property(p => p.Status).HasConversion<string>().IsRequired();
        builder.Property(p => p.Method).HasConversion<string>();
        builder.Property(p => p.Amount).HasColumnType("numeric(10,2)").IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();
    }
}
