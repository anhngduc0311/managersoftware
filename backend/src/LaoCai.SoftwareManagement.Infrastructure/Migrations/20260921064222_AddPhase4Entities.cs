using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaoCai.SoftwareManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase4Entities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "contracts");

            migrationBuilder.EnsureSchema(
                name: "documents");

            migrationBuilder.CreateTable(
                name: "contracts",
                schema: "contracts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    owning_organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Active"),
                    signed_date = table.Column<DateOnly>(type: "date", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "VND"),
                    maintenance_start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    maintenance_end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contracts", x => x.id);
                    table.ForeignKey(
                        name: "fk_contracts_organizations_owning_organization_id",
                        column: x => x.owning_organization_id,
                        principalSchema: "organizations",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contracts_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalSchema: "catalog",
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                schema: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    original_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    checksum_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    scan_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Pending"),
                    scan_message = table.Column<string>(type: "text", nullable: true),
                    uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_orphaned = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cleaned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_documents_users_uploaded_by_user_id",
                        column: x => x.uploaded_by_user_id,
                        principalSchema: "iam",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contract_items",
                schema: "contracts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    software_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_contract_items_contracts_contract_id",
                        column: x => x.contract_id,
                        principalSchema: "contracts",
                        principalTable: "contracts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_contract_items_software_software_id",
                        column: x => x.software_id,
                        principalSchema: "catalog",
                        principalTable: "software",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "document_attachments",
                schema: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attached_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attached_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_attachments", x => x.id);
                    table.ForeignKey(
                        name: "fk_document_attachments_documents_document_id",
                        column: x => x.document_id,
                        principalSchema: "documents",
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_document_attachments_users_attached_by_user_id",
                        column: x => x.attached_by_user_id,
                        principalSchema: "iam",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "license_entitlements",
                schema: "contracts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    license_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: true),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_license_entitlements", x => x.id);
                    table.ForeignKey(
                        name: "fk_license_entitlements_contract_items_contract_item_id",
                        column: x => x.contract_item_id,
                        principalSchema: "contracts",
                        principalTable: "contract_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "license_allocations",
                schema: "contracts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entitlement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    deployment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    allocated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_license_allocations", x => x.id);
                    table.ForeignKey(
                        name: "fk_license_allocations_deployments_deployment_id",
                        column: x => x.deployment_id,
                        principalSchema: "deployments",
                        principalTable: "deployments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_license_allocations_license_entitlements_entitlement_id",
                        column: x => x.entitlement_id,
                        principalSchema: "contracts",
                        principalTable: "license_entitlements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contract_items_contract_id",
                schema: "contracts",
                table: "contract_items",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_items_software_id",
                schema: "contracts",
                table: "contract_items",
                column: "software_id");

            migrationBuilder.CreateIndex(
                name: "ix_contracts_contract_no",
                schema: "contracts",
                table: "contracts",
                column: "contract_no");

            migrationBuilder.CreateIndex(
                name: "ix_contracts_owning_organization_id_status",
                schema: "contracts",
                table: "contracts",
                columns: new[] { "owning_organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_contracts_vendor_id_status",
                schema: "contracts",
                table: "contracts",
                columns: new[] { "vendor_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_document_attachments_attached_by_user_id",
                schema: "documents",
                table: "document_attachments",
                column: "attached_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_document_attachments_document_id_entity_type_entity_id",
                schema: "documents",
                table: "document_attachments",
                columns: new[] { "document_id", "entity_type", "entity_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_document_attachments_entity_type_entity_id",
                schema: "documents",
                table: "document_attachments",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_documents_created_at",
                schema: "documents",
                table: "documents",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_documents_scan_status",
                schema: "documents",
                table: "documents",
                column: "scan_status");

            migrationBuilder.CreateIndex(
                name: "ix_documents_storage_key",
                schema: "documents",
                table: "documents",
                column: "storage_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_documents_uploaded_by_user_id",
                schema: "documents",
                table: "documents",
                column: "uploaded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_license_allocations_deployment_id",
                schema: "contracts",
                table: "license_allocations",
                column: "deployment_id");

            migrationBuilder.CreateIndex(
                name: "ix_license_allocations_entitlement_id_deployment_id",
                schema: "contracts",
                table: "license_allocations",
                columns: new[] { "entitlement_id", "deployment_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_license_entitlements_contract_item_id",
                schema: "contracts",
                table: "license_entitlements",
                column: "contract_item_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_attachments",
                schema: "documents");

            migrationBuilder.DropTable(
                name: "license_allocations",
                schema: "contracts");

            migrationBuilder.DropTable(
                name: "documents",
                schema: "documents");

            migrationBuilder.DropTable(
                name: "license_entitlements",
                schema: "contracts");

            migrationBuilder.DropTable(
                name: "contract_items",
                schema: "contracts");

            migrationBuilder.DropTable(
                name: "contracts",
                schema: "contracts");
        }
    }
}
