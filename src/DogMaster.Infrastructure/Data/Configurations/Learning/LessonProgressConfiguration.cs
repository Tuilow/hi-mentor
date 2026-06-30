using DogMaster.Domain.Contexts.Learning.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogMaster.Infrastructure.Data.Configurations.Learning;

public sealed class LessonProgressConfiguration : IEntityTypeConfiguration<LessonProgress>
{
    public void Configure(EntityTypeBuilder<LessonProgress> builder)
    {
        builder.ToTable("lesson_progress", "learning");
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => new { p.EnrollmentId, p.LessonId }).IsUnique();
        builder.Property(p => p.WatchedSeconds).HasDefaultValue(0);
        builder.Property(p => p.TotalSeconds).HasDefaultValue(0);
        builder.Property(p => p.IsCompleted).HasDefaultValue(false);
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();
    }
}
