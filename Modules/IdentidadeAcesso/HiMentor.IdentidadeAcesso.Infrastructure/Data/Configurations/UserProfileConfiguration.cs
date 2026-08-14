using HiMentor.IdentidadeAcesso.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HiMentor.IdentidadeAcesso.Infrastructure.Data.Configurations;

public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles", "identity");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.LastName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.AvatarUrl).HasMaxLength(500);
        builder.Property(p => p.Phone).HasMaxLength(20);
        builder.Property(p => p.Bio).HasMaxLength(1000);
        builder.Ignore(p => p.FullName);
    }
}
