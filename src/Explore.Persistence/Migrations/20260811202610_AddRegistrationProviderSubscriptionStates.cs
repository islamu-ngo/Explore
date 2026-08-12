using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationProviderSubscriptionStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_registration_provider_bindings_secret_bindings_tenant_id_we",
                schema: "islamu_event",
                table: "registration_provider_bindings");

            migrationBuilder.DropForeignKey(
                name: "fk_registration_provider_connections_secret_bindings_tenant_id",
                schema: "islamu_event",
                table: "registration_provider_connections");

            migrationBuilder.DropForeignKey(
                name: "fk_registration_provider_connections_secret_bindings_tenant_id1",
                schema: "islamu_event",
                table: "registration_provider_connections");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_secret_bindings_scope_id_id",
                schema: "islamu_event",
                table: "secret_bindings");

            migrationBuilder.DropIndex(
                name: "ix_registration_provider_connections_tenant_id_api_token_secre",
                schema: "islamu_event",
                table: "registration_provider_connections");

            migrationBuilder.DropIndex(
                name: "ix_registration_provider_connections_tenant_id_webhook_secret_",
                schema: "islamu_event",
                table: "registration_provider_connections");

            migrationBuilder.DropIndex(
                name: "ix_registration_provider_bindings_tenant_id_webhook_secret_bin",
                schema: "islamu_event",
                table: "registration_provider_bindings");

            migrationBuilder.AlterColumn<Guid>(
                name: "scope_id",
                schema: "islamu_event",
                table: "secret_bindings",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "conformance_evidence_revision",
                schema: "islamu_event",
                table: "registration_provider_connections",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "granted_o_auth_scopes",
                schema: "islamu_event",
                table: "registration_provider_connections",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "last_access_validated_at",
                schema: "islamu_event",
                table: "registration_provider_connections",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_credential_refresh_at",
                schema: "islamu_event",
                table: "registration_provider_connections",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_identity",
                schema: "islamu_event",
                table: "registration_provider_connections",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "pub_sub_configuration_reference",
                schema: "islamu_event",
                table: "registration_provider_connections",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "registration_provider_subscription_states",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_provider_binding_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_event_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    watch_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    watch_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    response_checkpoint = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    last_notification_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    pending_notification_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_sweep_success_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_renewal_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_renewal_success_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_renewal_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_sweep_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failure_category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    renewal_failure_count = table.Column<int>(type: "integer", nullable: false),
                    sweep_failure_count = table.Column<int>(type: "integer", nullable: false),
                    processing_generation = table.Column<long>(type: "bigint", nullable: false),
                    lease_token = table.Column<Guid>(type: "uuid", nullable: true),
                    lease_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_provider_subscription_states", x => x.id);
                    table.UniqueConstraint("ak_registration_provider_subscription_states_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_registration_provider_subscription_states_failure_counts", "renewal_failure_count >= 0 AND sweep_failure_count >= 0");
                    table.CheckConstraint("ck_registration_provider_subscription_states_generation", "processing_generation >= 0");
                    table.CheckConstraint("ck_registration_provider_subscription_states_watch_expiry", "watch_expires_at > created_at");
                    table.ForeignKey(
                        name: "fk_registration_provider_subscription_states_registration_prov",
                        columns: x => new { x.tenant_id, x.registration_provider_binding_id },
                        principalSchema: "islamu_event",
                        principalTable: "registration_provider_bindings",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_provider_subscription_states_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_registration_provider_connections_api_token_secret_binding_",
                schema: "islamu_event",
                table: "registration_provider_connections",
                column: "api_token_secret_binding_id");

            migrationBuilder.CreateIndex(
                name: "ix_registration_provider_connections_webhook_secret_binding_id",
                schema: "islamu_event",
                table: "registration_provider_connections",
                column: "webhook_secret_binding_id");

            migrationBuilder.CreateIndex(
                name: "ix_registration_provider_bindings_webhook_secret_binding_id",
                schema: "islamu_event",
                table: "registration_provider_bindings",
                column: "webhook_secret_binding_id");

            migrationBuilder.CreateIndex(
                name: "ix_registration_provider_subscription_states_renewal_poll",
                schema: "islamu_event",
                table: "registration_provider_subscription_states",
                columns: new[] { "watch_expires_at", "lease_expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_provider_subscription_states_sweep_poll",
                schema: "islamu_event",
                table: "registration_provider_subscription_states",
                columns: new[] { "pending_notification_at", "next_sweep_attempt_at", "lease_expires_at" });

            migrationBuilder.CreateIndex(
                name: "ux_registration_provider_subscription_states_binding_event",
                schema: "islamu_event",
                table: "registration_provider_subscription_states",
                columns: new[] { "tenant_id", "registration_provider_binding_id", "provider_event_type" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_registration_provider_bindings_secret_bindings_webhook_secr",
                schema: "islamu_event",
                table: "registration_provider_bindings",
                column: "webhook_secret_binding_id",
                principalSchema: "islamu_event",
                principalTable: "secret_bindings",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_registration_provider_connections_secret_bindings_api_token",
                schema: "islamu_event",
                table: "registration_provider_connections",
                column: "api_token_secret_binding_id",
                principalSchema: "islamu_event",
                principalTable: "secret_bindings",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_registration_provider_connections_secret_bindings_webhook_s",
                schema: "islamu_event",
                table: "registration_provider_connections",
                column: "webhook_secret_binding_id",
                principalSchema: "islamu_event",
                principalTable: "secret_bindings",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_registration_provider_bindings_secret_bindings_webhook_secr",
                schema: "islamu_event",
                table: "registration_provider_bindings");

            migrationBuilder.DropForeignKey(
                name: "fk_registration_provider_connections_secret_bindings_api_token",
                schema: "islamu_event",
                table: "registration_provider_connections");

            migrationBuilder.DropForeignKey(
                name: "fk_registration_provider_connections_secret_bindings_webhook_s",
                schema: "islamu_event",
                table: "registration_provider_connections");

            migrationBuilder.DropTable(
                name: "registration_provider_subscription_states",
                schema: "islamu_event");

            migrationBuilder.DropIndex(
                name: "ix_registration_provider_connections_api_token_secret_binding_",
                schema: "islamu_event",
                table: "registration_provider_connections");

            migrationBuilder.DropIndex(
                name: "ix_registration_provider_connections_webhook_secret_binding_id",
                schema: "islamu_event",
                table: "registration_provider_connections");

            migrationBuilder.DropIndex(
                name: "ix_registration_provider_bindings_webhook_secret_binding_id",
                schema: "islamu_event",
                table: "registration_provider_bindings");

            migrationBuilder.DropColumn(
                name: "granted_o_auth_scopes",
                schema: "islamu_event",
                table: "registration_provider_connections");

            migrationBuilder.DropColumn(
                name: "last_access_validated_at",
                schema: "islamu_event",
                table: "registration_provider_connections");

            migrationBuilder.DropColumn(
                name: "last_credential_refresh_at",
                schema: "islamu_event",
                table: "registration_provider_connections");

            migrationBuilder.DropColumn(
                name: "provider_identity",
                schema: "islamu_event",
                table: "registration_provider_connections");

            migrationBuilder.DropColumn(
                name: "pub_sub_configuration_reference",
                schema: "islamu_event",
                table: "registration_provider_connections");

            migrationBuilder.AlterColumn<Guid>(
                name: "scope_id",
                schema: "islamu_event",
                table: "secret_bindings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "conformance_evidence_revision",
                schema: "islamu_event",
                table: "registration_provider_connections",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_secret_bindings_scope_id_id",
                schema: "islamu_event",
                table: "secret_bindings",
                columns: new[] { "scope_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_provider_connections_tenant_id_api_token_secre",
                schema: "islamu_event",
                table: "registration_provider_connections",
                columns: new[] { "tenant_id", "api_token_secret_binding_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_provider_connections_tenant_id_webhook_secret_",
                schema: "islamu_event",
                table: "registration_provider_connections",
                columns: new[] { "tenant_id", "webhook_secret_binding_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_provider_bindings_tenant_id_webhook_secret_bin",
                schema: "islamu_event",
                table: "registration_provider_bindings",
                columns: new[] { "tenant_id", "webhook_secret_binding_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_registration_provider_bindings_secret_bindings_tenant_id_we",
                schema: "islamu_event",
                table: "registration_provider_bindings",
                columns: new[] { "tenant_id", "webhook_secret_binding_id" },
                principalSchema: "islamu_event",
                principalTable: "secret_bindings",
                principalColumns: new[] { "scope_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_registration_provider_connections_secret_bindings_tenant_id",
                schema: "islamu_event",
                table: "registration_provider_connections",
                columns: new[] { "tenant_id", "api_token_secret_binding_id" },
                principalSchema: "islamu_event",
                principalTable: "secret_bindings",
                principalColumns: new[] { "scope_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_registration_provider_connections_secret_bindings_tenant_id1",
                schema: "islamu_event",
                table: "registration_provider_connections",
                columns: new[] { "tenant_id", "webhook_secret_binding_id" },
                principalSchema: "islamu_event",
                principalTable: "secret_bindings",
                principalColumns: new[] { "scope_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
