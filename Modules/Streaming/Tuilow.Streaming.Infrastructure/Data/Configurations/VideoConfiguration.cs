using Tuilow.Streaming.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tuilow.Streaming.Infrastructure.Data.Configurations;

public sealed class VideoConfiguration : IEntityTypeConfiguration<Video>
{
    public void Configure(EntityTypeBuilder<Video> builder)
    {
        builder.ToTable("videos", "streaming");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.CloudflareVideoId).HasMaxLength(200);
        builder.Property(v => v.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasIndex(v => v.CloudflareVideoId).IsUnique();

        // Importação externa (passo 2 do assistente).
        builder.Property(v => v.Source).HasColumnName("source").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(v => v.ExternalUrl).HasColumnName("external_url").HasMaxLength(1000);
        builder.Property(v => v.ExternalId).HasColumnName("external_id").HasMaxLength(200);
        builder.Property(v => v.Title).HasColumnName("title").HasMaxLength(300);

        // Produto ao qual o vídeo pertence — sem FK de verdade pro Course (Catalog é outro
        // módulo), só um Guid solto + índice, mesmo padrão de Plan.CourseId (Sales).
        builder.Property(v => v.CourseId).HasColumnName("course_id");
        builder.HasIndex(v => v.CourseId).HasDatabaseName("ix_videos_course_id");

        builder.Property(v => v.CreatedAt).IsRequired();
        builder.Property(v => v.UpdatedAt).IsRequired();
        builder.Ignore(v => v.DomainEvents);
    }
}
