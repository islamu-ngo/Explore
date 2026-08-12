using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationProviderSubscriptionStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ie_registration_provider_bindings_secret_bindings_tenant_id_webhook_secret_binding_id",
                table: "ie_registration_provider_bindings");

            migrationBuilder.DropForeignKey(
                name: "fk_ie_registration_provider_connections_secret_bindings_tenant_id_api_token_secret_binding_id",
                table: "ie_registration_provider_connections");

            migrationBuilder.DropForeignKey(
                name: "fk_ie_registration_provider_connections_secret_bindings_tenant_id_webhook_secret_binding_id",
                table: "ie_registration_provider_connections");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_secret_bindings_scope_id_id",
                table: "ie_secret_bindings");

            migrationBuilder.DropIndex(
                name: "ix_ie_registration_provider_connections_tenant_id_api_token_secret_binding_id",
                table: "ie_registration_provider_connections");

            migrationBuilder.DropIndex(
                name: "ix_ie_registration_provider_connections_tenant_id_webhook_secret_binding_id",
                table: "ie_registration_provider_connections");

            migrationBuilder.DropIndex(
                name: "ix_ie_registration_provider_bindings_tenant_id_webhook_secret_binding_id",
                table: "ie_registration_provider_bindings");

            migrationBuilder.AlterColumn<Guid>(
                name: "scope_id",
                table: "ie_secret_bindings",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "granted_o_auth_scopes",
                table: "ie_registration_provider_connections",
                type: "TEXT",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "last_access_validated_at",
                table: "ie_registration_provider_connections",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_credential_refresh_at",
                table: "ie_registration_provider_connections",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_identity",
                table: "ie_registration_provider_connections",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "pub_sub_configuration_reference",
                table: "ie_registration_provider_connections",
                type: "TEXT",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ie_registration_provider_subscription_states",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    registration_provider_binding_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    provider_event_type = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    watch_id = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    watch_expires_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    response_checkpoint = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    last_notification_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    pending_notification_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    last_sweep_success_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    last_renewal_attempt_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    last_renewal_success_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    next_renewal_attempt_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    next_sweep_attempt_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    failure_category = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    renewal_failure_count = table.Column<int>(type: "INTEGER", nullable: false),
                    sweep_failure_count = table.Column<int>(type: "INTEGER", nullable: false),
                    processing_generation = table.Column<long>(type: "INTEGER", nullable: false),
                    lease_token = table.Column<Guid>(type: "TEXT", nullable: true),
                    lease_expires_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    is_deleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    deleted_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_registration_provider_subscription_states", x => x.id);
                    table.UniqueConstraint("ak_registration_provider_subscription_states_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_registration_provider_subscription_states_failure_counts", "renewal_failure_count >= 0 AND sweep_failure_count >= 0");
                    table.CheckConstraint("ck_registration_provider_subscription_states_generation", "processing_generation >= 0");
                    table.CheckConstraint("ck_registration_provider_subscription_states_watch_expiry", "watch_expires_at > created_at");
                    table.ForeignKey(
                        name: "fk_ie_registration_provider_subscription_states_ie_registration_provider_bindings_tenant_id_registration_provider_binding_id",
                        columns: x => new { x.tenant_id, x.registration_provider_binding_id },
                        principalTable: "ie_registration_provider_bindings",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_registration_provider_subscription_states_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ie_registration_provider_connections_api_token_secret_binding_id",
                table: "ie_registration_provider_connections",
                column: "api_token_secret_binding_id");

            migrationBuilder.CreateIndex(
                name: "ix_ie_registration_provider_connections_webhook_secret_binding_id",
                table: "ie_registration_provider_connections",
                column: "webhook_secret_binding_id");

            migrationBuilder.CreateIndex(
                name: "ix_ie_registration_provider_bindings_webhook_secret_binding_id",
                table: "ie_registration_provider_bindings",
                column: "webhook_secret_binding_id");

            migrationBuilder.CreateIndex(
                name: "ix_registration_provider_subscription_states_renewal_poll",
                table: "ie_registration_provider_subscription_states",
                columns: new[] { "watch_expires_at", "lease_expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_provider_subscription_states_sweep_poll",
                table: "ie_registration_provider_subscription_states",
                columns: new[] { "pending_notification_at", "next_sweep_attempt_at", "lease_expires_at" });

            migrationBuilder.CreateIndex(
                name: "ux_registration_provider_subscription_states_binding_event",
                table: "ie_registration_provider_subscription_states",
                columns: new[] { "tenant_id", "registration_provider_binding_id", "provider_event_type" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_ie_registration_provider_bindings_secret_bindings_webhook_secret_binding_id",
                table: "ie_registration_provider_bindings",
                column: "webhook_secret_binding_id",
                principalTable: "ie_secret_bindings",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_ie_registration_provider_connections_secret_bindings_api_token_secret_binding_id",
                table: "ie_registration_provider_connections",
                column: "api_token_secret_binding_id",
                principalTable: "ie_secret_bindings",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_ie_registration_provider_connections_secret_bindings_webhook_secret_binding_id",
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
                name: "fk_ie_registration_provider_bindings_secret_bindings_webhook_secret_binding_id",
                table: "ie_registration_provider_bindings");

            migrationBuilder.DropForeignKey(
                name: "fk_ie_registration_provider_connections_secret_bindings_api_token_secret_binding_id",
                table: "ie_registration_provider_connections");

            migrationBuilder.DropForeignKey(
                name: "fk_ie_registration_provider_connections_secret_bindings_webhook_secret_binding_id",
                table: "ie_registration_provider_connections");

            migrationBuilder.DropTable(
                name: "ie_registration_provider_subscription_states");

            migrationBuilder.DropIndex(
                name: "ix_ie_registration_provider_connections_api_token_secret_binding_id",
                table: "ie_registration_provider_connections");

            migrationBuilder.DropIndex(
                name: "ix_ie_registration_provider_connections_webhook_secret_binding_id",
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
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_secret_bindings_scope_id_id",
                table: "ie_secret_bindings",
                columns: new[] { "scope_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_registration_provider_connections_tenant_id_api_token_secret_binding_id",
                table: "ie_registration_provider_connections",
                columns: new[] { "tenant_id", "api_token_secret_binding_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_registration_provider_connections_tenant_id_webhook_secret_binding_id",
                table: "ie_registration_provider_connections",
                columns: new[] { "tenant_id", "webhook_secret_binding_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_registration_provider_bindings_tenant_id_webhook_secret_binding_id",
                table: "ie_registration_provider_bindings",
                columns: new[] { "tenant_id", "webhook_secret_binding_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_ie_registration_provider_bindings_secret_bindings_tenant_id_webhook_secret_binding_id",
                table: "ie_registration_provider_bindings",
                columns: new[] { "tenant_id", "webhook_secret_binding_id" },
                principalTable: "ie_secret_bindings",
                principalColumns: new[] { "scope_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_ie_registration_provider_connections_secret_bindings_tenant_id_api_token_secret_binding_id",
                table: "ie_registration_provider_connections",
                columns: new[] { "tenant_id", "api_token_secret_binding_id" },
                principalTable: "ie_secret_bindings",
                principalColumns: new[] { "scope_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_ie_registration_provider_connections_secret_bindings_tenant_id_webhook_secret_binding_id",
                table: "ie_registration_provider_connections",
                columns: new[] { "tenant_id", "webhook_secret_binding_id" },
                principalTable: "ie_secret_bindings",
                principalColumns: new[] { "scope_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
