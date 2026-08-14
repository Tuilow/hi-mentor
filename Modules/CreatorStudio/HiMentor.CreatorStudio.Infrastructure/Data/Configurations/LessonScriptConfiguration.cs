using System.Text.Json;
using HiMentor.CreatorStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HiMentor.CreatorStudio.Infrastructure.Data.Configurations;

public sealed class LessonScriptConfiguration : IEntityTypeConfiguration<LessonScript>
{
    public void Configure(EntityTypeBuilder<LessonScript> builder)
    {
        builder.ToTable("lesson_scripts", "creator_studio");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.CreatorId).IsRequired();
        builder.HasIndex(s => s.CreatorId).HasDatabaseName("ix_lesson_scripts_creator_id");

        builder.Property(s => s.CourseId);
        builder.Property(s => s.LessonId);
        builder.Property(s => s.LessonTitle).HasMaxLength(300).IsRequired();
        builder.Property(s => s.Introduction).HasColumnType("text");
        builder.Property(s => s.ClosingCta).HasMaxLength(1000);
        builder.Property(s => s.WasRecorded).IsRequired();
        builder.Property(s => s.RecordedAt);

        // Listas simples (tópicos/sugestões) — mesma técnica de Catalog.Course.SalesPageBenefits:
        // serializadas como JSON numa única coluna, com ValueComparer explícito.
        builder.Ignore(s => s.DevelopmentTopics);
        builder.Property<List<string>>("_developmentTopics")
            .HasColumnName("development_topics")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()),
                v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
                v => v.ToList()));

        builder.Ignore(s => s.DemonstrationSuggestions);
        builder.Property<List<string>>("_demonstrationSuggestions")
            .HasColumnName("demonstration_suggestions")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()),
                v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
                v => v.ToList()));

        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();
    }
}
