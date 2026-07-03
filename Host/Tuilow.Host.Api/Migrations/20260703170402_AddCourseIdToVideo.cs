using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tuilow.Host.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseIdToVideo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "course_id",
                schema: "streaming",
                table: "videos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_videos_course_id",
                schema: "streaming",
                table: "videos",
                column: "course_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_videos_course_id",
                schema: "streaming",
                table: "videos");

            migrationBuilder.DropColumn(
                name: "course_id",
                schema: "streaming",
                table: "videos");
        }
    }
}
