using HiMentor.Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HiMentor.Finance.Infrastructure.Data.Configurations;

public sealed class ProcessedAsaasAccountEventConfiguration : IEntityTypeConfiguration<ProcessedAsaasAccountEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedAsaasAccountEvent> builder)
    {
        builder.ToTable("processed_asaas_account_events", "finance");
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.AsaasEventId).IsUnique();
        builder.Property(e => e.AsaasEventId).HasMaxLength(100).IsRequired();
        builder.Property(e => e.EventType).HasMaxLength(100).IsRequired();
    }
}
