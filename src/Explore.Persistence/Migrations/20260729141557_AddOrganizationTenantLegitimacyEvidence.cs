using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationTenantLegitimacyEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "owning_resource_id",
                table: "storage_upload_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "owning_resource_kind",
                table: "storage_upload_sessions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "organization_tenant_evidence",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_storage_object_id = table.Column<Guid>(type: "uuid", nullable: false),
                    review_status_id = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    review_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_tenant_evidence", x => x.id);
                    table.UniqueConstraint("ak_organization_tenant_evidence_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_organization_tenant_evidence_approval_statuses_review_statu",
                        column: x => x.review_status_id,
                        principalTable: "approval_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_organization_tenant_evidence_organization_tenants_tenant_id",
                        columns: x => new { x.tenant_id, x.organization_tenant_id },
                        principalTable: "organization_tenants",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_organization_tenant_evidence_storage_objects_tenant_id_docu",
                        columns: x => new { x.tenant_id, x.document_storage_object_id },
                        principalTable: "storage_objects",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_organization_tenant_evidence_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_organization_tenant_evidence_users_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_storage_upload_sessions_tenant_owner",
                table: "storage_upload_sessions",
                columns: new[] { "tenant_id", "owning_resource_kind", "owning_resource_id" },
                filter: "owning_resource_kind IS NOT NULL AND owning_resource_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_organization_tenant_evidence_review_status_id",
                table: "organization_tenant_evidence",
                column: "review_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_tenant_evidence_reviewed_by_user_id",
                table: "organization_tenant_evidence",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_tenant_evidence_tenant_id_document_storage_obj",
                table: "organization_tenant_evidence",
                columns: new[] { "tenant_id", "document_storage_object_id" });

            migrationBuilder.CreateIndex(
                name: "ix_organization_tenant_evidence_tenant_id_organization_tenant_",
                table: "organization_tenant_evidence",
                columns: new[] { "tenant_id", "organization_tenant_id", "document_storage_object_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_organization_tenant_evidence_tenant_id_organization_tenant_1",
                table: "organization_tenant_evidence",
                columns: new[] { "tenant_id", "organization_tenant_id", "review_status_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organization_tenant_evidence");

            migrationBuilder.DropIndex(
                name: "ix_storage_upload_sessions_tenant_owner",
                table: "storage_upload_sessions");

            migrationBuilder.DropColumn(
                name: "owning_resource_id",
                table: "storage_upload_sessions");

            migrationBuilder.DropColumn(
                name: "owning_resource_kind",
                table: "storage_upload_sessions");
        }
    }
}
