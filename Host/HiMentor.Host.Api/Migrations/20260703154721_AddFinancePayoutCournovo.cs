using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HiMentor.Host.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancePayoutCournovo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "creator_studio");

            migrationBuilder.AddColumn<string>(
                name: "external_id",
                schema: "streaming",
                table: "videos",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_url",
                schema: "streaming",
                table: "videos",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source",
                schema: "streaming",
                table: "videos",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "title",
                schema: "streaming",
                table: "videos",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "course_id",
                schema: "subscription",
                table: "plans",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "category",
                schema: "catalog",
                table: "courses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "product_type",
                schema: "catalog",
                table: "courses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "sales_page_benefits",
                schema: "catalog",
                table: "courses",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "sales_page_cta_text",
                schema: "catalog",
                table: "courses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sales_page_headline",
                schema: "catalog",
                table: "courses",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sales_page_subheadline",
                schema: "catalog",
                table: "courses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subcategory",
                schema: "catalog",
                table: "courses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "view_count",
                schema: "catalog",
                table: "courses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "course_faq_items",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Question = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Answer = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_faq_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_course_faq_items_courses_CourseId",
                        column: x => x.CourseId,
                        principalSchema: "catalog",
                        principalTable: "courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "leads",
                schema: "creator_studio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leads", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_plans_course_id",
                schema: "subscription",
                table: "plans",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_course_faq_items_CourseId",
                schema: "catalog",
                table: "course_faq_items",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "ix_leads_course_id",
                schema: "creator_studio",
                table: "leads",
                column: "CourseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "course_faq_items",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "leads",
                schema: "creator_studio");

            migrationBuilder.DropIndex(
                name: "ix_plans_course_id",
                schema: "subscription",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "external_id",
                schema: "streaming",
                table: "videos");

            migrationBuilder.DropColumn(
                name: "external_url",
                schema: "streaming",
                table: "videos");

            migrationBuilder.DropColumn(
                name: "source",
                schema: "streaming",
                table: "videos");

            migrationBuilder.DropColumn(
                name: "title",
                schema: "streaming",
                table: "videos");

            migrationBuilder.DropColumn(
                name: "course_id",
                schema: "subscription",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "category",
                schema: "catalog",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "product_type",
                schema: "catalog",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "sales_page_benefits",
                schema: "catalog",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "sales_page_cta_text",
                schema: "catalog",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "sales_page_headline",
                schema: "catalog",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "sales_page_subheadline",
                schema: "catalog",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "subcategory",
                schema: "catalog",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "view_count",
                schema: "catalog",
                table: "courses");
        }
    }
}
