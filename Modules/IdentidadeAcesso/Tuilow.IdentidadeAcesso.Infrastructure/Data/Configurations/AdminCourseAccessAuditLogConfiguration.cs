using Tuilow.IdentidadeAcesso.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tuilow.IdentidadeAcesso.Infrastructure.Data.Configurations;

public sealed class AdminCourseAccessAuditLogConfiguration : IEntityTypeConfiguration<AdminCourseAccessAuditLog>
{
    public void Configure(EntityTypeBuilder<AdminCourseAccessAuditLog> builder)
    {
        builder.ToTable("admin_course_access_audit_logs", "identity");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action).IsRequired().HasMaxLength(50);

        builder.HasIndex(a => a.StudentUserId).HasDatabaseName("ix_admin_course_access_audit_logs_student_user_id");
        builder.HasIndex(a => a.AdminUserId).HasDatabaseName("ix_admin_course_access_audit_logs_admin_user_id");
    }
}
