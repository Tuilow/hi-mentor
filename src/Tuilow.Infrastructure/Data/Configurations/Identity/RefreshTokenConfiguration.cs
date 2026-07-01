using Tuilow.Domain.Contexts.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tuilow.Infrastructure.Data.Configurations.Identity;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens", "identity");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Token).HasMaxLength(256).IsRequired();
        builder.HasIndex(r => r.Token).IsUnique();
        builder.Property(r => r.CreatedByIp).HasMaxLength(45);
        builder.Property(r => r.ReplacedByToken).HasMaxLength(256);
        builder.Property(r => r.ExpiresAt).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.UpdatedAt).IsRequired();
    }
}
