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
        builder.Property(v => v.CreatedAt).IsRequired();
        builder.Property(v => v.UpdatedAt).IsRequired();
        builder.Ignore(v => v.DomainEvents);
    }
}
