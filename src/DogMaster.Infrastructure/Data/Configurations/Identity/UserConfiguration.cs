using DogMaster.Domain.Contexts.Identity.Entities;
using DogMaster.Domain.Contexts.Identity.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogMaster.Infrastructure.Data.Configurations.Identity;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", "identity");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired()
            .HasConversion(e => e.Value, v => Email.Create(v));

        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("ix_users_email");

        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(u => u.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(u => u.Password)
            .HasColumnName("password_hash")
            .HasMaxLength(500)
            .HasConversion(
                p => p == null ? null : p.Hash,
                v => v == null ? null : Domain.Contexts.Identity.ValueObjects.Password.CreateFromHash(v));

        builder.HasOne(u => u.Profile)
            .WithOne()
            .HasForeignKey<Domain.Contexts.Identity.Entities.UserProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.SocialLogins)
            .WithOne()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(u => u.DomainEvents);
    }
}
