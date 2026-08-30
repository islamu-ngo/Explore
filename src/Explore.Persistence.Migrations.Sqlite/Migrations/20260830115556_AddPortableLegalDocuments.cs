using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddPortableLegalDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ie_legal_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    scope = table.Column<int>(type: "INTEGER", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    authority_key = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    kind = table.Column<int>(type: "INTEGER", nullable: false),
                    owner_role = table.Column<int>(type: "INTEGER", nullable: false),
                    state = table.Column<int>(type: "INTEGER", nullable: false),
                    current_version = table.Column<int>(type: "INTEGER", nullable: false),
                    accountable_identity_reference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_legal_documents", x => x.id);
                    table.CheckConstraint("ck_legal_documents_current_version", "current_version > 0");
                    table.CheckConstraint("ck_legal_documents_scope_tenant", "(scope = 1 AND tenant_id IS NULL) OR (scope = 2 AND tenant_id IS NOT NULL)");
                    table.CheckConstraint("ck_legal_documents_state", "state >= 1 AND state <= 6");
                });

            migrationBuilder.CreateTable(
                name: "ie_legal_document_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    legal_document_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    version = table.Column<int>(type: "INTEGER", nullable: false),
                    audience = table.Column<int>(type: "INTEGER", nullable: false),
                    state = table.Column<int>(type: "INTEGER", nullable: false),
                    content_digest = table.Column<string>(type: "TEXT", unicode: false, maxLength: 64, nullable: false),
                    source_origin = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    requires_fresh_acceptance = table.Column<bool>(type: "INTEGER", nullable: false),
                    template_id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    template_version = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    template_source_kind = table.Column<int>(type: "INTEGER", nullable: true),
                    template_license_expression = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    template_review_reference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    reviewer_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    review_evidence_reference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    accountable_identity_reference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    approved_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    proposed_effective_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    published_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    retired_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_legal_document_versions", x => x.id);
                    table.CheckConstraint("ck_legal_document_versions_state", "state >= 1 AND state <= 6");
                    table.CheckConstraint("ck_legal_document_versions_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_legal_document_versions_legal_documents_legal_document_id",
                        column: x => x.legal_document_id,
                        principalTable: "ie_legal_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ie_legal_document_localized_sources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    legal_document_version_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    language_tag = table.Column<string>(type: "TEXT", unicode: false, maxLength: 35, nullable: false),
                    title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    summary = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    markdown = table.Column<string>(type: "TEXT", maxLength: 262144, nullable: false),
                    utf8_byte_count = table.Column<int>(type: "INTEGER", nullable: false),
                    link_count = table.Column<int>(type: "INTEGER", nullable: false),
                    placeholder_count = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_legal_document_localized_sources", x => x.id);
                    table.CheckConstraint("ck_legal_document_localized_sources_counts", "utf8_byte_count >= 1 AND utf8_byte_count <= 262144 AND link_count >= 0 AND link_count <= 128 AND placeholder_count >= 0 AND placeholder_count <= 64");
                    table.ForeignKey(
                        name: "fk_legal_document_localized_sources_legal_document_versions_legal_document_version_id",
                        column: x => x.legal_document_version_id,
                        principalTable: "ie_legal_document_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ie_legal_document_publications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    legal_document_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    legal_document_version_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    version = table.Column<int>(type: "INTEGER", nullable: false),
                    lifecycle_state = table.Column<int>(type: "INTEGER", nullable: false),
                    content_digest = table.Column<string>(type: "TEXT", unicode: false, maxLength: 64, nullable: false),
                    accountable_identity_reference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    review_evidence_reference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    effective_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    requires_fresh_acceptance = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_legal_document_publications", x => x.id);
                    table.CheckConstraint("ck_legal_document_publications_state", "lifecycle_state IN (5, 6)");
                    table.CheckConstraint("ck_legal_document_publications_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_legal_document_publications_legal_document_versions_legal_document_version_id",
                        column: x => x.legal_document_version_id,
                        principalTable: "ie_legal_document_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_legal_document_publications_legal_documents_legal_document_id",
                        column: x => x.legal_document_id,
                        principalTable: "ie_legal_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_legal_document_localized_sources_legal_document_version_id_language_tag",
                table: "ie_legal_document_localized_sources",
                columns: new[] { "legal_document_version_id", "language_tag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_legal_document_publications_legal_document_id_occurred_at",
                table: "ie_legal_document_publications",
                columns: new[] { "legal_document_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_legal_document_publications_legal_document_id_version_lifecycle_state",
                table: "ie_legal_document_publications",
                columns: new[] { "legal_document_id", "version", "lifecycle_state" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_legal_document_publications_legal_document_version_id",
                table: "ie_legal_document_publications",
                column: "legal_document_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_legal_document_versions_legal_document_id_version",
                table: "ie_legal_document_versions",
                columns: new[] { "legal_document_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_legal_document_versions_state_proposed_effective_at",
                table: "ie_legal_document_versions",
                columns: new[] { "state", "proposed_effective_at" });

            migrationBuilder.CreateIndex(
                name: "ix_legal_documents_authority_key_kind",
                table: "ie_legal_documents",
                columns: new[] { "authority_key", "kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_legal_documents_tenant_id_state_kind",
                table: "ie_legal_documents",
                columns: new[] { "tenant_id", "state", "kind" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ie_legal_document_localized_sources");

            migrationBuilder.DropTable(
                name: "ie_legal_document_publications");

            migrationBuilder.DropTable(
                name: "ie_legal_document_versions");

            migrationBuilder.DropTable(
                name: "ie_legal_documents");
        }
    }
}
