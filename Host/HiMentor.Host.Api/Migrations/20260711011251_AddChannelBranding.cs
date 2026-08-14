using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HiMentor.Host.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "banner_url",
                schema: "channel",
                table: "creator_channels",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "intro_video_url",
                schema: "channel",
                table: "creator_channels",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "banner_url",
                schema: "channel",
                table: "creator_channels");

            migrationBuilder.DropColumn(
                name: "intro_video_url",
                schema: "channel",
                table: "creator_channels");
        }
    }
}
