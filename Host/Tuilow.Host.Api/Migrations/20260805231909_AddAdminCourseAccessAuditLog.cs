using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tuilow.Host.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminCourseAccessAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_course_access_audit_logs",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_course_access_audit_logs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_admin_course_access_audit_logs_admin_user_id",
                schema: "identity",
                table: "admin_course_access_audit_logs",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "ix_admin_course_access_audit_logs_student_user_id",
                schema: "identity",
                table: "admin_course_access_audit_logs",
                column: "StudentUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_course_access_audit_logs",
                schema: "identity");
        }
    }
}
