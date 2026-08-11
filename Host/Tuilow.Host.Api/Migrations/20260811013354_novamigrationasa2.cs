using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tuilow.Host.Api.Migrations
{
    /// <inheritdoc />
    public partial class novamigrationasa2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastAccountStatusSyncedAt",
                schema: "finance",
                table: "creator_asaas_subaccounts",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastAccountStatusSyncedAt",
                schema: "finance",
                table: "creator_asaas_subaccounts");
        }
    }
}
