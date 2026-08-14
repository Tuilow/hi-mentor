using HiMentor.Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HiMentor.Finance.Infrastructure.Data.Configurations;

public sealed class CreatorAsaasCustomerConfiguration : IEntityTypeConfiguration<CreatorAsaasCustomer>
{
    public void Configure(EntityTypeBuilder<CreatorAsaasCustomer> builder)
    {
        builder.ToTable("creator_asaas_customers", "finance");
        builder.HasKey(c => c.Id);

        builder.HasIndex(c => new { c.CreatorAsaasAccountId, c.StudentId }).IsUnique();
        builder.Property(c => c.AsaasCustomerId).HasMaxLength(100).IsRequired();
    }
}
