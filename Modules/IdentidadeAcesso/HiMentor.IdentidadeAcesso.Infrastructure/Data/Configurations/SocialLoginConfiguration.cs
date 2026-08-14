using HiMentor.IdentidadeAcesso.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HiMentor.IdentidadeAcesso.Infrastructure.Data.Configurations;

public sealed class SocialLoginConfiguration : IEntityTypeConfiguration<SocialLogin>
{
    public void Configure(EntityTypeBuilder<SocialLogin> builder)
    {
        builder.ToTable("social_logins", "identity");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Provider).HasMaxLength(50).IsRequired();
        builder.Property(s => s.ExternalId).HasMaxLength(200).IsRequired();
        builder.Property(s => s.ExternalEmail).HasMaxLength(300);
        builder.HasIndex(s => new { s.Provider, s.ExternalId }).IsUnique();
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();
    }
}
