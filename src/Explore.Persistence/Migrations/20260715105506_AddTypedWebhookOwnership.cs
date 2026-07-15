using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTypedWebhookOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_incoming_webhook_messages_webhook_consumer_provider_binding",
                table: "incoming_webhook_messages");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_bulk_replay_operations_webhook_consumers_tenant_id_",
                table: "webhook_bulk_replay_operations");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_bulk_replay_operations_webhook_endpoints_tenant_id_",
                table: "webhook_bulk_replay_operations");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_consumer_provider_bindings_webhook_consumers_tenant",
                table: "webhook_consumer_provider_bindings");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_consumers_actors_tenant_id_owner_actor_id",
                table: "webhook_consumers");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_consumers_users_owner_user_id",
                table: "webhook_consumers");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_delivery_attempts_webhook_endpoints_tenant_id_endpo",
                table: "webhook_delivery_attempts");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_delivery_plan_snapshots_webhook_consumers_tenant_id",
                table: "webhook_delivery_plan_snapshots");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_endpoint_subscriptions_webhook_endpoints_tenant_id_",
                table: "webhook_endpoint_subscriptions");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_endpoints_webhook_consumers_tenant_id_consumer_id",
                table: "webhook_endpoints");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_local_target_snapshots_webhook_endpoints_tenant_id_",
                table: "webhook_local_target_snapshots");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_messages_webhook_consumers_tenant_id_consumer_id",
                table: "webhook_messages");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_provider_publications_webhook_consumer_provider_bin",
                table: "webhook_provider_publications");

            migrationBuilder.DropIndex(
                name: "ix_webhook_provider_publications_tenant_id_provider_binding_id",
                table: "webhook_provider_publications");

            migrationBuilder.DropIndex(
                name: "ix_webhook_messages_tenant_id_consumer_id",
                table: "webhook_messages");

            migrationBuilder.DropIndex(
                name: "ix_webhook_local_target_snapshots_tenant_id_webhook_endpoint_id",
                table: "webhook_local_target_snapshots");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_webhook_endpoints_tenant_id_id",
                table: "webhook_endpoints");

            migrationBuilder.DropIndex(
                name: "ux_webhook_endpoints_tenant_provider_endpoint",
                table: "webhook_endpoints");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_webhook_endpoint_subscriptions_tenant_id_id",
                table: "webhook_endpoint_subscriptions");

            migrationBuilder.DropIndex(
                name: "ux_webhook_endpoint_subscriptions_endpoint_event_type",
                table: "webhook_endpoint_subscriptions");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_webhook_consumers_tenant_id_id",
                table: "webhook_consumers");

            migrationBuilder.DropIndex(
                name: "ix_webhook_consumers_owner_user_id",
                table: "webhook_consumers");

            migrationBuilder.DropIndex(
                name: "ix_webhook_consumers_tenant_id_owner_actor_id",
                table: "webhook_consumers");

            migrationBuilder.DropIndex(
                name: "ux_webhook_consumers_tenant_external_app",
                table: "webhook_consumers");

            migrationBuilder.DropIndex(
                name: "ux_webhook_consumers_tenant_name",
                table: "webhook_consumers");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_webhook_consumer_provider_bindings_tenant_id_id",
                table: "webhook_consumer_provider_bindings");

            migrationBuilder.DropIndex(
                name: "ux_webhook_provider_bindings_tenant_consumer_provider_environment",
                table: "webhook_consumer_provider_bindings");

            migrationBuilder.DropIndex(
                name: "ux_webhook_provider_bindings_tenant_provider_environment_application_uid",
                table: "webhook_consumer_provider_bindings");

            migrationBuilder.DropIndex(
                name: "ux_webhook_provider_bindings_tenant_provider_environment_external_app",
                table: "webhook_consumer_provider_bindings");

            migrationBuilder.DropIndex(
                name: "ix_webhook_bulk_replay_operations_tenant_id_webhook_consumer_id",
                table: "webhook_bulk_replay_operations");

            migrationBuilder.DropIndex(
                name: "ix_webhook_bulk_replay_operations_tenant_id_webhook_endpoint_id",
                table: "webhook_bulk_replay_operations");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_webhook_audit_events_tenant_id_id",
                table: "webhook_audit_events");

            migrationBuilder.DropIndex(
                name: "ix_webhook_audit_events_effective_scope_kind_id",
                table: "webhook_audit_events");

            migrationBuilder.DropIndex(
                name: "ix_tenantusers_tenant_user",
                table: "tenant_users");

            migrationBuilder.RenameColumn(
                name: "owner_actor_id",
                table: "webhook_consumers",
                newName: "organization_id");

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                table: "webhook_endpoints",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "instance_id",
                table: "webhook_endpoints",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                table: "webhook_endpoint_subscriptions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "instance_id",
                table: "webhook_endpoint_subscriptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                table: "webhook_consumers",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "group_id",
                table: "webhook_consumers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "instance_id",
                table: "webhook_consumers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                table: "webhook_consumer_provider_bindings",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                table: "webhook_audit_events",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddUniqueConstraint(
                name: "ak_tenant_users_tenant_id_user_id",
                table: "tenant_users",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_provider_publications_provider_binding_id",
                table: "webhook_provider_publications",
                column: "provider_binding_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_messages_consumer_id",
                table: "webhook_messages",
                column: "consumer_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_local_target_snapshots_webhook_endpoint_id",
                table: "webhook_local_target_snapshots",
                column: "webhook_endpoint_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_endpoints_consumer_id",
                table: "webhook_endpoints",
                column: "consumer_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_endpoints_instance_consumer_status",
                table: "webhook_endpoints",
                columns: new[] { "instance_id", "consumer_id", "status_id" },
                filter: "instance_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_webhook_endpoints_instance_provider_endpoint",
                table: "webhook_endpoints",
                columns: new[] { "instance_id", "provider_endpoint_id" },
                unique: true,
                filter: "instance_id IS NOT NULL AND provider_endpoint_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_webhook_endpoints_tenant_provider_endpoint",
                table: "webhook_endpoints",
                columns: new[] { "tenant_id", "provider_endpoint_id" },
                unique: true,
                filter: "tenant_id IS NOT NULL AND provider_endpoint_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_webhook_endpoints_configuration_scope",
                table: "webhook_endpoints",
                sql: "(tenant_id IS NOT NULL AND instance_id IS NULL) OR (tenant_id IS NULL AND instance_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_endpoint_subscriptions_endpoint_id",
                table: "webhook_endpoint_subscriptions",
                column: "endpoint_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_endpoint_subscriptions_instance_event_type",
                table: "webhook_endpoint_subscriptions",
                columns: new[] { "instance_id", "event_type_id", "is_enabled" },
                filter: "instance_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_webhook_endpoint_subscriptions_endpoint_event_type",
                table: "webhook_endpoint_subscriptions",
                columns: new[] { "tenant_id", "endpoint_id", "event_type_id" },
                unique: true,
                filter: "tenant_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_webhook_endpoint_subscriptions_instance_endpoint_event_type",
                table: "webhook_endpoint_subscriptions",
                columns: new[] { "instance_id", "endpoint_id", "event_type_id" },
                unique: true,
                filter: "instance_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_webhook_endpoint_subscriptions_configuration_scope",
                table: "webhook_endpoint_subscriptions",
                sql: "(tenant_id IS NOT NULL AND instance_id IS NULL) OR (tenant_id IS NULL AND instance_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_delivery_plan_snapshots_webhook_consumer_id",
                table: "webhook_delivery_plan_snapshots",
                column: "webhook_consumer_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_delivery_attempts_endpoint_id",
                table: "webhook_delivery_attempts",
                column: "endpoint_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_consumers_instance_status_provider",
                table: "webhook_consumers",
                columns: new[] { "instance_id", "status_id", "provider_mode_id" },
                filter: "instance_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_webhook_consumers_external_app",
                table: "webhook_consumers",
                column: "external_provider_app_id",
                unique: true,
                filter: "external_provider_app_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_webhook_consumers_group_name",
                table: "webhook_consumers",
                columns: new[] { "tenant_id", "group_id", "name" },
                unique: true,
                filter: "consumer_kind_id = 3");

            migrationBuilder.CreateIndex(
                name: "ux_webhook_consumers_instance_name",
                table: "webhook_consumers",
                columns: new[] { "instance_id", "name" },
                unique: true,
                filter: "consumer_kind_id = 5");

            migrationBuilder.CreateIndex(
                name: "ux_webhook_consumers_organization_name",
                table: "webhook_consumers",
                columns: new[] { "tenant_id", "organization_id", "name" },
                unique: true,
                filter: "consumer_kind_id = 2");

            migrationBuilder.CreateIndex(
                name: "ux_webhook_consumers_tenant_name",
                table: "webhook_consumers",
                columns: new[] { "tenant_id", "name" },
                unique: true,
                filter: "consumer_kind_id = 1");

            migrationBuilder.CreateIndex(
                name: "ux_webhook_consumers_user_name",
                table: "webhook_consumers",
                columns: new[] { "tenant_id", "owner_user_id", "name" },
                unique: true,
                filter: "consumer_kind_id = 4");

            migrationBuilder.AddCheckConstraint(
                name: "ck_webhook_consumers_typed_owner",
                table: "webhook_consumers",
                sql: "(consumer_kind_id = 1 AND tenant_id IS NOT NULL AND instance_id IS NULL AND organization_id IS NULL AND group_id IS NULL AND owner_user_id IS NULL) OR (consumer_kind_id = 2 AND tenant_id IS NOT NULL AND instance_id IS NULL AND organization_id IS NOT NULL AND group_id IS NULL AND owner_user_id IS NULL) OR (consumer_kind_id = 3 AND tenant_id IS NOT NULL AND instance_id IS NULL AND organization_id IS NULL AND group_id IS NOT NULL AND owner_user_id IS NULL) OR (consumer_kind_id = 4 AND tenant_id IS NOT NULL AND instance_id IS NULL AND organization_id IS NULL AND group_id IS NULL AND owner_user_id IS NOT NULL) OR (consumer_kind_id = 5 AND tenant_id IS NULL AND instance_id IS NOT NULL AND organization_id IS NULL AND group_id IS NULL AND owner_user_id IS NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_consumer_provider_bindings_tenant_id",
                table: "webhook_consumer_provider_bindings",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ux_webhook_provider_bindings_consumer_provider_environment",
                table: "webhook_consumer_provider_bindings",
                columns: new[] { "webhook_consumer_id", "provider_kind_id", "normalized_environment" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_webhook_provider_bindings_provider_environment_application_uid",
                table: "webhook_consumer_provider_bindings",
                columns: new[] { "provider_kind_id", "normalized_environment", "normalized_application_uid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_webhook_provider_bindings_provider_environment_external_app",
                table: "webhook_consumer_provider_bindings",
                columns: new[] { "provider_kind_id", "normalized_environment", "normalized_external_application_id" },
                unique: true,
                filter: "normalized_external_application_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_webhook_consumer_provider_bindings_verified_scope",
                table: "webhook_consumer_provider_bindings",
                sql: "verification_state_id <> 2 OR verified_tenant_id IS NOT DISTINCT FROM tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_bulk_replay_operations_webhook_consumer_id",
                table: "webhook_bulk_replay_operations",
                column: "webhook_consumer_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_bulk_replay_operations_webhook_endpoint_id",
                table: "webhook_bulk_replay_operations",
                column: "webhook_endpoint_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_audit_events_scope_occurred",
                table: "webhook_audit_events",
                columns: new[] { "effective_scope_kind_id", "effective_scope_id", "occurred_at" },
                descending: new[] { false, false, true });

            migrationBuilder.AddCheckConstraint(
                name: "ck_webhook_audit_events_effective_scope",
                table: "webhook_audit_events",
                sql: "(effective_scope_kind_id = 2 AND tenant_id IS NULL AND effective_scope_id IS NOT NULL) OR (effective_scope_kind_id IN (1, 3, 4, 5) AND tenant_id IS NOT NULL AND effective_scope_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_incoming_webhook_messages_webhook_consumer_provider_binding",
                table: "incoming_webhook_messages",
                column: "webhook_consumer_provider_binding_id");

            migrationBuilder.AddForeignKey(
                name: "fk_incoming_webhook_messages_webhook_consumer_provider_binding",
                table: "incoming_webhook_messages",
                column: "webhook_consumer_provider_binding_id",
                principalTable: "webhook_consumer_provider_bindings",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_bulk_replay_operations_webhook_consumers_webhook_co",
                table: "webhook_bulk_replay_operations",
                column: "webhook_consumer_id",
                principalTable: "webhook_consumers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_bulk_replay_operations_webhook_endpoints_webhook_en",
                table: "webhook_bulk_replay_operations",
                column: "webhook_endpoint_id",
                principalTable: "webhook_endpoints",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_consumer_provider_bindings_webhook_consumers_webhoo",
                table: "webhook_consumer_provider_bindings",
                column: "webhook_consumer_id",
                principalTable: "webhook_consumers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_consumers_groups_tenant_id_group_id",
                table: "webhook_consumers",
                columns: new[] { "tenant_id", "group_id" },
                principalTable: "groups",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_consumers_instance_bootstrap_states_instance_id",
                table: "webhook_consumers",
                column: "instance_id",
                principalTable: "instance_bootstrap_states",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_consumers_organizations_tenant_id_organization_id",
                table: "webhook_consumers",
                columns: new[] { "tenant_id", "organization_id" },
                principalTable: "organizations",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_consumers_tenant_users_tenant_id_owner_user_id",
                table: "webhook_consumers",
                columns: new[] { "tenant_id", "owner_user_id" },
                principalTable: "tenant_users",
                principalColumns: new[] { "tenant_id", "user_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_delivery_attempts_webhook_endpoints_endpoint_id",
                table: "webhook_delivery_attempts",
                column: "endpoint_id",
                principalTable: "webhook_endpoints",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_delivery_plan_snapshots_webhook_consumers_webhook_c",
                table: "webhook_delivery_plan_snapshots",
                column: "webhook_consumer_id",
                principalTable: "webhook_consumers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_endpoint_subscriptions_instance_bootstrap_states_in",
                table: "webhook_endpoint_subscriptions",
                column: "instance_id",
                principalTable: "instance_bootstrap_states",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_endpoint_subscriptions_webhook_endpoints_endpoint_id",
                table: "webhook_endpoint_subscriptions",
                column: "endpoint_id",
                principalTable: "webhook_endpoints",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_endpoints_instance_bootstrap_states_instance_id",
                table: "webhook_endpoints",
                column: "instance_id",
                principalTable: "instance_bootstrap_states",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_endpoints_webhook_consumers_consumer_id",
                table: "webhook_endpoints",
                column: "consumer_id",
                principalTable: "webhook_consumers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_local_target_snapshots_webhook_endpoints_webhook_en",
                table: "webhook_local_target_snapshots",
                column: "webhook_endpoint_id",
                principalTable: "webhook_endpoints",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_messages_webhook_consumers_consumer_id",
                table: "webhook_messages",
                column: "consumer_id",
                principalTable: "webhook_consumers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_provider_publications_webhook_consumer_provider_bin",
                table: "webhook_provider_publications",
                column: "provider_binding_id",
                principalTable: "webhook_consumer_provider_bindings",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_incoming_webhook_messages_webhook_consumer_provider_binding",
                table: "incoming_webhook_messages");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_bulk_replay_operations_webhook_consumers_webhook_co",
                table: "webhook_bulk_replay_operations");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_bulk_replay_operations_webhook_endpoints_webhook_en",
                table: "webhook_bulk_replay_operations");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_consumer_provider_bindings_webhook_consumers_webhoo",
                table: "webhook_consumer_provider_bindings");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_consumers_groups_tenant_id_group_id",
                table: "webhook_consumers");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_consumers_instance_bootstrap_states_instance_id",
                table: "webhook_consumers");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_consumers_organizations_tenant_id_organization_id",
                table: "webhook_consumers");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_consumers_tenant_users_tenant_id_owner_user_id",
                table: "webhook_consumers");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_delivery_attempts_webhook_endpoints_endpoint_id",
                table: "webhook_delivery_attempts");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_delivery_plan_snapshots_webhook_consumers_webhook_c",
                table: "webhook_delivery_plan_snapshots");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_endpoint_subscriptions_instance_bootstrap_states_in",
                table: "webhook_endpoint_subscriptions");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_endpoint_subscriptions_webhook_endpoints_endpoint_id",
                table: "webhook_endpoint_subscriptions");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_endpoints_instance_bootstrap_states_instance_id",
                table: "webhook_endpoints");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_endpoints_webhook_consumers_consumer_id",
                table: "webhook_endpoints");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_local_target_snapshots_webhook_endpoints_webhook_en",
                table: "webhook_local_target_snapshots");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_messages_webhook_consumers_consumer_id",
                table: "webhook_messages");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_provider_publications_webhook_consumer_provider_bin",
                table: "webhook_provider_publications");

            migrationBuilder.DropIndex(
                name: "ix_webhook_provider_publications_provider_binding_id",
                table: "webhook_provider_publications");

            migrationBuilder.DropIndex(
                name: "ix_webhook_messages_consumer_id",
                table: "webhook_messages");

            migrationBuilder.DropIndex(
                name: "ix_webhook_local_target_snapshots_webhook_endpoint_id",
                table: "webhook_local_target_snapshots");

            migrationBuilder.DropIndex(
                name: "ix_webhook_endpoints_consumer_id",
                table: "webhook_endpoints");

            migrationBuilder.DropIndex(
                name: "ix_webhook_endpoints_instance_consumer_status",
                table: "webhook_endpoints");

            migrationBuilder.DropIndex(
                name: "ux_webhook_endpoints_instance_provider_endpoint",
                table: "webhook_endpoints");

            migrationBuilder.DropIndex(
                name: "ux_webhook_endpoints_tenant_provider_endpoint",
                table: "webhook_endpoints");

            migrationBuilder.DropCheckConstraint(
                name: "ck_webhook_endpoints_configuration_scope",
                table: "webhook_endpoints");

            migrationBuilder.DropIndex(
                name: "ix_webhook_endpoint_subscriptions_endpoint_id",
                table: "webhook_endpoint_subscriptions");

            migrationBuilder.DropIndex(
                name: "ix_webhook_endpoint_subscriptions_instance_event_type",
                table: "webhook_endpoint_subscriptions");

            migrationBuilder.DropIndex(
                name: "ux_webhook_endpoint_subscriptions_endpoint_event_type",
                table: "webhook_endpoint_subscriptions");

            migrationBuilder.DropIndex(
                name: "ux_webhook_endpoint_subscriptions_instance_endpoint_event_type",
                table: "webhook_endpoint_subscriptions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_webhook_endpoint_subscriptions_configuration_scope",
                table: "webhook_endpoint_subscriptions");

            migrationBuilder.DropIndex(
                name: "ix_webhook_delivery_plan_snapshots_webhook_consumer_id",
                table: "webhook_delivery_plan_snapshots");

            migrationBuilder.DropIndex(
                name: "ix_webhook_delivery_attempts_endpoint_id",
                table: "webhook_delivery_attempts");

            migrationBuilder.DropIndex(
                name: "ix_webhook_consumers_instance_status_provider",
                table: "webhook_consumers");

            migrationBuilder.DropIndex(
                name: "ux_webhook_consumers_external_app",
                table: "webhook_consumers");

            migrationBuilder.DropIndex(
                name: "ux_webhook_consumers_group_name",
                table: "webhook_consumers");

            migrationBuilder.DropIndex(
                name: "ux_webhook_consumers_instance_name",
                table: "webhook_consumers");

            migrationBuilder.DropIndex(
                name: "ux_webhook_consumers_organization_name",
                table: "webhook_consumers");

            migrationBuilder.DropIndex(
                name: "ux_webhook_consumers_tenant_name",
                table: "webhook_consumers");

            migrationBuilder.DropIndex(
                name: "ux_webhook_consumers_user_name",
                table: "webhook_consumers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_webhook_consumers_typed_owner",
                table: "webhook_consumers");

            migrationBuilder.DropIndex(
                name: "ix_webhook_consumer_provider_bindings_tenant_id",
                table: "webhook_consumer_provider_bindings");

            migrationBuilder.DropIndex(
                name: "ux_webhook_provider_bindings_consumer_provider_environment",
                table: "webhook_consumer_provider_bindings");

            migrationBuilder.DropIndex(
                name: "ux_webhook_provider_bindings_provider_environment_application_uid",
                table: "webhook_consumer_provider_bindings");

            migrationBuilder.DropIndex(
                name: "ux_webhook_provider_bindings_provider_environment_external_app",
                table: "webhook_consumer_provider_bindings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_webhook_consumer_provider_bindings_verified_scope",
                table: "webhook_consumer_provider_bindings");

            migrationBuilder.DropIndex(
                name: "ix_webhook_bulk_replay_operations_webhook_consumer_id",
                table: "webhook_bulk_replay_operations");

            migrationBuilder.DropIndex(
                name: "ix_webhook_bulk_replay_operations_webhook_endpoint_id",
                table: "webhook_bulk_replay_operations");

            migrationBuilder.DropIndex(
                name: "ix_webhook_audit_events_scope_occurred",
                table: "webhook_audit_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_webhook_audit_events_effective_scope",
                table: "webhook_audit_events");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_tenant_users_tenant_id_user_id",
                table: "tenant_users");

            migrationBuilder.DropIndex(
                name: "ix_incoming_webhook_messages_webhook_consumer_provider_binding",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "instance_id",
                table: "webhook_endpoints");

            migrationBuilder.DropColumn(
                name: "instance_id",
                table: "webhook_endpoint_subscriptions");

            migrationBuilder.DropColumn(
                name: "group_id",
                table: "webhook_consumers");

            migrationBuilder.DropColumn(
                name: "instance_id",
                table: "webhook_consumers");

            migrationBuilder.RenameColumn(
                name: "organization_id",
                table: "webhook_consumers",
                newName: "owner_actor_id");

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                table: "webhook_endpoints",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                table: "webhook_endpoint_subscriptions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                table: "webhook_consumers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                table: "webhook_consumer_provider_bindings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                table: "webhook_audit_events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_webhook_endpoints_tenant_id_id",
                table: "webhook_endpoints",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_webhook_endpoint_subscriptions_tenant_id_id",
                table: "webhook_endpoint_subscriptions",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_webhook_consumers_tenant_id_id",
                table: "webhook_consumers",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_webhook_consumer_provider_bindings_tenant_id_id",
                table: "webhook_consumer_provider_bindings",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_webhook_audit_events_tenant_id_id",
                table: "webhook_audit_events",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_provider_publications_tenant_id_provider_binding_id",
                table: "webhook_provider_publications",
                columns: new[] { "tenant_id", "provider_binding_id" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_messages_tenant_id_consumer_id",
                table: "webhook_messages",
                columns: new[] { "tenant_id", "consumer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_local_target_snapshots_tenant_id_webhook_endpoint_id",
                table: "webhook_local_target_snapshots",
                columns: new[] { "tenant_id", "webhook_endpoint_id" });

            migrationBuilder.CreateIndex(
                name: "ux_webhook_endpoints_tenant_provider_endpoint",
                table: "webhook_endpoints",
                columns: new[] { "tenant_id", "provider_endpoint_id" },
                unique: true,
                filter: "provider_endpoint_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_webhook_endpoint_subscriptions_endpoint_event_type",
                table: "webhook_endpoint_subscriptions",
                columns: new[] { "tenant_id", "endpoint_id", "event_type_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_webhook_consumers_owner_user_id",
                table: "webhook_consumers",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_consumers_tenant_id_owner_actor_id",
                table: "webhook_consumers",
                columns: new[] { "tenant_id", "owner_actor_id" });

            migrationBuilder.CreateIndex(
                name: "ux_webhook_consumers_tenant_external_app",
                table: "webhook_consumers",
                columns: new[] { "tenant_id", "external_provider_app_id" },
                unique: true,
                filter: "external_provider_app_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_webhook_consumers_tenant_name",
                table: "webhook_consumers",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_webhook_provider_bindings_tenant_consumer_provider_environment",
                table: "webhook_consumer_provider_bindings",
                columns: new[] { "tenant_id", "webhook_consumer_id", "provider_kind_id", "normalized_environment" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_webhook_provider_bindings_tenant_provider_environment_application_uid",
                table: "webhook_consumer_provider_bindings",
                columns: new[] { "tenant_id", "provider_kind_id", "normalized_environment", "normalized_application_uid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_webhook_provider_bindings_tenant_provider_environment_external_app",
                table: "webhook_consumer_provider_bindings",
                columns: new[] { "tenant_id", "provider_kind_id", "normalized_environment", "normalized_external_application_id" },
                unique: true,
                filter: "normalized_external_application_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_bulk_replay_operations_tenant_id_webhook_consumer_id",
                table: "webhook_bulk_replay_operations",
                columns: new[] { "tenant_id", "webhook_consumer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_bulk_replay_operations_tenant_id_webhook_endpoint_id",
                table: "webhook_bulk_replay_operations",
                columns: new[] { "tenant_id", "webhook_endpoint_id" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_audit_events_effective_scope_kind_id",
                table: "webhook_audit_events",
                column: "effective_scope_kind_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenantusers_tenant_user",
                table: "tenant_users",
                columns: new[] { "tenant_id", "user_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_incoming_webhook_messages_webhook_consumer_provider_binding",
                table: "incoming_webhook_messages",
                columns: new[] { "tenant_id", "webhook_consumer_provider_binding_id" },
                principalTable: "webhook_consumer_provider_bindings",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_bulk_replay_operations_webhook_consumers_tenant_id_",
                table: "webhook_bulk_replay_operations",
                columns: new[] { "tenant_id", "webhook_consumer_id" },
                principalTable: "webhook_consumers",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_bulk_replay_operations_webhook_endpoints_tenant_id_",
                table: "webhook_bulk_replay_operations",
                columns: new[] { "tenant_id", "webhook_endpoint_id" },
                principalTable: "webhook_endpoints",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_consumer_provider_bindings_webhook_consumers_tenant",
                table: "webhook_consumer_provider_bindings",
                columns: new[] { "tenant_id", "webhook_consumer_id" },
                principalTable: "webhook_consumers",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_consumers_actors_tenant_id_owner_actor_id",
                table: "webhook_consumers",
                columns: new[] { "tenant_id", "owner_actor_id" },
                principalTable: "actors",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_consumers_users_owner_user_id",
                table: "webhook_consumers",
                column: "owner_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_delivery_attempts_webhook_endpoints_tenant_id_endpo",
                table: "webhook_delivery_attempts",
                columns: new[] { "tenant_id", "endpoint_id" },
                principalTable: "webhook_endpoints",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_delivery_plan_snapshots_webhook_consumers_tenant_id",
                table: "webhook_delivery_plan_snapshots",
                columns: new[] { "tenant_id", "webhook_consumer_id" },
                principalTable: "webhook_consumers",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_endpoint_subscriptions_webhook_endpoints_tenant_id_",
                table: "webhook_endpoint_subscriptions",
                columns: new[] { "tenant_id", "endpoint_id" },
                principalTable: "webhook_endpoints",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_endpoints_webhook_consumers_tenant_id_consumer_id",
                table: "webhook_endpoints",
                columns: new[] { "tenant_id", "consumer_id" },
                principalTable: "webhook_consumers",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_local_target_snapshots_webhook_endpoints_tenant_id_",
                table: "webhook_local_target_snapshots",
                columns: new[] { "tenant_id", "webhook_endpoint_id" },
                principalTable: "webhook_endpoints",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_messages_webhook_consumers_tenant_id_consumer_id",
                table: "webhook_messages",
                columns: new[] { "tenant_id", "consumer_id" },
                principalTable: "webhook_consumers",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_provider_publications_webhook_consumer_provider_bin",
                table: "webhook_provider_publications",
                columns: new[] { "tenant_id", "provider_binding_id" },
                principalTable: "webhook_consumer_provider_bindings",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
