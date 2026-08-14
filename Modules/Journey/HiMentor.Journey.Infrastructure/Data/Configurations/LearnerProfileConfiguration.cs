using HiMentor.Journey.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HiMentor.Journey.Infrastructure.Data.Configurations;

public sealed class LearnerProfileConfiguration : IEntityTypeConfiguration<LearnerProfile>
{
    public void Configure(EntityTypeBuilder<LearnerProfile> builder)
    {
        builder.ToTable("learner_profiles", "learner_profile");
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => p.UserId).HasDatabaseName("ix_learner_profiles_user_id");

        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Category).HasMaxLength(100);
        builder.Property(p => p.PhotoUrl).HasMaxLength(500);
        builder.Property(p => p.Level).HasConversion<string>().HasMaxLength(50);

        builder.HasMany(p => p.Goals)
            .WithOne()
            .HasForeignKey(g => g.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(p => p.DomainEvents);
        builder.Ignore(p => p.AgeMonths);
    }
}
