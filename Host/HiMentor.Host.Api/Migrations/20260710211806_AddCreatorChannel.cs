using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HiMentor.Host.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatorChannel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "channel");

            migrationBuilder.CreateTable(
                name: "creator_channels",
                schema: "channel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    handle = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    social_links = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_creator_channels", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_creator_channels_creator_id",
                schema: "channel",
                table: "creator_channels",
                column: "CreatorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_creator_channels_handle",
                schema: "channel",
                table: "creator_channels",
                column: "handle",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "creator_channels",
                schema: "channel");
        }
    }
}
