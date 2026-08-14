using HiMentor.IdentidadeAcesso.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HiMentor.IdentidadeAcesso.Infrastructure.Data.Configurations;

public sealed class MagicLinkTokenConfiguration : IEntityTypeConfiguration<MagicLinkToken>
{
    public void Configure(EntityTypeBuilder<MagicLinkToken> builder)
    {
        builder.ToTable("magic_link_tokens", "identity");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Token).HasMaxLength(256).IsRequired();
        builder.HasIndex(m => m.Token).IsUnique();
        builder.Property(m => m.ExpiresAt).IsRequired();
        builder.Property(m => m.CreatedAt).IsRequired();
        builder.Property(m => m.UpdatedAt).IsRequired();
    }
}
