using Tuilow.Sales.Domain.Entities;
using Tuilow.Catalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tuilow.Sales.Infrastructure.Data.Configurations;

public sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("plans", "subscription");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Slug)
            .HasMaxLength(100).IsRequired()
            .HasConversion(s => s.Value, v => Slug.Create(v));
        builder.HasIndex(p => p.Slug)
            .IsUnique()
            .HasFilter(null);
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.AsaasPlanId).HasMaxLength(100);

        builder.OwnsOne(p => p.Price, price =>
        {
            price.Property(m => m.Amount).HasColumnName("price_amount")
                .HasColumnType("numeric(10,2)").IsRequired();
            price.Property(m => m.Currency).HasColumnName("price_currency")
                .HasMaxLength(3).IsRequired();
        });

        builder.Property(p => p.BillingCycle).HasConversion<string>().IsRequired();
        builder.Property(p => p.IsActive).HasDefaultValue(true);
        builder.Property(p => p.TrialDays).HasDefaultValue(0);

        // Plano de assinatura por produto (passo 5 do assistente). Null = plano da plataforma
        // (modelo legado). Sem FK de verdade pro Course (Catalog é outro módulo) — só índice
        // pra consulta rápida de "plano de assinatura deste curso".
        builder.Property(p => p.CourseId).HasColumnName("course_id");
        builder.HasIndex(p => p.CourseId).HasDatabaseName("ix_plans_course_id");

        builder.HasMany(p => p.Features)
            .WithOne()
            .HasForeignKey(f => f.PlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PlanFeatureConfiguration : IEntityTypeConfiguration<PlanFeature>
{
    public void Configure(EntityTypeBuilder<PlanFeature> builder)
    {
        builder.ToTable("plan_features", "subscription");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.FeatureKey).HasMaxLength(100).IsRequired();
        builder.Property(f => f.FeatureValue).HasMaxLength(200);
        builder.Property(f => f.DisplayName).HasMaxLength(200);
    }
}
