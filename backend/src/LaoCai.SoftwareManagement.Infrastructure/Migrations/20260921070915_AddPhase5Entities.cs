using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaoCai.SoftwareManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase5Entities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "reports");

            migrationBuilder.CreateTable(
                name: "coverage_eligibility",
                schema: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    software_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_eligible = table.Column<bool>(type: "boolean", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_coverage_eligibility", x => x.id);
                    table.ForeignKey(
                        name: "fk_coverage_eligibility_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "organizations",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_coverage_eligibility_software_software_id",
                        column: x => x.software_id,
                        principalSchema: "catalog",
                        principalTable: "software",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "export_requests",
                schema: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    export_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    filter_snapshot_json = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Queued"),
                    document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_export_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "import_batches",
                schema: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Pending"),
                    total_rows = table.Column<int>(type: "integer", nullable: false),
                    valid_rows = table.Column<int>(type: "integer", nullable: false),
                    error_rows = table.Column<int>(type: "integer", nullable: false),
                    staging_data_json = table.Column<string>(type: "text", nullable: true),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    validated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    committed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_import_batches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "import_row_errors",
                schema: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    row_index = table.Column<int>(type: "integer", nullable: false),
                    column_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    error_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    raw_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_import_row_errors", x => x.id);
                    table.ForeignKey(
                        name: "fk_import_row_errors_import_batches_batch_id",
                        column: x => x.batch_id,
                        principalSchema: "reports",
                        principalTable: "import_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_coverage_eligibility_organization_id_software_id_valid_from",
                schema: "reports",
                table: "coverage_eligibility",
                columns: new[] { "organization_id", "software_id", "valid_from" });

            migrationBuilder.CreateIndex(
                name: "ix_coverage_eligibility_software_id",
                schema: "reports",
                table: "coverage_eligibility",
                column: "software_id");

            migrationBuilder.CreateIndex(
                name: "ix_export_requests_expires_at",
                schema: "reports",
                table: "export_requests",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_export_requests_requested_by_user_id",
                schema: "reports",
                table: "export_requests",
                column: "requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_export_requests_status",
                schema: "reports",
                table: "export_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_import_batches_requested_by_user_id",
                schema: "reports",
                table: "import_batches",
                column: "requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_import_batches_status",
                schema: "reports",
                table: "import_batches",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_import_row_errors_batch_id_row_index",
                schema: "reports",
                table: "import_row_errors",
                columns: new[] { "batch_id", "row_index" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "coverage_eligibility",
                schema: "reports");

            migrationBuilder.DropTable(
                name: "export_requests",
                schema: "reports");

            migrationBuilder.DropTable(
                name: "import_row_errors",
                schema: "reports");

            migrationBuilder.DropTable(
                name: "import_batches",
                schema: "reports");
        }
    }
}
