using Tuilow.Learning.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tuilow.Learning.Infrastructure.Data.Configurations;

public sealed class AccessCodeConfiguration : IEntityTypeConfiguration<AccessCode>
{
    public void Configure(EntityTypeBuilder<AccessCode> builder)
    {
        builder.ToTable("access_codes", "learning");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Code).HasMaxLength(16).IsRequired();
        builder.HasIndex(a => a.Code).IsUnique().HasDatabaseName("ix_access_codes_code");

        builder.HasMany(a => a.Redemptions)
            .WithOne()
            .HasForeignKey(r => r.AccessCodeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Campo backing "_redemptions" (singular) vs propriedade "Redemptions" (plural) — mesmo
        // ajuste de EnrollmentConfiguration.LessonsProgress: sem isto, Include() lança
        // NotSupportedException ao tentar Add() no wrapper .AsReadOnly() em vez de usar o campo
        // real via reflection.
        builder.Navigation(a => a.Redemptions)
            .HasField("_redemptions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(a => a.DomainEvents);
    }
}
