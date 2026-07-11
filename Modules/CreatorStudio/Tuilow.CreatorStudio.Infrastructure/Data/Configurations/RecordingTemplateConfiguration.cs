using System.Text.Json;
using Tuilow.CreatorStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tuilow.CreatorStudio.Infrastructure.Data.Configurations;

public sealed class RecordingTemplateConfiguration : IEntityTypeConfiguration<RecordingTemplate>
{
    public void Configure(EntityTypeBuilder<RecordingTemplate> builder)
    {
        builder.ToTable("recording_templates", "creator_studio");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.CreatorId).IsRequired();
        builder.HasIndex(t => t.CreatorId).HasDatabaseName("ix_recording_templates_creator_id");

        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.IsDefault).IsRequired();

        // Mesma técnica de Catalog.Course.SalesPageBenefits — lista serializada como JSON.
        builder.Ignore(t => t.Sections);
        builder.Property<List<string>>("_sections")
            .HasColumnName("sections")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()),
                v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
                v => v.ToList()));

        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();
    }
}
