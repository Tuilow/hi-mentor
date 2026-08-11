using Tuilow.Learning.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tuilow.Learning.Infrastructure.Data.Configurations;

public sealed class AccessCodeRedemptionConfiguration : IEntityTypeConfiguration<AccessCodeRedemption>
{
    public void Configure(EntityTypeBuilder<AccessCodeRedemption> builder)
    {
        builder.ToTable("access_code_redemptions", "learning");
        builder.HasKey(r => r.Id);

        // Impede o mesmo aluno de resgatar o mesmo código duas vezes no banco (defesa em
        // profundidade — a checagem principal já acontece em AccessCode.Redeem).
        builder.HasIndex(r => new { r.AccessCodeId, r.UserId })
            .IsUnique().HasDatabaseName("ix_access_code_redemptions_code_user");
    }
}
