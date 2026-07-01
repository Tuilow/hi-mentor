using DogMaster.Domain.Contexts.DogProfile.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogMaster.Infrastructure.Data.Configurations.DogProfile;

public sealed class DogConfiguration : IEntityTypeConfiguration<Dog>
{
    public void Configure(EntityTypeBuilder<Dog> builder)
    {
        builder.ToTable("dogs", "dog_profile");
        builder.HasKey(d => d.Id);

        builder.HasIndex(d => d.UserId).HasDatabaseName("ix_dogs_user_id");

        builder.Property(d => d.Name).HasMaxLength(100).IsRequired();
        builder.Property(d => d.Breed).HasMaxLength(100);
        builder.Property(d => d.Sex).HasMaxLength(10);
        builder.Property(d => d.PhotoUrl).HasMaxLength(500);
        builder.Property(d => d.Level).HasConversion<string>().HasMaxLength(50);
        builder.Property(d => d.WeightKg).HasPrecision(5, 2);

        builder.HasMany(d => d.Objectives)
            .WithOne()
            .HasForeignKey(o => o.DogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(d => d.DomainEvents);
        builder.Ignore(d => d.AgeMonths);
    }
}
