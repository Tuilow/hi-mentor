using Tuilow.Domain.Contexts.Learning.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tuilow.Infrastructure.Data.Configurations.Learning;

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

        builder.HasMany(e => e.LessonsProgress)
            .WithOne()
            .HasForeignKey(p => p.EnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Campo "_lessonProgress" (singular) vs propriedade "LessonsProgress" (plural) — não
        // batem com a convenção de backing field do EF. Mesmo problema/fix de UserConfiguration
        // (User.UserRoleAssignments): sem isto, Include() lança NotSupportedException ao tentar
        // Add() no wrapper .AsReadOnly() em vez de usar o campo real via reflection.
        builder.Navigation(e => e.LessonsProgress)
            .HasField("_lessonProgress")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(e => e.DomainEvents);
    }
}
