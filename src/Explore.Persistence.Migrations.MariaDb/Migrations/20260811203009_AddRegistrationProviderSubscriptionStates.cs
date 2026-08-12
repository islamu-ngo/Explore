using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.MariaDb.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationProviderSubscriptionStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ie_registration_provider_bindings_ie_secret_bindings_8A893DE3",
                table: "ie_registration_provider_bindings");

            migrationBuilder.DropForeignKey(
                name: "FK_ie_registration_provider_connections_ie_secret_bindi_3DB329FD",
                table: "ie_registration_provider_connections");

            migrationBuilder.DropForeignKey(
                name: "FK_ie_registration_provider_connections_ie_secret_bindi_D238C5F9",
                table: "ie_registration_provider_connections");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_secret_bindings_scope_id_id",
                table: "ie_secret_bindings");

            migrationBuilder.DropIndex(
                name: "IX_ie_registration_provider_connections_tenant_id_api_t_FD21D9A5",
                table: "ie_registration_provider_connections");

            migrationBuilder.DropIndex(
                name: "IX_ie_registration_provider_connections_tenant_id_webho_FB44C077",
                table: "ie_registration_provider_connections");

            migrationBuilder.DropIndex(
                name: "IX_ie_registration_provider_bindings_tenant_id_webhook__77525641",
                table: "ie_registration_provider_bindings");

            migrationBuilder.AlterColumn<Guid>(
                name: "scope_id",
                table: "ie_secret_bindings",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)")
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "conformance_evidence_revision",
                table: "ie_registration_provider_connections",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "granted_o_auth_scopes",
                table: "ie_registration_provider_connections",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "last_access_validated_at",
                table: "ie_registration_provider_connections",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_credential_refresh_at",
                table: "ie_registration_provider_connections",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_identity",
                table: "ie_registration_provider_connections",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "pub_sub_configuration_reference",
                table: "ie_registration_provider_connections",
                type: "varchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_registration_provider_subscription_states",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    registration_provider_binding_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    provider_event_type = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    watch_id = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    watch_expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    response_checkpoint = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_notification_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    pending_notification_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    last_sweep_success_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    last_renewal_attempt_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    last_renewal_success_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    next_renewal_attempt_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    next_sweep_attempt_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    failure_category = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    renewal_failure_count = table.Column<int>(type: "int", nullable: false),
                    sweep_failure_count = table.Column<int>(type: "int", nullable: false),
                    processing_generation = table.Column<long>(type: "bigint", nullable: false),
                    lease_token = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    lease_expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    deleted_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_registration_provider_subscription_states", x => x.id);
                    table.UniqueConstraint("ak_registration_provider_subscription_states_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_registration_provider_subscription_states_failure_counts", "renewal_failure_count >= 0 AND sweep_failure_count >= 0");
                    table.CheckConstraint("ck_registration_provider_subscription_states_generation", "processing_generation >= 0");
                    table.CheckConstraint("ck_registration_provider_subscription_states_watch_expiry", "watch_expires_at > created_at");
                    table.ForeignKey(
                        name: "FK_ie_registration_provider_subscription_states_ie_regi_343A1C29",
                        columns: x => new { x.tenant_id, x.registration_provider_binding_id },
                        principalTable: "ie_registration_provider_bindings",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_registration_provider_subscription_states_ie_tena_9A6DB097",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ie_registration_provider_connections_api_token_secre_089F7099",
                table: "ie_registration_provider_connections",
                column: "api_token_secret_binding_id");

            migrationBuilder.CreateIndex(
                name: "IX_ie_registration_provider_connections_webhook_secret__3770094C",
                table: "ie_registration_provider_connections",
                column: "webhook_secret_binding_id");

            migrationBuilder.CreateIndex(
                name: "ix_ie_registration_provider_bindings_webhook_secret_binding_id",
                table: "ie_registration_provider_bindings",
                column: "webhook_secret_binding_id");

            migrationBuilder.CreateIndex(
                name: "IX_ie_registration_provider_subscription_states_pending_021E53D6",
                table: "ie_registration_provider_subscription_states",
                columns: new[] { "pending_notification_at", "next_sweep_attempt_at", "lease_expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_registration_provider_subscription_states_tenant__CA01FABE",
                table: "ie_registration_provider_subscription_states",
                columns: new[] { "tenant_id", "registration_provider_binding_id", "provider_event_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_registration_provider_subscription_states_watch_e_9B3AC270",
                table: "ie_registration_provider_subscription_states",
                columns: new[] { "watch_expires_at", "lease_expires_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_ie_registration_provider_bindings_ie_secret_bindings_69A51153",
                table: "ie_registration_provider_bindings",
                column: "webhook_secret_binding_id",
                principalTable: "ie_secret_bindings",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ie_registration_provider_connections_ie_secret_bindi_5B68AC56",
                table: "ie_registration_provider_connections",
                column: "api_token_secret_binding_id",
                principalTable: "ie_secret_bindings",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ie_registration_provider_connections_ie_secret_bindi_C65A77CA",
                table: "ie_registration_provider_connections",
                column: "webhook_secret_binding_id",
                principalTable: "ie_secret_bindings",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ie_registration_provider_bindings_ie_secret_bindings_69A51153",
                table: "ie_registration_provider_bindings");

            migrationBuilder.DropForeignKey(
                name: "FK_ie_registration_provider_connections_ie_secret_bindi_5B68AC56",
                table: "ie_registration_provider_connections");

            migrationBuilder.DropForeignKey(
                name: "FK_ie_registration_provider_connections_ie_secret_bindi_C65A77CA",
                table: "ie_registration_provider_connections");

            migrationBuilder.DropTable(
                name: "ie_registration_provider_subscription_states");

            migrationBuilder.DropIndex(
                name: "IX_ie_registration_provider_connections_api_token_secre_089F7099",
                table: "ie_registration_provider_connections");

            migrationBuilder.DropIndex(
                name: "IX_ie_registration_provider_connections_webhook_secret__3770094C",
                table: "ie_registration_provider_connections");

            migrationBuilder.DropIndex(
                name: "ix_ie_registration_provider_bindings_webhook_secret_binding_id",
                table: "ie_registration_provider_bindings");

            migrationBuilder.DropColumn(
                name: "granted_o_auth_scopes",
                table: "ie_registration_provider_connections");

            migrationBuilder.DropColumn(
                name: "last_access_validated_at",
                table: "ie_registration_provider_connections");

            migrationBuilder.DropColumn(
                name: "last_credential_refresh_at",
                table: "ie_registration_provider_connections");

            migrationBuilder.DropColumn(
                name: "provider_identity",
                table: "ie_registration_provider_connections");

            migrationBuilder.DropColumn(
                name: "pub_sub_configuration_reference",
                table: "ie_registration_provider_connections");

            migrationBuilder.AlterColumn<Guid>(
                name: "scope_id",
                table: "ie_secret_bindings",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "conformance_evidence_revision",
                table: "ie_registration_provider_connections",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(120)",
                oldMaxLength: 120)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddUniqueConstraint(
                name: "ak_secret_bindings_scope_id_id",
                table: "ie_secret_bindings",
                columns: new[] { "scope_id", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_registration_provider_connections_tenant_id_api_t_FD21D9A5",
                table: "ie_registration_provider_connections",
                columns: new[] { "tenant_id", "api_token_secret_binding_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_registration_provider_connections_tenant_id_webho_FB44C077",
                table: "ie_registration_provider_connections",
                columns: new[] { "tenant_id", "webhook_secret_binding_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_registration_provider_bindings_tenant_id_webhook__77525641",
                table: "ie_registration_provider_bindings",
                columns: new[] { "tenant_id", "webhook_secret_binding_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_ie_registration_provider_bindings_ie_secret_bindings_8A893DE3",
                table: "ie_registration_provider_bindings",
                columns: new[] { "tenant_id", "webhook_secret_binding_id" },
                principalTable: "ie_secret_bindings",
                principalColumns: new[] { "scope_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ie_registration_provider_connections_ie_secret_bindi_3DB329FD",
                table: "ie_registration_provider_connections",
                columns: new[] { "tenant_id", "webhook_secret_binding_id" },
                principalTable: "ie_secret_bindings",
                principalColumns: new[] { "scope_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ie_registration_provider_connections_ie_secret_bindi_D238C5F9",
                table: "ie_registration_provider_connections",
                columns: new[] { "tenant_id", "api_token_secret_binding_id" },
                principalTable: "ie_secret_bindings",
                principalColumns: new[] { "scope_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
