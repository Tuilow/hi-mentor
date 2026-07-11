using Tuilow.Payout.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tuilow.Payout.Infrastructure.Data.Configurations;

public sealed class PayoutTransactionConfiguration : IEntityTypeConfiguration<PayoutTransaction>
{
    public void Configure(EntityTypeBuilder<PayoutTransaction> builder)
    {
        builder.ToTable("payout_transactions", "payout");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.ExternalReference).HasMaxLength(200);
        builder.Property(t => t.ProcessedAt).IsRequired();

        builder.OwnsOne(t => t.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("amount").HasColumnType("numeric(12,2)").IsRequired();
            money.Property(m => m.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        });

        builder.HasIndex(t => t.PayoutRequestId);
    }
}
