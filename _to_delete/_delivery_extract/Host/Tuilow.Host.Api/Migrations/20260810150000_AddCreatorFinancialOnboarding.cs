using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tuilow.Host.Api.Migrations
{
    /// <summary>
    /// Onboarding financeiro do criador via subconta Asaas/BaaS (ver CreatorAsaasSubaccount) --
    /// só cria tabelas novas, nenhuma tabela existente é alterada (creator_asaas_accounts/
    /// creator_asaas_customers do modelo legado ficam intocadas, ver relatório final).
    ///
    /// NOTA IMPORTANTE (ver relatório final, seção Testes/Pendências): esta migration foi escrita
    /// manualmente espelhando exatamente as EntityTypeConfiguration novas (CreatorAsaasSubaccountConfiguration,
    /// CreatorAsaasOnboardingDocumentConfiguration, ProcessedAsaasAccountEventConfiguration) --
    /// não foi possível rodar `dotnet ef migrations add` neste ambiente (SDK .NET 9 não instalável
    /// aqui, ver Pendências) para gerar o par migration+Designer.cs+ModelSnapshot oficial. Antes
    /// de aplicar em qualquer ambiente real, rode `dotnet ef migrations add
    /// AddCreatorFinancialOnboarding` num ambiente com SDK 9 (ele deve detectar que o modelo já
    /// bate com o Up() abaixo e gerar só o Designer.cs/snapshot faltantes) ou substitua esta
    /// migration pela gerada pela ferramenta.
    /// </summary>
    public partial class AddCreatorFinancialOnboarding : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "creator_asaas_subaccounts",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    LegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CpfCnpj = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CompanyType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MobilePhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IncomeValue = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    AddressNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AddressComplement = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Province = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PostalCode = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    AsaasAccountId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    WalletId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ApiKeyEncrypted = table.Column<string>(type: "text", nullable: true),
                    WebhookTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDocumentsSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_creator_asaas_subaccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "processed_asaas_account_events",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AsaasEventId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processed_asaas_account_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "creator_asaas_onboarding_documents",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatorAsaasSubaccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    AsaasDocumentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    OnboardingUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_creator_asaas_onboarding_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_creator_asaas_onboarding_documents_creator_asaas_subaccounts_CreatorAsaasSubaccountId",
                        column: x => x.CreatorAsaasSubaccountId,
                        principalSchema: "finance",
                        principalTable: "creator_asaas_subaccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_creator_asaas_subaccounts_CreatorId",
                schema: "finance",
                table: "creator_asaas_subaccounts",
                column: "CreatorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_creator_asaas_subaccounts_AsaasAccountId",
                schema: "finance",
                table: "creator_asaas_subaccounts",
                column: "AsaasAccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_creator_asaas_subaccounts_WebhookTokenHash",
                schema: "finance",
                table: "creator_asaas_subaccounts",
                column: "WebhookTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_creator_asaas_onboarding_documents_CreatorAsaasSubaccountId_AsaasDocumentId",
                schema: "finance",
                table: "creator_asaas_onboarding_documents",
                columns: new[] { "CreatorAsaasSubaccountId", "AsaasDocumentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_processed_asaas_account_events_AsaasEventId",
                schema: "finance",
                table: "processed_asaas_account_events",
                column: "AsaasEventId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "creator_asaas_onboarding_documents",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "creator_asaas_subaccounts",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "processed_asaas_account_events",
                schema: "finance");
        }
    }
}
