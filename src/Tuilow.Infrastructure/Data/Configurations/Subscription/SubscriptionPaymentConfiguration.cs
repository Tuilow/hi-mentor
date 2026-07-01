using Tuilow.Domain.Contexts.Subscription.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tuilow.Infrastructure.Data.Configurations.Subscription;

public sealed class SubscriptionPaymentConfiguration : IEntityTypeConfiguration<SubscriptionPayment>
{
    public void Configure(EntityTypeBuilder<SubscriptionPayment> builder)
    {
        builder.ToTable("subscription_payments", "subscription");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.AsaasPaymentId).HasMaxLength(100).IsRequired();
        builder.HasIndex(p => p.AsaasPaymentId).IsUnique();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(p => p.Method).HasConversion<string>().HasMaxLength(50);
        builder.OwnsOne(p => p.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("amount")
                .HasColumnType("numeric(10,2)").IsRequired();
            money.Property(m => m.Currency).HasColumnName("currency")
                .HasMaxLength(3).IsRequired();
        });
        builder.Property(p => p.DueDate).IsRequired();
        builder.Property(p => p.PaidAt);
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();
    }
}
