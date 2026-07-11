using System.Text.Json;
using Tuilow.Channel.Domain.Entities;
using Tuilow.Channel.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tuilow.Channel.Infrastructure.Data.Configurations;

public sealed class CreatorChannelConfiguration : IEntityTypeConfiguration<CreatorChannel>
{
    public void Configure(EntityTypeBuilder<CreatorChannel> builder)
    {
        builder.ToTable("creator_channels", "channel");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CreatorId).IsRequired();
        builder.HasIndex(c => c.CreatorId).IsUnique().HasDatabaseName("ix_creator_channels_creator_id");

        builder.Property(c => c.Handle)
            .HasColumnName("handle")
            .HasMaxLength(30)
            .HasConversion(h => h.Value, v => Handle.Create(v));
        builder.HasIndex(c => c.Handle).IsUnique().HasDatabaseName("ix_creator_channels_handle");

        builder.Property(c => c.BannerUrl).HasColumnName("banner_url").HasMaxLength(500);
        builder.Property(c => c.IntroVideoUrl).HasColumnName("intro_video_url").HasMaxLength(500);

        // Lista simples de redes sociais — mesma técnica de Catalog.Course.SalesPageBenefits
        // (serializada como JSON numa única coluna, com ValueComparer explícito porque é uma
        // coleção mutável mapeada via conversão, não um tipo primitivo).
        builder.Ignore(c => c.SocialLinks);
        builder.Property<List<SocialLink>>("_socialLinks")
            .HasColumnName("social_links")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v)
                    ? new List<SocialLink>()
                    : JsonSerializer.Deserialize<List<SocialLink>>(v, (JsonSerializerOptions?)null) ?? new List<SocialLink>())
            .Metadata.SetValueComparer(new ValueComparer<List<SocialLink>>(
                (a, b) => (a ?? new List<SocialLink>()).SequenceEqual(b ?? new List<SocialLink>()),
                v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
                v => v.ToList()));

        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();
    }
}
