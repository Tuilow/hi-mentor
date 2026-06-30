using DogMaster.Domain.Contexts.Learning.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogMaster.Infrastructure.Data.Configurations.Learning;

public sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("enrollments", "learning");
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.UserId, e.CourseId })
            .IsUnique().HasDatabaseName("ix_enrollments_user_course");

        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(e => e.ProgressPercentage).HasPrecision(5, 2);

        builder.HasMany(e => e.LessonProgress)
            .WithOne()
            .HasForeignKey(p => p.EnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(e => e.DomainEvents);
    }
}
