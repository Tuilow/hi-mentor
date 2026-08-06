using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CoursePurchaseEntity = Tuilow.Sales.Domain.Entities.CoursePurchase;

namespace Tuilow.Sales.Infrastructure.Data.Configurations;

/// <summary>
/// Schema próprio ("course_sales") em vez de reaproveitar o schema "subscription" — mantém a
/// compra avulsa de curso (modelo principal atual) fisicamente separada das tabelas de
/// assinatura da plataforma (modelo legado), sem tocar nelas.
/// </summary>
public sealed class CoursePurchaseConfiguration : IEntityTypeConfiguration<CoursePurchaseEntity>
{
    public void Configure(EntityTypeBuilder<CoursePurchaseEntity> builder)
    {
        builder.ToTable("course_purchases", "course_sales");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(p => p.AsaasCustomerId).HasMaxLength(100).IsRequired();
        builder.Property(p => p.AsaasPaymentId).HasMaxLength(100).IsRequired();

        // Marketplace de split (creator como emissor da cobranca) -- ver CoursePurchase e
        // CreatorAsaasAccount para o racional completo.
        builder.Property(p => p.PaymentModel).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(p => p.CommissionPercentageSnapshot).HasColumnType("numeric(5,2)");

        builder.OwnsOne(p => p.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("amount").HasColumnType("numeric(10,2)").IsRequired();
            money.Property(m => m.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        });
        builder.OwnsOne(p => p.PlatformCommissionAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("platform_commission_amount").HasColumnType("numeric(10,2)");
            money.Property(m => m.Currency).HasColumnName("platform_commission_currency").HasMaxLength(3);
        });
        builder.OwnsOne(p => p.CreatorNetAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("creator_net_amount").HasColumnType("numeric(10,2)");
            money.Property(m => m.Currency).HasColumnName("creator_net_currency").HasMaxLength(3);
        });
        builder.OwnsOne(p => p.AsaasNetValueReceived, money =>
        {
            money.Property(m => m.Amount).HasColumnName("asaas_net_value_received").HasColumnType("numeric(10,2)");
            money.Property(m => m.Currency).HasColumnName("asaas_net_value_received_currency").HasMaxLength(3);
        });

        builder.HasIndex(p => p.AsaasPaymentId).IsUnique();
        builder.HasIndex(p => new { p.StudentId, p.CourseId });
        builder.HasIndex(p => p.CreatorId);
        builder.HasIndex(p => p.CreatorAsaasAccountId);

        builder.Ignore(p => p.DomainEvents);
    }
}
