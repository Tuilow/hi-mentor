using DogMaster.Domain.Contexts.Catalog.Entities;
using DogMaster.Domain.Contexts.Catalog.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogMaster.Infrastructure.Data.Configurations.Catalog;

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

        builder.HasMany(c => c.Modules)
            .WithOne()
            .HasForeignKey(m => m.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(c => c.DomainEvents);
        builder.Ignore(c => c.IsFree);
        builder.Ignore(c => c.TotalDurationMinutes);
    }
}
