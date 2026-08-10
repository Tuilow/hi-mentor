using Tuilow.Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tuilow.Finance.Infrastructure.Data.Configurations;

public sealed class CreatorAsaasSubaccountConfiguration : IEntityTypeConfiguration<CreatorAsaasSubaccount>
{
    public void Configure(EntityTypeBuilder<CreatorAsaasSubaccount> builder)
    {
        builder.ToTable("creator_asaas_subaccounts", "finance");
        builder.HasKey(a => a.Id);

        builder.HasIndex(a => a.CreatorId).IsUnique();
        builder.HasIndex(a => a.AsaasAccountId).IsUnique();
        builder.HasIndex(a => a.WebhookTokenHash).IsUnique();

        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(a => a.LegalName).HasMaxLength(200);
        builder.Property(a => a.CpfCnpj).HasMaxLength(20);
        builder.Property(a => a.CompanyType).HasMaxLength(30);
        builder.Property(a => a.Email).HasMaxLength(200);
        builder.Property(a => a.MobilePhone).HasMaxLength(20);
        builder.Property(a => a.Phone).HasMaxLength(20);
        builder.Property(a => a.IncomeValue).HasColumnType("numeric(14,2)");
        builder.Property(a => a.Address).HasMaxLength(300);
        builder.Property(a => a.AddressNumber).HasMaxLength(20);
        builder.Property(a => a.AddressComplement).HasMaxLength(200);
        builder.Property(a => a.Province).HasMaxLength(150);
        builder.Property(a => a.PostalCode).HasMaxLength(12);

        builder.Property(a => a.AsaasAccountId).HasMaxLength(100);
        builder.Property(a => a.WalletId).HasMaxLength(100);
        // ApiKeyEncrypted: mesmo racional de CreatorAsaasAccountConfiguration — payload protegido
        // pelo Data Protection API é maior que a API Key original.
        builder.Property(a => a.ApiKeyEncrypted).HasColumnType("text");
        builder.Property(a => a.WebhookTokenHash).HasMaxLength(64); // hex de SHA-256
        builder.Property(a => a.RejectionReason).HasMaxLength(1000);

        builder.HasMany(a => a.Documents)
            .WithOne()
            .HasForeignKey(d => d.CreatorAsaasSubaccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(a => a.DomainEvents);
    }
}
