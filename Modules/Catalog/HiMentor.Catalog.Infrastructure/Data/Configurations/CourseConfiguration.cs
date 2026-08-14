using System.Text.Json;
using HiMentor.Catalog.Domain.Entities;
using HiMentor.Catalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HiMentor.Catalog.Infrastructure.Data.Configurations;

public sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("courses", "catalog");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Title).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Description).IsRequired();
        builder.Property(c => c.ShortDescription).HasMaxLength(500);
        builder.Property(c => c.ThumbnailUrl).HasMaxLength(500);

        builder.Property(c => c.Slug)
            .HasColumnName("slug")
            .HasMaxLength(200)
            .HasConversion(s => s.Value, v => Slug.Create(v));
        builder.HasIndex(c => c.Slug).IsUnique().HasDatabaseName("ix_courses_slug");

        builder.OwnsOne(c => c.Price, price =>
        {
            price.Property(p => p.Amount).HasColumnName("price_amount").HasPrecision(10, 2);
            price.Property(p => p.Currency).HasColumnName("price_currency").HasMaxLength(3);
        });

        builder.Property(c => c.Level).HasConversion<string>().HasMaxLength(50);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(50);

        // ─── Jornada Guiada de Criação de Produtos (wizard) ─────────────────────
        builder.Property(c => c.Category).HasColumnName("category").HasMaxLength(100);
        builder.Property(c => c.Subcategory).HasColumnName("subcategory").HasMaxLength(100);
        builder.Property(c => c.ProductType).HasColumnName("product_type").HasConversion<string>().HasMaxLength(50);
        builder.Property(c => c.ViewCount).HasColumnName("view_count").HasDefaultValue(0);

        builder.Property(c => c.SalesPageHeadline).HasColumnName("sales_page_headline").HasMaxLength(300);
        builder.Property(c => c.SalesPageSubheadline).HasColumnName("sales_page_subheadline").HasMaxLength(500);
        builder.Property(c => c.SalesPageCtaText).HasColumnName("sales_page_cta_text").HasMaxLength(100);
        builder.Property(c => c.SalesPageVideoUrl).HasColumnName("sales_page_video_url").HasMaxLength(500);
        builder.Property(c => c.GuaranteeDays).HasColumnName("guarantee_days");
        builder.Property(c => c.GuaranteeText).HasColumnName("guarantee_text").HasMaxLength(500);

        // Lista simples de bullets — não precisa de entidade filha própria (sem ID/ordem
        // relevantes fora da lista). SalesPageBenefits é uma propriedade só-leitura (expõe o
        // campo privado _salesPageBenefits) — mapeamos o campo diretamente, serializado como
        // JSON numa única coluna text, e ignoramos a propriedade computada.
        builder.Ignore(c => c.SalesPageBenefits);
        builder.Property<List<string>>("_salesPageBenefits")
            .HasColumnName("sales_page_benefits")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                // Cursos que já existiam antes desta coluna ser criada ficam com NULL (ou string
                // vazia) no banco até serem salvos de novo — JsonSerializer.Deserialize lança
                // exceção para ambos os casos ("input does not contain any JSON tokens"), então
                // tratamos como lista vazia em vez de deixar estourar ao carregar esses cursos.
                v => string.IsNullOrEmpty(v)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()),
                v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
                v => v.ToList()));

        // Depoimentos — mesma técnica de SalesPageBenefits, só que com objetos em vez de strings.
        builder.Ignore(c => c.Testimonials);
        builder.Property<List<Testimonial>>("_testimonials")
            .HasColumnName("testimonials")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v)
                    ? new List<Testimonial>()
                    : JsonSerializer.Deserialize<List<Testimonial>>(v, (JsonSerializerOptions?)null) ?? new List<Testimonial>())
            .Metadata.SetValueComparer(new ValueComparer<List<Testimonial>>(
                (a, b) => (a ?? new List<Testimonial>()).SequenceEqual(b ?? new List<Testimonial>()),
                v => v.Aggregate(0, (hash, t) => HashCode.Combine(hash, t.GetHashCode())),
                v => v.ToList()));

        builder.HasMany(c => c.Modules)
            .WithOne()
            .HasForeignKey(m => m.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.FaqItems)
            .WithOne()
            .HasForeignKey(f => f.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(c => c.DomainEvents);
        builder.Ignore(c => c.IsFree);
        builder.Ignore(c => c.TotalDurationMinutes);
    }
}

public sealed class CourseFaqItemConfiguration : IEntityTypeConfiguration<CourseFaqItem>
{
    public void Configure(EntityTypeBuilder<CourseFaqItem> builder)
    {
        builder.ToTable("course_faq_items", "catalog");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Question).HasMaxLength(500).IsRequired();
        builder.Property(f => f.Answer).HasMaxLength(2000).IsRequired();
        builder.Property(f => f.Order).IsRequired();
        builder.Property(f => f.CreatedAt).IsRequired();
        builder.Property(f => f.UpdatedAt).IsRequired();
    }
}
