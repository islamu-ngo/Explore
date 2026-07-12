// ABOUTME: Adds a durable current-outbox pointer to managed tenant provisioning operations.
// ABOUTME: Backfills safely from matching outbox records before enforcing the nonempty invariant.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManagedTenantProvisioningOperationOutboxPointer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "current_outbox_message_id",
                table: "managed_tenant_provisioning_operations",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE managed_tenant_provisioning_operations AS operation
                SET current_outbox_message_id = (
                    SELECT message.id
                    FROM outbox_messages AS message
                    WHERE message.aggregate_type = 'ManagedTenantProvisioningOperation'
                      AND message.aggregate_id = operation.id
                      AND message.event_type = 'ManagedTenantProvisioningProcessRequested'
                    ORDER BY message.created_at DESC, message.id DESC
                    LIMIT 1)
                WHERE operation.current_outbox_message_id IS NULL;

                DO $$
                DECLARE
                    missing_operation_id uuid;
                BEGIN
                    SELECT id
                    INTO missing_operation_id
                    FROM managed_tenant_provisioning_operations
                    WHERE current_outbox_message_id IS NULL
                    LIMIT 1;

                    IF missing_operation_id IS NOT NULL THEN
                        RAISE EXCEPTION 'Cannot fence managed tenant provisioning operation %: no matching process outbox message exists.', missing_operation_id
                            USING HINT = 'Restore or reconcile the missing outbox record explicitly; this migration never deletes operation data.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "current_outbox_message_id",
                table: "managed_tenant_provisioning_operations",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_managed_tenant_provisioning_outbox_pointer",
                table: "managed_tenant_provisioning_operations",
                sql: "current_outbox_message_id <> '00000000-0000-0000-0000-000000000000'::uuid");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_managed_tenant_provisioning_outbox_pointer",
                table: "managed_tenant_provisioning_operations");

            migrationBuilder.DropColumn(
                name: "current_outbox_message_id",
                table: "managed_tenant_provisioning_operations");
        }
    }
}
