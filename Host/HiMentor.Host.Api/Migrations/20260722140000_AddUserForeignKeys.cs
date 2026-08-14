using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using HiMentor.Host.Api.Data;

#nullable disable

namespace HiMentor.Host.Api.Migrations
{
    /// <summary>
    /// Adiciona, no nível do banco (Postgres), as foreign keys de todas as colunas que hoje
    /// referenciam "identity.users" apenas como um Guid solto (sem FK), respeitando o
    /// isolamento entre módulos: nenhuma classe C# ganhou navegação ou referência de projeto
    /// para o módulo IdentidadeAcesso, essas constraints existem só no banco.
    ///
    /// Grupo "conteúdo" (ON DELETE CASCADE) - se o usuário for removido, o conteúdo dele some junto:
    ///   catalog.courses.InstructorId, learning.enrollments.UserId, learning.certificates.UserId,
    ///   learner_profile.learner_profiles.UserId, channel.creator_channels.CreatorId,
    ///   creator_studio.creator_style_profiles.CreatorId, creator_studio.lesson_scripts.CreatorId,
    ///   creator_studio.recording_templates.CreatorId.
    ///
    /// Grupo "financeiro" (ON DELETE RESTRICT) - o Postgres BLOQUEIA a remoção do usuário
    /// (e avisa com um erro claro) enquanto existir uma linha financeira apontando pra ele:
    ///   course_sales.course_purchases.StudentId/CreatorId, subscription.subscriptions.UserId,
    ///   finance.creator_wallets.CreatorId, payout.payout_requests.CreatorId/ReviewedByUserId,
    ///   finance.platform_fee_configurations.CreatedByUserId.
    ///
    /// IMPORTANTE: antes de aplicar esta migration em um banco que já tem dados órfãos
    /// (usuários apagados diretamente, sem passar pela aplicação), rode primeiro o script
    /// cleanup_orphaned_users_data.sql - o Postgres recusa criar qualquer uma dessas FKs
    /// (cascade ou restrict) se já existir uma linha violando a constraint.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260722140000_AddUserForeignKeys")]
    public partial class AddUserForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---- Grupo "conteúdo": cascade ----

            migrationBuilder.AddForeignKey(
                name: "FK_courses_users_InstructorId",
                schema: "catalog",
                table: "courses",
                column: "InstructorId",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_enrollments_users_UserId",
                schema: "learning",
                table: "enrollments",
                column: "UserId",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_certificates_users_UserId",
                schema: "learning",
                table: "certificates",
                column: "UserId",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_learner_profiles_users_UserId",
                schema: "learner_profile",
                table: "learner_profiles",
                column: "UserId",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_creator_channels_users_CreatorId",
                schema: "channel",
                table: "creator_channels",
                column: "CreatorId",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_creator_style_profiles_users_CreatorId",
                schema: "creator_studio",
                table: "creator_style_profiles",
                column: "CreatorId",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_lesson_scripts_users_CreatorId",
                schema: "creator_studio",
                table: "lesson_scripts",
                column: "CreatorId",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_recording_templates_users_CreatorId",
                schema: "creator_studio",
                table: "recording_templates",
                column: "CreatorId",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // ---- Grupo "financeiro": restrict (bloqueia e avisa) ----

            migrationBuilder.AddForeignKey(
                name: "FK_course_purchases_users_StudentId",
                schema: "course_sales",
                table: "course_purchases",
                column: "StudentId",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_course_purchases_users_CreatorId",
                schema: "course_sales",
                table: "course_purchases",
                column: "CreatorId",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_subscriptions_users_UserId",
                schema: "subscription",
                table: "subscriptions",
                column: "UserId",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_creator_wallets_users_CreatorId",
                schema: "finance",
                table: "creator_wallets",
                column: "CreatorId",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payout_requests_users_CreatorId",
                schema: "payout",
                table: "payout_requests",
                column: "CreatorId",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payout_requests_users_ReviewedByUserId",
                schema: "payout",
                table: "payout_requests",
                column: "ReviewedByUserId",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_platform_fee_configurations_users_CreatedByUserId",
                schema: "finance",
                table: "platform_fee_configurations",
                column: "CreatedByUserId",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_courses_users_InstructorId", schema: "catalog", table: "courses");
            migrationBuilder.DropForeignKey(name: "FK_enrollments_users_UserId", schema: "learning", table: "enrollments");
            migrationBuilder.DropForeignKey(name: "FK_certificates_users_UserId", schema: "learning", table: "certificates");
            migrationBuilder.DropForeignKey(name: "FK_learner_profiles_users_UserId", schema: "learner_profile", table: "learner_profiles");
            migrationBuilder.DropForeignKey(name: "FK_creator_channels_users_CreatorId", schema: "channel", table: "creator_channels");
            migrationBuilder.DropForeignKey(name: "FK_creator_style_profiles_users_CreatorId", schema: "creator_studio", table: "creator_style_profiles");
            migrationBuilder.DropForeignKey(name: "FK_lesson_scripts_users_CreatorId", schema: "creator_studio", table: "lesson_scripts");
            migrationBuilder.DropForeignKey(name: "FK_recording_templates_users_CreatorId", schema: "creator_studio", table: "recording_templates");
            migrationBuilder.DropForeignKey(name: "FK_course_purchases_users_StudentId", schema: "course_sales", table: "course_purchases");
            migrationBuilder.DropForeignKey(name: "FK_course_purchases_users_CreatorId", schema: "course_sales", table: "course_purchases");
            migrationBuilder.DropForeignKey(name: "FK_subscriptions_users_UserId", schema: "subscription", table: "subscriptions");
            migrationBuilder.DropForeignKey(name: "FK_creator_wallets_users_CreatorId", schema: "finance", table: "creator_wallets");
            migrationBuilder.DropForeignKey(name: "FK_payout_requests_users_CreatorId", schema: "payout", table: "payout_requests");
            migrationBuilder.DropForeignKey(name: "FK_payout_requests_users_ReviewedByUserId", schema: "payout", table: "payout_requests");
            migrationBuilder.DropForeignKey(name: "FK_platform_fee_configurations_users_CreatedByUserId", schema: "finance", table: "platform_fee_configurations");
        }
    }
}
