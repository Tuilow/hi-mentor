using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tuilow.Host.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMagicLinkAndAnonymousCheckout2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "guarantee_days",
                schema: "catalog",
                table: "courses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "guarantee_text",
                schema: "catalog",
                table: "courses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sales_page_video_url",
                schema: "catalog",
                table: "courses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "testimonials",
                schema: "catalog",
                table: "courses",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "guarantee_days",
                schema: "catalog",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "guarantee_text",
                schema: "catalog",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "sales_page_video_url",
                schema: "catalog",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "testimonials",
                schema: "catalog",
                table: "courses");
        }
    }
}
