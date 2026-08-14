using HiMentor.CreatorStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HiMentor.CreatorStudio.Infrastructure.Data.Configurations;

public sealed class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("leads", "creator_studio");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.CourseId).IsRequired();
        builder.Property(l => l.Name).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Email).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Phone).HasMaxLength(30);
        builder.Property(l => l.Source).HasMaxLength(50);
        builder.Property(l => l.CreatedAt).IsRequired();
        builder.Property(l => l.UpdatedAt).IsRequired();

        builder.HasIndex(l => l.CourseId).HasDatabaseName("ix_leads_course_id");
    }
}
