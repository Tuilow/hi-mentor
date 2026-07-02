using Tuilow.Domain.Contexts.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tuilow.Infrastructure.Data.Configurations.Identity;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles", "identity");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
        builder.HasIndex(r => r.Name).IsUnique().HasDatabaseName("ix_roles_name");

        builder.Property(r => r.Description).HasColumnName("description").HasMaxLength(255);
    }
}
