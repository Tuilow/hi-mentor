using HiMentor.Payout.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HiMentor.Payout.Infrastructure.Data.Configurations;

public sealed class PayoutRequestConfiguration : IEntityTypeConfiguration<PayoutRequest>
{
    public void Configure(EntityTypeBuilder<PayoutRequest> builder)
    {
        builder.ToTable("payout_requests", "payout");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(p => p.CycleStart).IsRequired();
        builder.Property(p => p.CycleEnd).IsRequired();
        builder.Property(p => p.RequestedAt).IsRequired();
        builder.Property(p => p.RejectionReason).HasMaxLength(500);

        builder.OwnsOne(p => p.RequestedAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("requested_amount").HasColumnType("numeric(12,2)").IsRequired();
            money.Property(m => m.Currency).HasColumnName("requested_currency").HasMaxLength(3).IsRequired();
        });

        builder.HasMany(p => p.Transactions)
            .WithOne()
            .HasForeignKey(t => t.PayoutRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.CreatorId, p.Status });
        builder.HasIndex(p => p.Status);

        builder.Ignore(p => p.DomainEvents);
    }
}
