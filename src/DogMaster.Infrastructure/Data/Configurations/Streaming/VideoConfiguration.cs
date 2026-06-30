using DogMaster.Domain.Contexts.Streaming.Entities;
using DogMaster.Domain.Contexts.Streaming.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogMaster.Infrastructure.Data.Configurations.Streaming;

public sealed class VideoConfiguration : IEntityTypeConfiguration<Video>
{
    public void Configure(EntityTypeBuilder<Video> builder)
    {
        builder.ToTable("videos", "streaming");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Title).HasMaxLength(500).IsRequired();
        builder.Property(v => v.CloudflareVideoId).HasMaxLength(200).IsRequired();
        builder.Property(v => v.Status).HasConversion<string>()
            .HasDefaultValue(VideoStatus.Processing);
        builder.HasIndex(v => v.CloudflareVideoId).IsUnique();
        builder.Property(v => v.CreatedAt).IsRequired();
        builder.Property(v => v.UpdatedAt).IsRequired();
    }
}
