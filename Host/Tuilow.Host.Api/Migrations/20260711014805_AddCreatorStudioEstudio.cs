using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tuilow.Host.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatorStudioEstudio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "creator_style_profiles",
                schema: "creator_studio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Niche = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetAudience = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Objective = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_creator_style_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "lesson_scripts",
                schema: "creator_studio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: true),
                    LessonId = table.Column<Guid>(type: "uuid", nullable: true),
                    LessonTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Introduction = table.Column<string>(type: "text", nullable: false),
                    ClosingCta = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    WasRecorded = table.Column<bool>(type: "boolean", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    demonstration_suggestions = table.Column<string>(type: "text", nullable: false),
                    development_topics = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lesson_scripts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "recording_templates",
                schema: "creator_studio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    sections = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recording_templates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_creator_style_profiles_creator_id",
                schema: "creator_studio",
                table: "creator_style_profiles",
                column: "CreatorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lesson_scripts_creator_id",
                schema: "creator_studio",
                table: "lesson_scripts",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "ix_recording_templates_creator_id",
                schema: "creator_studio",
                table: "recording_templates",
                column: "CreatorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "creator_style_profiles",
                schema: "creator_studio");

            migrationBuilder.DropTable(
                name: "lesson_scripts",
                schema: "creator_studio");

            migrationBuilder.DropTable(
                name: "recording_templates",
                schema: "creator_studio");
        }
    }
}
