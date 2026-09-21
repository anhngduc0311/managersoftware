using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaoCai.SoftwareManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_deployment_revisions_users_responsible_user_id",
                schema: "deployments",
                table: "deployment_revisions");

            migrationBuilder.CreateIndex(
                name: "ix_license_entitlements_valid_to_license_type",
                schema: "contracts",
                table: "license_entitlements",
                columns: new[] { "valid_to", "license_type" });

            migrationBuilder.CreateIndex(
                name: "ix_deployment_revisions_operational_status_approved_at",
                schema: "deployments",
                table: "deployment_revisions",
                columns: new[] { "operational_status", "approved_at" });

            migrationBuilder.CreateIndex(
                name: "ix_contracts_status_end_date",
                schema: "contracts",
                table: "contracts",
                columns: new[] { "status", "end_date" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity_type_entity_id_occurred_at",
                schema: "audit",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id", "occurred_at" });

            migrationBuilder.AddForeignKey(
                name: "fk_deployment_revisions_users_responsible_user_id",
                schema: "deployments",
                table: "deployment_revisions",
                column: "responsible_user_id",
                principalSchema: "iam",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_deployment_revisions_users_responsible_user_id",
                schema: "deployments",
                table: "deployment_revisions");

            migrationBuilder.DropIndex(
                name: "ix_license_entitlements_valid_to_license_type",
                schema: "contracts",
                table: "license_entitlements");

            migrationBuilder.DropIndex(
                name: "ix_deployment_revisions_operational_status_approved_at",
                schema: "deployments",
                table: "deployment_revisions");

            migrationBuilder.DropIndex(
                name: "ix_contracts_status_end_date",
                schema: "contracts",
                table: "contracts");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_entity_type_entity_id_occurred_at",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.AddForeignKey(
                name: "fk_deployment_revisions_users_responsible_user_id",
                schema: "deployments",
                table: "deployment_revisions",
                column: "responsible_user_id",
                principalSchema: "iam",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
