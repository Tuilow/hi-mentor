using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HiMentor.Host.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentCorrelationAndNotificationLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourcePurchaseId",
                schema: "learning",
                table: "enrollments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceSubscriptionId",
                schema: "learning",
                table: "enrollments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_enrollments_single_source",
                schema: "learning",
                table: "enrollments",
                sql: "\"SourcePurchaseId\" IS NULL OR \"SourceSubscriptionId\" IS NULL");

            migrationBuilder.CreateTable(
                name: "notification_logs",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Template = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Recipient = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    AsaasPaymentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    Error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_logs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_notification_logs_asaas_payment_id",
                schema: "learning",
                table: "notification_logs",
                column: "AsaasPaymentId");

            migrationBuilder.CreateIndex(
                name: "ix_notification_logs_correlation_id",
                schema: "learning",
                table: "notification_logs",
                column: "CorrelationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_logs",
                schema: "learning");

            migrationBuilder.DropCheckConstraint(
                name: "ck_enrollments_single_source",
                schema: "learning",
                table: "enrollments");

            migrationBuilder.DropColumn(
                name: "SourcePurchaseId",
                schema: "learning",
                table: "enrollments");

            migrationBuilder.DropColumn(
                name: "SourceSubscriptionId",
                schema: "learning",
                table: "enrollments");
        }
    }
}
