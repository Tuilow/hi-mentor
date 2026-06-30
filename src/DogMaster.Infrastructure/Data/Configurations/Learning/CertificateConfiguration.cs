using DogMaster.Domain.Contexts.Learning.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogMaster.Infrastructure.Data.Configurations.Learning;

public sealed class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> builder)
    {
        builder.ToTable("certificates", "learning");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(c => c.Code).IsUnique();
        builder.Property(c => c.PdfUrl).HasMaxLength(500);
        builder.Property(c => c.IssuedAt).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();
    }
}
