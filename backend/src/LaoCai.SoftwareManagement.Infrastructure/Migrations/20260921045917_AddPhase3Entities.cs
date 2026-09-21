using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaoCai.SoftwareManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase3Entities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "deployments");

            migrationBuilder.EnsureSchema(
                name: "jobs");

            migrationBuilder.EnsureSchema(
                name: "notifications");

            migrationBuilder.CreateTable(
                name: "background_jobs",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Queued"),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    max_attempts = table.Column<int>(type: "integer", nullable: false),
                    lease_owner = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    lease_token = table.Column<Guid>(type: "uuid", nullable: true),
                    lease_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_run_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_background_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "idempotency_records",
                schema: "jobs",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_path = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    request_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status_code = table.Column<int>(type: "integer", nullable: false),
                    response_body = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_idempotency_records", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    target_route = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    deduplication_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_notifications_users_recipient_user_id",
                        column: x => x.recipient_user_id,
                        principalSchema: "iam",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "approval_decisions",
                schema: "deployments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    deployment_revision_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_approval_decisions", x => x.id);
                    table.ForeignKey(
                        name: "fk_approval_decisions_users_actor_id",
                        column: x => x.actor_id,
                        principalSchema: "iam",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "deployment_revisions",
                schema: "deployments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    deployment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision_no = table.Column<int>(type: "integer", nullable: false),
                    release_id = table.Column<Guid>(type: "uuid", nullable: true),
                    operational_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    go_live_date = table.Column<DateOnly>(type: "date", nullable: true),
                    responsible_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    workflow_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Draft"),
                    submitted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deployment_revisions", x => x.id);
                    table.ForeignKey(
                        name: "fk_deployment_revisions_software_releases_release_id",
                        column: x => x.release_id,
                        principalSchema: "catalog",
                        principalTable: "software_releases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_deployment_revisions_users_responsible_user_id",
                        column: x => x.responsible_user_id,
                        principalSchema: "iam",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_deployment_revisions_users_submitted_by",
                        column: x => x.submitted_by,
                        principalSchema: "iam",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "deployments",
                schema: "deployments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    software_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    environment = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    instance_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "default"),
                    current_approved_revision_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deployments", x => x.id);
                    table.ForeignKey(
                        name: "fk_deployments_deployment_revisions_current_approved_revision_",
                        column: x => x.current_approved_revision_id,
                        principalSchema: "deployments",
                        principalTable: "deployment_revisions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_deployments_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "organizations",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_deployments_software_software_id",
                        column: x => x.software_id,
                        principalSchema: "catalog",
                        principalTable: "software",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_approval_decisions_actor_id",
                schema: "deployments",
                table: "approval_decisions",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_decisions_deployment_revision_id",
                schema: "deployments",
                table: "approval_decisions",
                column: "deployment_revision_id");

            migrationBuilder.CreateIndex(
                name: "ix_background_jobs_status_next_run_at_lease_until",
                schema: "jobs",
                table: "background_jobs",
                columns: new[] { "status", "next_run_at", "lease_until" });

            migrationBuilder.CreateIndex(
                name: "ix_deployment_revisions_active",
                schema: "deployments",
                table: "deployment_revisions",
                column: "deployment_id",
                unique: true,
                filter: "\"workflow_status\" IN ('Draft', 'Submitted')");

            migrationBuilder.CreateIndex(
                name: "ix_deployment_revisions_deployment_id_approved_at",
                schema: "deployments",
                table: "deployment_revisions",
                columns: new[] { "deployment_id", "approved_at" });

            migrationBuilder.CreateIndex(
                name: "ix_deployment_revisions_deployment_id_revision_no",
                schema: "deployments",
                table: "deployment_revisions",
                columns: new[] { "deployment_id", "revision_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_deployment_revisions_release_id",
                schema: "deployments",
                table: "deployment_revisions",
                column: "release_id");

            migrationBuilder.CreateIndex(
                name: "ix_deployment_revisions_responsible_user_id",
                schema: "deployments",
                table: "deployment_revisions",
                column: "responsible_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_deployment_revisions_submitted_by",
                schema: "deployments",
                table: "deployment_revisions",
                column: "submitted_by");

            migrationBuilder.CreateIndex(
                name: "ix_deployment_revisions_workflow_status_deployment_id",
                schema: "deployments",
                table: "deployment_revisions",
                columns: new[] { "workflow_status", "deployment_id" });

            migrationBuilder.CreateIndex(
                name: "ix_deployments_current_approved_revision_id",
                schema: "deployments",
                table: "deployments",
                column: "current_approved_revision_id");

            migrationBuilder.CreateIndex(
                name: "ix_deployments_organization_id_software_id",
                schema: "deployments",
                table: "deployments",
                columns: new[] { "organization_id", "software_id" });

            migrationBuilder.CreateIndex(
                name: "ix_deployments_software_id_organization_id_environment_instanc",
                schema: "deployments",
                table: "deployments",
                columns: new[] { "software_id", "organization_id", "environment", "instance_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notifications_deduplication_key",
                schema: "notifications",
                table: "notifications",
                column: "deduplication_key",
                unique: true,
                filter: "\"deduplication_key\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_recipient_user_id_read_at_created_at",
                schema: "notifications",
                table: "notifications",
                columns: new[] { "recipient_user_id", "read_at", "created_at" });

            migrationBuilder.AddForeignKey(
                name: "fk_approval_decisions_deployment_revisions_deployment_revision",
                schema: "deployments",
                table: "approval_decisions",
                column: "deployment_revision_id",
                principalSchema: "deployments",
                principalTable: "deployment_revisions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_deployment_revisions_deployments_deployment_id",
                schema: "deployments",
                table: "deployment_revisions",
                column: "deployment_id",
                principalSchema: "deployments",
                principalTable: "deployments",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_deployments_deployment_revisions_current_approved_revision_",
                schema: "deployments",
                table: "deployments");

            migrationBuilder.DropTable(
                name: "approval_decisions",
                schema: "deployments");

            migrationBuilder.DropTable(
                name: "background_jobs",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "idempotency_records",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "notifications",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "deployment_revisions",
                schema: "deployments");

            migrationBuilder.DropTable(
                name: "deployments",
                schema: "deployments");
        }
    }
}
