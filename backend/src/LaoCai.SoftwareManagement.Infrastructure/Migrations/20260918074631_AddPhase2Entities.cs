using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaoCai.SoftwareManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase2Entities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.EnsureSchema(
                name: "organizations");

            migrationBuilder.CreateTable(
                name: "organizations",
                schema: "organizations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organizations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "software_categories",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_software_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vendors",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    contact_info = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vendors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organization_successions",
                schema: "organizations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    predecessor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    successor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effective_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_successions", x => x.id);
                    table.ForeignKey(
                        name: "fk_organization_successions_organizations_predecessor_id",
                        column: x => x.predecessor_id,
                        principalSchema: "organizations",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_organization_successions_organizations_successor_id",
                        column: x => x.successor_id,
                        principalSchema: "organizations",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "organization_versions",
                schema: "organizations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_versions", x => x.id);
                    table.ForeignKey(
                        name: "fk_organization_versions_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "organizations",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_organization_versions_organizations_parent_id",
                        column: x => x.parent_id,
                        principalSchema: "organizations",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "software",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    lifecycle_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_software", x => x.id);
                    table.ForeignKey(
                        name: "fk_software_software_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "catalog",
                        principalTable: "software_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_software_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalSchema: "catalog",
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "catalog_proposals",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    software_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    created_software_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_proposals", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_proposals_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "organizations",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_catalog_proposals_software_created_software_id",
                        column: x => x.created_software_id,
                        principalSchema: "catalog",
                        principalTable: "software",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_catalog_proposals_users_proposed_by_user_id",
                        column: x => x.proposed_by_user_id,
                        principalSchema: "iam",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_catalog_proposals_users_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalSchema: "iam",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "software_releases",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    software_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    release_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    support_end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_software_releases", x => x.id);
                    table.ForeignKey(
                        name: "fk_software_releases_software_software_id",
                        column: x => x.software_id,
                        principalSchema: "catalog",
                        principalTable: "software",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_proposals_created_software_id",
                schema: "catalog",
                table: "catalog_proposals",
                column: "created_software_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_proposals_organization_id",
                schema: "catalog",
                table: "catalog_proposals",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_proposals_proposed_by_user_id",
                schema: "catalog",
                table: "catalog_proposals",
                column: "proposed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_proposals_reviewed_by_user_id",
                schema: "catalog",
                table: "catalog_proposals",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_successions_predecessor_id",
                schema: "organizations",
                table: "organization_successions",
                column: "predecessor_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_successions_successor_id",
                schema: "organizations",
                table: "organization_successions",
                column: "successor_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_versions_organization_id_valid_from",
                schema: "organizations",
                table: "organization_versions",
                columns: new[] { "organization_id", "valid_from" });

            migrationBuilder.CreateIndex(
                name: "ix_organization_versions_parent_id",
                schema: "organizations",
                table: "organization_versions",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_organizations_code",
                schema: "organizations",
                table: "organizations",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_software_category_id",
                schema: "catalog",
                table: "software",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_software_code",
                schema: "catalog",
                table: "software",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_software_vendor_id",
                schema: "catalog",
                table: "software",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_software_categories_code",
                schema: "catalog",
                table: "software_categories",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_software_releases_software_id_version_name",
                schema: "catalog",
                table: "software_releases",
                columns: new[] { "software_id", "version_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vendors_code",
                schema: "catalog",
                table: "vendors",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_proposals",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "organization_successions",
                schema: "organizations");

            migrationBuilder.DropTable(
                name: "organization_versions",
                schema: "organizations");

            migrationBuilder.DropTable(
                name: "software_releases",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "organizations",
                schema: "organizations");

            migrationBuilder.DropTable(
                name: "software",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "software_categories",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "vendors",
                schema: "catalog");
        }
    }
}
