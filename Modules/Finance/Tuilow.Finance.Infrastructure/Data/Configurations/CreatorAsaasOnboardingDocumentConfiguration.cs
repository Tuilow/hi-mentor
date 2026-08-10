using Tuilow.Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tuilow.Finance.Infrastructure.Data.Configurations;

public sealed class CreatorAsaasOnboardingDocumentConfiguration : IEntityTypeConfiguration<CreatorAsaasOnboardingDocument>
{
    public void Configure(EntityTypeBuilder<CreatorAsaasOnboardingDocument> builder)
    {
        builder.ToTable("creator_asaas_onboarding_documents", "finance");
        builder.HasKey(d => d.Id);

        builder.HasIndex(d => new { d.CreatorAsaasSubaccountId, d.AsaasDocumentId }).IsUnique();

        builder.Property(d => d.AsaasDocumentId).HasMaxLength(100).IsRequired();
        builder.Property(d => d.Type).HasMaxLength(100).IsRequired();
        builder.Property(d => d.Title).HasMaxLength(300).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(1000);
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(d => d.OnboardingUrl).HasMaxLength(500);
    }
}
