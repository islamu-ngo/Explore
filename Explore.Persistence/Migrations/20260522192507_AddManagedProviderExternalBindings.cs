using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManagedProviderExternalBindings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_actors_user_id",
                table: "actors");

            migrationBuilder.CreateTable(
                name: "external_bindings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    external_system = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    external_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    external_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    internal_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    internal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope_tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    external_binding_status_id = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_external_bindings", x => x.id);
                    table.CheckConstraint("ck_external_bindings_status", "external_binding_status_id IN (1, 2, 3)");
                    table.CheckConstraint("ck_external_bindings_text_not_blank", "length(btrim(provider_key)) > 0 AND length(btrim(external_system)) > 0 AND length(btrim(external_type)) > 0 AND length(btrim(external_id)) > 0 AND length(btrim(internal_type)) > 0");
                    table.ForeignKey(
                        name: "fk_external_bindings_tenants_scope_tenant_id",
                        column: x => x.scope_tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_actors_user_id_tenant_id",
                table: "actors",
                columns: new[] { "user_id", "tenant_id" },
                unique: true,
                filter: "user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_external_bindings_external_global_unique",
                table: "external_bindings",
                columns: new[] { "provider_key", "external_system", "external_type", "external_id" },
                unique: true,
                filter: "scope_tenant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_external_bindings_external_tenant_unique",
                table: "external_bindings",
                columns: new[] { "provider_key", "external_system", "external_type", "external_id", "scope_tenant_id" },
                unique: true,
                filter: "scope_tenant_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_external_bindings_internal_global_unique",
                table: "external_bindings",
                columns: new[] { "provider_key", "external_system", "internal_type", "internal_id" },
                unique: true,
                filter: "scope_tenant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_external_bindings_internal_tenant_unique",
                table: "external_bindings",
                columns: new[] { "provider_key", "external_system", "internal_type", "internal_id", "scope_tenant_id" },
                unique: true,
                filter: "scope_tenant_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_external_bindings_scope_tenant_id",
                table: "external_bindings",
                column: "scope_tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "external_bindings");

            migrationBuilder.DropIndex(
                name: "ix_actors_user_id_tenant_id",
                table: "actors");

            migrationBuilder.CreateIndex(
                name: "ix_actors_user_id",
                table: "actors",
                column: "user_id",
                unique: true,
                filter: "user_id IS NOT NULL");
        }
    }
}
