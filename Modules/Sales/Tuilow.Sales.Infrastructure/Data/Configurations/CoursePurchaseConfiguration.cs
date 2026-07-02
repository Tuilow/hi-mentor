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

        builder.OwnsOne(p => p.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("amount").HasColumnType("numeric(10,2)").IsRequired();
            money.Property(m => m.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        });

        builder.HasIndex(p => p.AsaasPaymentId).IsUnique();
        builder.HasIndex(p => new { p.StudentId, p.CourseId });
        builder.HasIndex(p => p.CreatorId);

        builder.Ignore(p => p.DomainEvents);
    }
}
