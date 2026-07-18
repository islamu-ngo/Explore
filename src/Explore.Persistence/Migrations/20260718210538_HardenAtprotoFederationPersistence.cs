// ABOUTME: Replaces legacy federation rows with global canonical records and tenant-owned fenced delivery state.
// ABOUTME: Refuses ambiguous legacy conversion and creates atomic Jetstream cursor, presentation, and quarantine storage.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenAtprotoFederationPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $migration$
                BEGIN
                    IF EXISTS (SELECT 1 FROM atproto_records) OR EXISTS (SELECT 1 FROM pds_sync_outbox) THEN
                        RAISE EXCEPTION 'HardenAtprotoFederationPersistence requires atproto_records and pds_sync_outbox to be empty; legacy federation rows have no safe ownership or cursor backfill.';
                    END IF;
                END
                $migration$;
                """);

            migrationBuilder.DropIndex(
                name: "IX_PdsSyncOutbox_Did",
                table: "pds_sync_outbox");

            migrationBuilder.DropIndex(
                name: "IX_PdsSyncOutbox_SourceEntity",
                table: "pds_sync_outbox");

            migrationBuilder.DropIndex(
                name: "IX_PdsSyncOutbox_Unique",
                table: "pds_sync_outbox");

            migrationBuilder.DropIndex(
                name: "IX_PdsSyncOutbox_WorkerPoll",
                table: "pds_sync_outbox");

            migrationBuilder.RenameIndex(
                name: "ix_atproto_records_did_collection_record_key",
                table: "atproto_records",
                newName: "ux_atproto_records_identity");

            migrationBuilder.AlterColumn<string>(
                name: "source_entity_type",
                table: "pds_sync_outbox",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "source_entity_id",
                table: "pds_sync_outbox",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "pds_host",
                table: "pds_sync_outbox",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "last_error",
                table: "pds_sync_outbox",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "atproto_record_id",
                table: "pds_sync_outbox",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "depends_on_atproto_record_id",
                table: "pds_sync_outbox",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "expected_cid",
                table: "pds_sync_outbox",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "pds_sync_outbox",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "lease_expires_at",
                table: "pds_sync_outbox",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "lease_fence",
                table: "pds_sync_outbox",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "lease_owner",
                table: "pds_sync_outbox",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "lease_token",
                table: "pds_sync_outbox",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payload_hash",
                table: "pds_sync_outbox",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "settled_cid",
                table: "pds_sync_outbox",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "settled_uri",
                table: "pds_sync_outbox",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_version",
                table: "pds_sync_outbox",
                type: "uuid",
                nullable: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "superseded_at",
                table: "pds_sync_outbox",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "superseded_by_id",
                table: "pds_sync_outbox",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "pds_sync_outbox",
                type: "uuid",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "pds_sync_outbox",
                type: "uuid",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "direction",
                table: "atproto_records",
                type: "integer",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "provenance",
                table: "atproto_records",
                type: "integer",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "record_hash",
                table: "atproto_records",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "record_json",
                table: "atproto_records",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "source_cursor",
                table: "atproto_records",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "source_version",
                table: "atproto_records",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "subject_cid",
                table: "atproto_records",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subject_uri",
                table: "atproto_records",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "tombstoned_at",
                table: "atproto_records",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "atproto_records",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.CreateTable(
                name: "atproto_jetstream_consumer_states",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    service = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    cursor = table.Column<long>(type: "bigint", nullable: false),
                    last_event_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    lease_owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    lease_token = table.Column<Guid>(type: "uuid", nullable: true),
                    lease_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    lease_fence = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_atproto_jetstream_consumer_states", x => x.id);
                    table.CheckConstraint("ck_atproto_jetstream_cursor", "cursor >= 0");
                    table.CheckConstraint("ck_atproto_jetstream_lease_fence", "lease_fence >= 0");
                    table.CheckConstraint("ck_atproto_jetstream_lease_shape", "(lease_owner IS NULL AND lease_token IS NULL AND lease_expires_at IS NULL) OR (lease_owner IS NOT NULL AND btrim(lease_owner) <> '' AND lease_token IS NOT NULL AND lease_expires_at IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "atproto_outbound_record_ownerships",
                columns: table => new
                {
                    atproto_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_atproto_outbound_record_ownerships", x => x.atproto_record_id);
                    table.ForeignKey(
                        name: "fk_atproto_outbound_record_ownerships_atproto_records_atproto_",
                        column: x => x.atproto_record_id,
                        principalTable: "atproto_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_atproto_outbound_record_ownerships_tenant_users_tenant_id_u",
                        columns: x => new { x.tenant_id, x.user_id },
                        principalTable: "tenant_users",
                        principalColumns: new[] { "tenant_id", "user_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_atproto_outbound_record_ownerships_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_atproto_outbound_record_ownerships_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "atproto_record_tenant_presentations",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    atproto_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false),
                    source_version = table.Column<long>(type: "bigint", nullable: false),
                    evaluated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_atproto_record_tenant_presentations", x => new { x.tenant_id, x.atproto_record_id });
                    table.CheckConstraint("ck_atproto_record_tenant_presentations_source_version", "source_version >= 0");
                    table.ForeignKey(
                        name: "fk_atproto_record_tenant_presentations_atproto_records_atproto",
                        column: x => x.atproto_record_id,
                        principalTable: "atproto_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_atproto_record_tenant_presentations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "atproto_jetstream_quarantines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    consumer_state_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cursor = table.Column<long>(type: "bigint", nullable: false),
                    reason_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    envelope_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    record_identity_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    event_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    quarantined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_atproto_jetstream_quarantines", x => x.id);
                    table.CheckConstraint("ck_atproto_jetstream_quarantine_cursor", "cursor >= 0");
                    table.CheckConstraint("ck_atproto_jetstream_quarantine_envelope_hash", "envelope_hash ~ '^[0-9a-f]{64}$'");
                    table.CheckConstraint("ck_atproto_jetstream_quarantine_identity_hash", "record_identity_hash IS NULL OR record_identity_hash ~ '^[0-9a-f]{64}$'");
                    table.ForeignKey(
                        name: "fk_atproto_jetstream_quarantines_atproto_jetstream_consumer_st",
                        column: x => x.consumer_state_id,
                        principalTable: "atproto_jetstream_consumer_states",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pds_sync_outbox_atproto_record_id",
                table: "pds_sync_outbox",
                column: "atproto_record_id");

            migrationBuilder.CreateIndex(
                name: "ix_pds_sync_outbox_dependency",
                table: "pds_sync_outbox",
                column: "depends_on_atproto_record_id",
                filter: "depends_on_atproto_record_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_pds_sync_outbox_owner",
                table: "pds_sync_outbox",
                columns: new[] { "tenant_id", "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_pds_sync_outbox_record_identity",
                table: "pds_sync_outbox",
                columns: new[] { "did", "collection", "record_key" });

            migrationBuilder.CreateIndex(
                name: "ix_pds_sync_outbox_superseded_by_id",
                table: "pds_sync_outbox",
                column: "superseded_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_pds_sync_outbox_user_id",
                table: "pds_sync_outbox",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_pds_sync_outbox_worker_poll",
                table: "pds_sync_outbox",
                columns: new[] { "status", "next_retry_at", "lease_expires_at", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_pds_sync_outbox_idempotency",
                table: "pds_sync_outbox",
                columns: new[] { "tenant_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_pds_sync_outbox_source_version",
                table: "pds_sync_outbox",
                columns: new[] { "tenant_id", "source_entity_type", "source_entity_id", "source_version", "operation" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_pds_sync_outbox_completion_shape",
                table: "pds_sync_outbox",
                sql: "status <> 3 OR (processed_at IS NOT NULL AND settled_uri IS NOT NULL AND settled_cid IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pds_sync_outbox_lease_fence",
                table: "pds_sync_outbox",
                sql: "lease_fence >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pds_sync_outbox_lease_shape",
                table: "pds_sync_outbox",
                sql: "(status = 2 AND lease_owner IS NOT NULL AND btrim(lease_owner) <> '' AND lease_token IS NOT NULL AND lease_expires_at IS NOT NULL) OR (status <> 2 AND lease_owner IS NULL AND lease_token IS NULL AND lease_expires_at IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pds_sync_outbox_operation",
                table: "pds_sync_outbox",
                sql: "operation BETWEEN 1 AND 3");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pds_sync_outbox_payload_hash",
                table: "pds_sync_outbox",
                sql: "payload_hash ~ '^[0-9a-f]{64}$'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pds_sync_outbox_payload_shape",
                table: "pds_sync_outbox",
                sql: "(operation = 3 AND payload IS NULL) OR (operation IN (1, 2) AND payload IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pds_sync_outbox_retry_count",
                table: "pds_sync_outbox",
                sql: "retry_count >= 0 AND max_retries > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pds_sync_outbox_status",
                table: "pds_sync_outbox",
                sql: "status BETWEEN 1 AND 6");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pds_sync_outbox_supersession_shape",
                table: "pds_sync_outbox",
                sql: "(status = 6 AND superseded_by_id IS NOT NULL AND superseded_at IS NOT NULL) OR (status <> 6 AND superseded_by_id IS NULL AND superseded_at IS NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_atproto_records_subject_uri",
                table: "atproto_records",
                column: "subject_uri",
                filter: "subject_uri IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_atproto_records_uri",
                table: "atproto_records",
                column: "uri",
                unique: true,
                filter: "uri IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_atproto_records_direction",
                table: "atproto_records",
                sql: "direction BETWEEN 1 AND 3");

            migrationBuilder.AddCheckConstraint(
                name: "ck_atproto_records_provenance",
                table: "atproto_records",
                sql: "provenance BETWEEN 1 AND 3");

            migrationBuilder.AddCheckConstraint(
                name: "ck_atproto_records_record_hash",
                table: "atproto_records",
                sql: "record_hash IS NULL OR record_hash ~ '^[0-9a-f]{64}$'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_atproto_records_source_version",
                table: "atproto_records",
                sql: "source_version >= 0");

            migrationBuilder.CreateIndex(
                name: "ux_atproto_jetstream_consumer_service",
                table: "atproto_jetstream_consumer_states",
                column: "service",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_atproto_jetstream_quarantine_reason",
                table: "atproto_jetstream_quarantines",
                columns: new[] { "reason_code", "quarantined_at" });

            migrationBuilder.CreateIndex(
                name: "ux_atproto_jetstream_quarantine_cursor",
                table: "atproto_jetstream_quarantines",
                columns: new[] { "consumer_state_id", "cursor" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_atproto_outbound_ownership_user",
                table: "atproto_outbound_record_ownerships",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_atproto_outbound_record_ownerships_user_id",
                table: "atproto_outbound_record_ownerships",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_atproto_outbound_ownership_source",
                table: "atproto_outbound_record_ownerships",
                columns: new[] { "tenant_id", "source_entity_type", "source_entity_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_atproto_record_presentations_visible",
                table: "atproto_record_tenant_presentations",
                columns: new[] { "tenant_id", "is_visible", "evaluated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_atproto_record_tenant_presentations_atproto_record_id",
                table: "atproto_record_tenant_presentations",
                column: "atproto_record_id");

            migrationBuilder.AddForeignKey(
                name: "fk_pds_sync_outbox_atproto_records_atproto_record_id",
                table: "pds_sync_outbox",
                column: "atproto_record_id",
                principalTable: "atproto_records",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pds_sync_outbox_atproto_records_depends_on_atproto_record_id",
                table: "pds_sync_outbox",
                column: "depends_on_atproto_record_id",
                principalTable: "atproto_records",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pds_sync_outbox_pds_sync_outbox_superseded_by_id",
                table: "pds_sync_outbox",
                column: "superseded_by_id",
                principalTable: "pds_sync_outbox",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pds_sync_outbox_tenant_users_tenant_id_user_id",
                table: "pds_sync_outbox",
                columns: new[] { "tenant_id", "user_id" },
                principalTable: "tenant_users",
                principalColumns: new[] { "tenant_id", "user_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pds_sync_outbox_tenants_tenant_id",
                table: "pds_sync_outbox",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pds_sync_outbox_users_user_id",
                table: "pds_sync_outbox",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $migration$
                BEGIN
                    IF EXISTS (SELECT 1 FROM atproto_records)
                       OR EXISTS (SELECT 1 FROM pds_sync_outbox)
                       OR EXISTS (SELECT 1 FROM atproto_jetstream_consumer_states)
                       OR EXISTS (SELECT 1 FROM atproto_jetstream_quarantines)
                       OR EXISTS (SELECT 1 FROM atproto_outbound_record_ownerships)
                       OR EXISTS (SELECT 1 FROM atproto_record_tenant_presentations) THEN
                        RAISE EXCEPTION 'Cannot downgrade HardenAtprotoFederationPersistence while hardened federation state exists; remove it explicitly before downgrade.';
                    END IF;
                END
                $migration$;
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_pds_sync_outbox_atproto_records_atproto_record_id",
                table: "pds_sync_outbox");

            migrationBuilder.DropForeignKey(
                name: "fk_pds_sync_outbox_atproto_records_depends_on_atproto_record_id",
                table: "pds_sync_outbox");

            migrationBuilder.DropForeignKey(
                name: "fk_pds_sync_outbox_pds_sync_outbox_superseded_by_id",
                table: "pds_sync_outbox");

            migrationBuilder.DropForeignKey(
                name: "fk_pds_sync_outbox_tenant_users_tenant_id_user_id",
                table: "pds_sync_outbox");

            migrationBuilder.DropForeignKey(
                name: "fk_pds_sync_outbox_tenants_tenant_id",
                table: "pds_sync_outbox");

            migrationBuilder.DropForeignKey(
                name: "fk_pds_sync_outbox_users_user_id",
                table: "pds_sync_outbox");

            migrationBuilder.DropTable(
                name: "atproto_jetstream_quarantines");

            migrationBuilder.DropTable(
                name: "atproto_outbound_record_ownerships");

            migrationBuilder.DropTable(
                name: "atproto_record_tenant_presentations");

            migrationBuilder.DropTable(
                name: "atproto_jetstream_consumer_states");

            migrationBuilder.DropIndex(
                name: "ix_pds_sync_outbox_atproto_record_id",
                table: "pds_sync_outbox");

            migrationBuilder.DropIndex(
                name: "ix_pds_sync_outbox_dependency",
                table: "pds_sync_outbox");

            migrationBuilder.DropIndex(
                name: "ix_pds_sync_outbox_owner",
                table: "pds_sync_outbox");

            migrationBuilder.DropIndex(
                name: "ix_pds_sync_outbox_record_identity",
                table: "pds_sync_outbox");

            migrationBuilder.DropIndex(
                name: "ix_pds_sync_outbox_superseded_by_id",
                table: "pds_sync_outbox");

            migrationBuilder.DropIndex(
                name: "ix_pds_sync_outbox_user_id",
                table: "pds_sync_outbox");

            migrationBuilder.DropIndex(
                name: "ix_pds_sync_outbox_worker_poll",
                table: "pds_sync_outbox");

            migrationBuilder.DropIndex(
                name: "ux_pds_sync_outbox_idempotency",
                table: "pds_sync_outbox");

            migrationBuilder.DropIndex(
                name: "ux_pds_sync_outbox_source_version",
                table: "pds_sync_outbox");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pds_sync_outbox_completion_shape",
                table: "pds_sync_outbox");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pds_sync_outbox_lease_fence",
                table: "pds_sync_outbox");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pds_sync_outbox_lease_shape",
                table: "pds_sync_outbox");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pds_sync_outbox_operation",
                table: "pds_sync_outbox");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pds_sync_outbox_payload_hash",
                table: "pds_sync_outbox");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pds_sync_outbox_payload_shape",
                table: "pds_sync_outbox");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pds_sync_outbox_retry_count",
                table: "pds_sync_outbox");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pds_sync_outbox_status",
                table: "pds_sync_outbox");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pds_sync_outbox_supersession_shape",
                table: "pds_sync_outbox");

            migrationBuilder.DropIndex(
                name: "ix_atproto_records_subject_uri",
                table: "atproto_records");

            migrationBuilder.DropIndex(
                name: "ux_atproto_records_uri",
                table: "atproto_records");

            migrationBuilder.DropCheckConstraint(
                name: "ck_atproto_records_direction",
                table: "atproto_records");

            migrationBuilder.DropCheckConstraint(
                name: "ck_atproto_records_provenance",
                table: "atproto_records");

            migrationBuilder.DropCheckConstraint(
                name: "ck_atproto_records_record_hash",
                table: "atproto_records");

            migrationBuilder.DropCheckConstraint(
                name: "ck_atproto_records_source_version",
                table: "atproto_records");

            migrationBuilder.DropColumn(
                name: "atproto_record_id",
                table: "pds_sync_outbox");

            migrationBuilder.DropColumn(
                name: "depends_on_atproto_record_id",
                table: "pds_sync_outbox");

            migrationBuilder.DropColumn(
                name: "expected_cid",
                table: "pds_sync_outbox");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "pds_sync_outbox");

            migrationBuilder.DropColumn(
                name: "lease_expires_at",
                table: "pds_sync_outbox");

            migrationBuilder.DropColumn(
                name: "lease_fence",
                table: "pds_sync_outbox");

            migrationBuilder.DropColumn(
                name: "lease_owner",
                table: "pds_sync_outbox");

            migrationBuilder.DropColumn(
                name: "lease_token",
                table: "pds_sync_outbox");

            migrationBuilder.DropColumn(
                name: "payload_hash",
                table: "pds_sync_outbox");

            migrationBuilder.DropColumn(
                name: "settled_cid",
                table: "pds_sync_outbox");

            migrationBuilder.DropColumn(
                name: "settled_uri",
                table: "pds_sync_outbox");

            migrationBuilder.DropColumn(
                name: "source_version",
                table: "pds_sync_outbox");

            migrationBuilder.DropColumn(
                name: "superseded_at",
                table: "pds_sync_outbox");

            migrationBuilder.DropColumn(
                name: "superseded_by_id",
                table: "pds_sync_outbox");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "pds_sync_outbox");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "pds_sync_outbox");

            migrationBuilder.DropColumn(
                name: "direction",
                table: "atproto_records");

            migrationBuilder.DropColumn(
                name: "provenance",
                table: "atproto_records");

            migrationBuilder.DropColumn(
                name: "record_hash",
                table: "atproto_records");

            migrationBuilder.DropColumn(
                name: "record_json",
                table: "atproto_records");

            migrationBuilder.DropColumn(
                name: "source_cursor",
                table: "atproto_records");

            migrationBuilder.DropColumn(
                name: "source_version",
                table: "atproto_records");

            migrationBuilder.DropColumn(
                name: "subject_cid",
                table: "atproto_records");

            migrationBuilder.DropColumn(
                name: "subject_uri",
                table: "atproto_records");

            migrationBuilder.DropColumn(
                name: "tombstoned_at",
                table: "atproto_records");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "atproto_records");

            migrationBuilder.RenameIndex(
                name: "ux_atproto_records_identity",
                table: "atproto_records",
                newName: "ix_atproto_records_did_collection_record_key");

            migrationBuilder.AlterColumn<string>(
                name: "source_entity_type",
                table: "pds_sync_outbox",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<Guid>(
                name: "source_entity_id",
                table: "pds_sync_outbox",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "pds_host",
                table: "pds_sync_outbox",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "last_error",
                table: "pds_sync_outbox",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PdsSyncOutbox_Did",
                table: "pds_sync_outbox",
                column: "did");

            migrationBuilder.CreateIndex(
                name: "IX_PdsSyncOutbox_SourceEntity",
                table: "pds_sync_outbox",
                columns: new[] { "source_entity_type", "source_entity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_PdsSyncOutbox_Unique",
                table: "pds_sync_outbox",
                columns: new[] { "did", "collection", "record_key", "operation", "created_at" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PdsSyncOutbox_WorkerPoll",
                table: "pds_sync_outbox",
                columns: new[] { "status", "next_retry_at", "created_at" });
        }
    }
}
