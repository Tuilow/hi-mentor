using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HiMentor.Host.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAsaasMarketplaceSplit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CommissionPercentageSnapshot",
                schema: "course_sales",
                table: "course_purchases",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatorAsaasAccountId",
                schema: "course_sales",
                table: "course_purchases",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentModel",
                schema: "course_sales",
                table: "course_purchases",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "asaas_net_value_received",
                schema: "course_sales",
                table: "course_purchases",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "asaas_net_value_received_currency",
                schema: "course_sales",
                table: "course_purchases",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "creator_net_amount",
                schema: "course_sales",
                table: "course_purchases",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "creator_net_currency",
                schema: "course_sales",
                table: "course_purchases",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "platform_commission_amount",
                schema: "course_sales",
                table: "course_purchases",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "platform_commission_currency",
                schema: "course_sales",
                table: "course_purchases",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "creator_asaas_accounts",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AsaasAccountId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    WalletId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ApiKeyEncrypted = table.Column<string>(type: "text", nullable: false),
                    WebhookTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CpfCnpj = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    LegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CommissionOverridePercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    IsEnabledForSelling = table.Column<bool>(type: "boolean", nullable: false),
                    LastValidationError = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LastValidatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastWebhookReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_creator_asaas_accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "creator_asaas_customers",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatorAsaasAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AsaasCustomerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_creator_asaas_customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_course_purchases_CreatorAsaasAccountId",
                schema: "course_sales",
                table: "course_purchases",
                column: "CreatorAsaasAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_creator_asaas_accounts_CreatorId",
                schema: "finance",
                table: "creator_asaas_accounts",
                column: "CreatorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_creator_asaas_accounts_WebhookTokenHash",
                schema: "finance",
                table: "creator_asaas_accounts",
                column: "WebhookTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_creator_asaas_customers_CreatorAsaasAccountId_StudentId",
                schema: "finance",
                table: "creator_asaas_customers",
                columns: new[] { "CreatorAsaasAccountId", "StudentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "creator_asaas_accounts",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "creator_asaas_customers",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "DataProtectionKeys",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_course_purchases_CreatorAsaasAccountId",
                schema: "course_sales",
                table: "course_purchases");

            migrationBuilder.DropColumn(
                name: "CommissionPercentageSnapshot",
                schema: "course_sales",
                table: "course_purchases");

            migrationBuilder.DropColumn(
                name: "CreatorAsaasAccountId",
                schema: "course_sales",
                table: "course_purchases");

            migrationBuilder.DropColumn(
                name: "PaymentModel",
                schema: "course_sales",
                table: "course_purchases");

            migrationBuilder.DropColumn(
                name: "asaas_net_value_received",
                schema: "course_sales",
                table: "course_purchases");

            migrationBuilder.DropColumn(
                name: "asaas_net_value_received_currency",
                schema: "course_sales",
                table: "course_purchases");

            migrationBuilder.DropColumn(
                name: "creator_net_amount",
                schema: "course_sales",
                table: "course_purchases");

            migrationBuilder.DropColumn(
                name: "creator_net_currency",
                schema: "course_sales",
                table: "course_purchases");

            migrationBuilder.DropColumn(
                name: "platform_commission_amount",
                schema: "course_sales",
                table: "course_purchases");

            migrationBuilder.DropColumn(
                name: "platform_commission_currency",
                schema: "course_sales",
                table: "course_purchases");
        }
    }
}
