using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HiMentor.Host.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancePayoutCoursePurchase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "course_sales");

            migrationBuilder.EnsureSchema(
                name: "finance");

            migrationBuilder.EnsureSchema(
                name: "payout");

            migrationBuilder.CreateTable(
                name: "course_purchases",
                schema: "course_sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AsaasCustomerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AsaasPaymentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_purchases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "creator_wallets",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    available_balance_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    available_balance_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    pending_balance_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    pending_balance_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    total_gross_sales_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    total_gross_sales_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    total_platform_fee_paid_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    total_platform_fee_paid_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    total_net_earned_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    total_net_earned_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    total_withdrawn_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    total_withdrawn_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_creator_wallets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "payout_requests",
                schema: "payout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    requested_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CycleStart = table.Column<DateOnly>(type: "date", nullable: false),
                    CycleEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payout_requests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "platform_fee_configurations",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_fee_configurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "wallet_transactions",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatorWalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    gross_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    gross_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    fee_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    fee_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    net_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    net_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    AppliedFeePercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    ReferenceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CycleStart = table.Column<DateOnly>(type: "date", nullable: false),
                    CycleEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallet_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wallet_transactions_creator_wallets_CreatorWalletId",
                        column: x => x.CreatorWalletId,
                        principalSchema: "finance",
                        principalTable: "creator_wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payout_transactions",
                schema: "payout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayoutRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ExternalReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payout_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payout_transactions_payout_requests_PayoutRequestId",
                        column: x => x.PayoutRequestId,
                        principalSchema: "payout",
                        principalTable: "payout_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_course_purchases_AsaasPaymentId",
                schema: "course_sales",
                table: "course_purchases",
                column: "AsaasPaymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_course_purchases_CreatorId",
                schema: "course_sales",
                table: "course_purchases",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_course_purchases_StudentId_CourseId",
                schema: "course_sales",
                table: "course_purchases",
                columns: new[] { "StudentId", "CourseId" });

            migrationBuilder.CreateIndex(
                name: "IX_creator_wallets_CreatorId",
                schema: "finance",
                table: "creator_wallets",
                column: "CreatorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payout_requests_CreatorId_Status",
                schema: "payout",
                table: "payout_requests",
                columns: new[] { "CreatorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_payout_requests_Status",
                schema: "payout",
                table: "payout_requests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_payout_transactions_PayoutRequestId",
                schema: "payout",
                table: "payout_transactions",
                column: "PayoutRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_platform_fee_configurations_IsActive",
                schema: "finance",
                table: "platform_fee_configurations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_transactions_CreatorWalletId",
                schema: "finance",
                table: "wallet_transactions",
                column: "CreatorWalletId");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_transactions_ReferenceType_ReferenceId",
                schema: "finance",
                table: "wallet_transactions",
                columns: new[] { "ReferenceType", "ReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_wallet_transactions_Type_Status",
                schema: "finance",
                table: "wallet_transactions",
                columns: new[] { "Type", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "course_purchases",
                schema: "course_sales");

            migrationBuilder.DropTable(
                name: "payout_transactions",
                schema: "payout");

            migrationBuilder.DropTable(
                name: "platform_fee_configurations",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "wallet_transactions",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "payout_requests",
                schema: "payout");

            migrationBuilder.DropTable(
                name: "creator_wallets",
                schema: "finance");
        }
    }
}
