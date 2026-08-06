using Tuilow.Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tuilow.Finance.Infrastructure.Data.Configurations;

public sealed class CreatorAsaasAccountConfiguration : IEntityTypeConfiguration<CreatorAsaasAccount>
{
    public void Configure(EntityTypeBuilder<CreatorAsaasAccount> builder)
    {
        builder.ToTable("creator_asaas_accounts", "finance");
        builder.HasKey(a => a.Id);

        builder.HasIndex(a => a.CreatorId).IsUnique();
        // Lookup do webhook precisa ser rapido e nao pode colidir entre creators diferentes.
        builder.HasIndex(a => a.WebhookTokenHash).IsUnique();

        builder.Property(a => a.AsaasAccountId).HasMaxLength(100);
        builder.Property(a => a.WalletId).HasMaxLength(100);
        // ApiKeyEncrypted: sem limite curto de tamanho -- o payload protegido pelo Data
        // Protection API e maior que a API Key original (inclui metadados/IV/tag).
        builder.Property(a => a.ApiKeyEncrypted).HasColumnType("text");
        builder.Property(a => a.WebhookTokenHash).HasMaxLength(64); // hex de SHA-256
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(a => a.CpfCnpj).HasMaxLength(20);
        builder.Property(a => a.LegalName).HasMaxLength(200);
        builder.Property(a => a.CommissionOverridePercentage).HasColumnType("numeric(5,2)");
        builder.Property(a => a.LastValidationError).HasMaxLength(500);

        builder.Ignore(a => a.DomainEvents);
    }
}
