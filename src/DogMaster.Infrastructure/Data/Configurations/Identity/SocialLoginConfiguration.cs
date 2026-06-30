using DogMaster.Domain.Contexts.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogMaster.Infrastructure.Data.Configurations.Identity;

public sealed class SocialLoginConfiguration : IEntityTypeConfiguration<SocialLogin>
{
    public void Configure(EntityTypeBuilder<SocialLogin> builder)
    {
        builder.ToTable("social_logins", "identity");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Provider).HasMaxLength(50).IsRequired();
        builder.Property(s => s.ExternalId).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Email).HasMaxLength(300);
        builder.HasIndex(s => new { s.Provider, s.ExternalId }).IsUnique();
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();
    }
}
