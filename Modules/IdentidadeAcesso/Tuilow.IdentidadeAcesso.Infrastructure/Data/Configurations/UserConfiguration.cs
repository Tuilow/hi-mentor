using Tuilow.IdentidadeAcesso.Domain.Entities;
using Tuilow.IdentidadeAcesso.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tuilow.IdentidadeAcesso.Infrastructure.Data.Configurations;

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

        builder.Property(u => u.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(u => u.Password)
            .HasColumnName("password_hash")
            .HasMaxLength(500)
            .HasConversion(
                p => p == null ? null : p.Hash,
                v => v == null ? null : Password.CreateFromHash(v));

        builder.HasOne(u => u.Profile)
            .WithOne()
            .HasForeignKey<UserProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.SocialLogins)
            .WithOne()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.MagicLinkTokens)
            .WithOne()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.UserRoleAssignments)
            .WithOne()
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // O campo é "_userRoles" mas a propriedade é "UserRoleAssignments" — os nomes não
        // batem com a convenção de auto-detecção de backing field do EF Core. Sem isto, o EF
        // tenta materializar Include() chamando Add() no wrapper de _userRoles.AsReadOnly(),
        // que lança NotSupportedException("Collection is read-only"). Aponta explicitamente
        // para o campo real para a fixup de navegação usar reflection sobre ele, não a propriedade.
        builder.Navigation(u => u.UserRoleAssignments)
            .HasField("_userRoles")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(u => u.Roles);
        builder.Ignore(u => u.DomainEvents);
    }
}
