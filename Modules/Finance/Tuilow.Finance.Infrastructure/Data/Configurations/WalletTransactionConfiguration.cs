using Tuilow.Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tuilow.Finance.Infrastructure.Data.Configurations;

public sealed class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.ToTable("wallet_transactions", "finance");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(t => t.AppliedFeePercentage).HasColumnType("numeric(5,2)");
        builder.Property(t => t.ReferenceType).HasMaxLength(100);
        builder.Property(t => t.ReferenceId);
        builder.Property(t => t.CycleStart).IsRequired();
        builder.Property(t => t.CycleEnd).IsRequired();

        builder.OwnsOne(t => t.GrossAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("gross_amount").HasColumnType("numeric(12,2)");
            money.Property(m => m.Currency).HasColumnName("gross_currency").HasMaxLength(3);
        });
        builder.OwnsOne(t => t.FeeAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("fee_amount").HasColumnType("numeric(12,2)");
            money.Property(m => m.Currency).HasColumnName("fee_currency").HasMaxLength(3);
        });
        builder.OwnsOne(t => t.NetAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("net_amount").HasColumnType("numeric(12,2)").IsRequired();
            money.Property(m => m.Currency).HasColumnName("net_currency").HasMaxLength(3).IsRequired();
        });

        builder.HasIndex(t => t.CreatorWalletId);
        builder.HasIndex(t => new { t.ReferenceType, t.ReferenceId });
        builder.HasIndex(t => new { t.Type, t.Status });
    }
}
