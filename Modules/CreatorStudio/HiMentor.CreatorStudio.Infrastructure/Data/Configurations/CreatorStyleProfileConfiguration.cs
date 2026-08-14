using HiMentor.CreatorStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HiMentor.CreatorStudio.Infrastructure.Data.Configurations;

public sealed class CreatorStyleProfileConfiguration : IEntityTypeConfiguration<CreatorStyleProfile>
{
    public void Configure(EntityTypeBuilder<CreatorStyleProfile> builder)
    {
        builder.ToTable("creator_style_profiles", "creator_studio");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.CreatorId).IsRequired();
        builder.HasIndex(p => p.CreatorId).IsUnique().HasDatabaseName("ix_creator_style_profiles_creator_id");

        builder.Property(p => p.Niche).HasMaxLength(200).IsRequired();
        builder.Property(p => p.TargetAudience).HasMaxLength(500).IsRequired();
        builder.Property(p => p.Objective).HasMaxLength(500).IsRequired();
        builder.Property(p => p.Level).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();
    }
}
