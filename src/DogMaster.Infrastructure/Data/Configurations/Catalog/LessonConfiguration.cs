using DogMaster.Domain.Contexts.Catalog.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogMaster.Infrastructure.Data.Configurations.Catalog;

public sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("lessons", "catalog");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Title).HasMaxLength(300).IsRequired();
        builder.Property(l => l.Description).HasMaxLength(2000);
        builder.Property(l => l.CloudflareVideoId).HasMaxLength(200);
        builder.Property(l => l.DurationSeconds).HasDefaultValue(0);
        builder.Property(l => l.Order).IsRequired();
        builder.Property(l => l.IsFree).HasDefaultValue(false);
        builder.Property(l => l.CreatedAt).IsRequired();
        builder.Property(l => l.UpdatedAt).IsRequired();

        builder.HasMany(l => l.Attachments)
            .WithOne()
            .HasForeignKey(a => a.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(l => l.Exercises)
            .WithOne()
            .HasForeignKey(e => e.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class LessonAttachmentConfiguration : IEntityTypeConfiguration<LessonAttachment>
{
    public void Configure(EntityTypeBuilder<LessonAttachment> builder)
    {
        builder.ToTable("lesson_attachments", "catalog");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.FileName).HasMaxLength(300).IsRequired();
        builder.Property(a => a.Url).HasMaxLength(1000).IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired();
    }
}

public sealed class LessonExerciseConfiguration : IEntityTypeConfiguration<LessonExercise>
{
    public void Configure(EntityTypeBuilder<LessonExercise> builder)
    {
        builder.ToTable("lesson_exercises", "catalog");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.Order).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();
    }
}
