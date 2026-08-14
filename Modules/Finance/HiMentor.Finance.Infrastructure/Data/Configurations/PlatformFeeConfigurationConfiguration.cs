using HiMentor.Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HiMentor.Finance.Infrastructure.Data.Configurations;

public sealed class PlatformFeeConfigurationConfiguration : IEntityTypeConfiguration<PlatformFeeConfiguration>
{
    public void Configure(EntityTypeBuilder<PlatformFeeConfiguration> builder)
    {
        builder.ToTable("platform_fee_configurations", "finance");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Percentage).HasColumnType("numeric(5,2)").IsRequired();
        builder.Property(f => f.IsActive).HasDefaultValue(true);
        builder.Property(f => f.Notes).HasMaxLength(500);
        builder.Property(f => f.EffectiveFrom).IsRequired();

        builder.HasIndex(f => f.IsActive);

        builder.Ignore(f => f.DomainEvents);
    }
}
