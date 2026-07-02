using Tuilow.Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tuilow.Finance.Infrastructure.Data.Configurations;

public sealed class CreatorWalletConfiguration : IEntityTypeConfiguration<CreatorWallet>
{
    public void Configure(EntityTypeBuilder<CreatorWallet> builder)
    {
        builder.ToTable("creator_wallets", "finance");
        builder.HasKey(w => w.Id);

        builder.HasIndex(w => w.CreatorId).IsUnique();

        builder.OwnsOne(w => w.AvailableBalance, money =>
        {
            money.Property(m => m.Amount).HasColumnName("available_balance_amount").HasColumnType("numeric(12,2)").IsRequired();
            money.Property(m => m.Currency).HasColumnName("available_balance_currency").HasMaxLength(3).IsRequired();
        });
        builder.OwnsOne(w => w.PendingBalance, money =>
        {
            money.Property(m => m.Amount).HasColumnName("pending_balance_amount").HasColumnType("numeric(12,2)").IsRequired();
            money.Property(m => m.Currency).HasColumnName("pending_balance_currency").HasMaxLength(3).IsRequired();
        });
        builder.OwnsOne(w => w.TotalGrossSales, money =>
        {
            money.Property(m => m.Amount).HasColumnName("total_gross_sales_amount").HasColumnType("numeric(12,2)").IsRequired();
            money.Property(m => m.Currency).HasColumnName("total_gross_sales_currency").HasMaxLength(3).IsRequired();
        });
        builder.OwnsOne(w => w.TotalPlatformFeePaid, money =>
        {
            money.Property(m => m.Amount).HasColumnName("total_platform_fee_paid_amount").HasColumnType("numeric(12,2)").IsRequired();
            money.Property(m => m.Currency).HasColumnName("total_platform_fee_paid_currency").HasMaxLength(3).IsRequired();
        });
        builder.OwnsOne(w => w.TotalNetEarned, money =>
        {
            money.Property(m => m.Amount).HasColumnName("total_net_earned_amount").HasColumnType("numeric(12,2)").IsRequired();
            money.Property(m => m.Currency).HasColumnName("total_net_earned_currency").HasMaxLength(3).IsRequired();
        });
        builder.OwnsOne(w => w.TotalWithdrawn, money =>
        {
            money.Property(m => m.Amount).HasColumnName("total_withdrawn_amount").HasColumnType("numeric(12,2)").IsRequired();
            money.Property(m => m.Currency).HasColumnName("total_withdrawn_currency").HasMaxLength(3).IsRequired();
        });

        builder.HasMany(w => w.Transactions)
            .WithOne()
            .HasForeignKey(t => t.CreatorWalletId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(w => w.DomainEvents);
    }
}
