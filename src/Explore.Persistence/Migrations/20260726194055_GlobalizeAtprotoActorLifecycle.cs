// ABOUTME: Globalizes Actors and concrete subjects while preserving tenant participation and exact-DID identity data.
// ABOUTME: Performs a guarded PostgreSQL expand-backfill-contract cutover and rejects lossy downgrade attempts.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GlobalizeAtprotoActorLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM actors actor
                        LEFT JOIN actor_pii pii ON pii.actor_id = actor.id
                        WHERE pii.actor_id IS NULL)
                    THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle requires one actor_pii row per actor.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM actor_pii
                        WHERE did IS NOT NULL AND btrim(did) = '')
                    THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle cannot migrate blank legacy DIDs.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM actors actor
                        JOIN actor_pii pii ON pii.actor_id = actor.id
                        WHERE pii.did IS NOT NULL
                          AND btrim(pii.did) <> ''
                          AND (actor.pds_host IS NULL OR btrim(actor.pds_host) = ''))
                    THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle cannot migrate a DID without its PDS host.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM actors actor
                        JOIN actor_pii pii ON pii.actor_id = actor.id
                        WHERE pii.did IS NULL
                          AND (pii.handle IS NOT NULL
                               OR actor.pds_host IS NOT NULL
                               OR actor.did_custody_type_id IS NOT NULL
                               OR actor.indexed_at IS NOT NULL))
                    THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle cannot preserve legacy AT Protocol metadata without a DID.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM actors left_actor
                        JOIN actor_pii left_pii ON left_pii.actor_id = left_actor.id
                        JOIN actors right_actor ON left_actor.id < right_actor.id
                        JOIN actor_pii right_pii ON right_pii.actor_id = right_actor.id
                        WHERE left_pii.did IS NOT NULL
                          AND btrim(left_pii.did) <> ''
                          AND left_pii.did COLLATE "C" = right_pii.did COLLATE "C"
                          AND (
                              left_actor.actor_type_id IS DISTINCT FROM right_actor.actor_type_id
                              OR left_actor.user_id IS DISTINCT FROM right_actor.user_id
                              OR left_actor.organization_id IS DISTINCT FROM right_actor.organization_id
                              OR left_actor.group_id IS DISTINCT FROM right_actor.group_id
                              OR left_actor.description IS DISTINCT FROM right_actor.description
                              OR left_actor.profile_picture_id IS DISTINCT FROM right_actor.profile_picture_id
                              OR left_actor.banner_picture_id IS DISTINCT FROM right_actor.banner_picture_id
                              OR left_actor.background_image_id IS DISTINCT FROM right_actor.background_image_id
                              OR left_actor.profile_picture_cid IS DISTINCT FROM right_actor.profile_picture_cid
                              OR left_actor.background_color IS DISTINCT FROM right_actor.background_color
                              OR left_actor.background_effect IS DISTINCT FROM right_actor.background_effect
                              OR left_actor.banner_color IS DISTINCT FROM right_actor.banner_color
                              OR left_actor.pds_host IS DISTINCT FROM right_actor.pds_host
                              OR left_actor.did_custody_type_id IS DISTINCT FROM right_actor.did_custody_type_id
                              OR left_pii.handle IS DISTINCT FROM right_pii.handle
                              OR left_pii.display_name IS DISTINCT FROM right_pii.display_name
                              OR left_pii.profile_picture_uri IS DISTINCT FROM right_pii.profile_picture_uri))
                    THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle found an exact duplicate DID with conflicting actor metadata or media.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM actors left_actor
                        JOIN actor_pii left_pii ON left_pii.actor_id = left_actor.id
                        JOIN actors right_actor ON left_actor.id < right_actor.id
                        JOIN actor_pii right_pii ON right_pii.actor_id = right_actor.id
                        WHERE left_actor.user_id IS NOT NULL
                          AND left_actor.user_id = right_actor.user_id
                          AND (
                              left_actor.actor_type_id IS DISTINCT FROM right_actor.actor_type_id
                              OR left_actor.organization_id IS DISTINCT FROM right_actor.organization_id
                              OR left_actor.group_id IS DISTINCT FROM right_actor.group_id
                              OR left_actor.description IS DISTINCT FROM right_actor.description
                              OR left_actor.profile_picture_id IS DISTINCT FROM right_actor.profile_picture_id
                              OR left_actor.banner_picture_id IS DISTINCT FROM right_actor.banner_picture_id
                              OR left_actor.background_image_id IS DISTINCT FROM right_actor.background_image_id
                              OR left_actor.profile_picture_cid IS DISTINCT FROM right_actor.profile_picture_cid
                              OR left_actor.background_color IS DISTINCT FROM right_actor.background_color
                              OR left_actor.background_effect IS DISTINCT FROM right_actor.background_effect
                              OR left_actor.banner_color IS DISTINCT FROM right_actor.banner_color
                              OR left_pii.display_name IS DISTINCT FROM right_pii.display_name
                              OR left_pii.profile_picture_uri IS DISTINCT FROM right_pii.profile_picture_uri))
                    THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle found tenant-local user actors with conflicting metadata or media.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM actors actor
                        LEFT JOIN storage_objects profile_picture ON profile_picture.id = actor.profile_picture_id
                        JOIN actor_pii pii ON pii.actor_id = actor.id
                        WHERE actor.profile_picture_id IS NOT NULL
                          AND (profile_picture.id IS NULL
                               OR length(profile_picture.uri) > 500
                               OR (pii.profile_picture_uri IS NOT NULL AND pii.profile_picture_uri IS DISTINCT FROM profile_picture.uri)))
                    THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle cannot preserve a legacy actor profile image without overwriting profile metadata.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM actors actor
                        WHERE actor.organization_id IS NULL
                          AND actor.group_id IS NULL
                          AND (actor.banner_picture_id IS NOT NULL OR actor.background_image_id IS NOT NULL))
                    THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle has legacy banner or background media without a final participation target.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM groups "group"
                        JOIN actors actor ON actor.id = "group".actor_id
                        WHERE "group".profile_picture_id IS NOT NULL
                          AND actor.profile_picture_id IS NOT NULL
                          AND "group".profile_picture_id IS DISTINCT FROM actor.profile_picture_id)
                    THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle found conflicting group and actor profile media.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM events
                        WHERE event_url IS NOT NULL
                          AND (btrim(event_url) = ''
                               OR event_url !~* '^https://[^/@?#]+(?:[/:?#]|$)'
                               OR event_url ~* '^https://[^/]+@'
                               OR event_url LIKE '%#%'))
                    THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle cannot convert a legacy event_url into a safe HTTPS public action.';
                    END IF;
                END
                $$;
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_actor_subscriptions_actors_tenant_id_target_actor_id",
                table: "actor_subscriptions");

            migrationBuilder.DropForeignKey(
                name: "fk_actors_did_custody_types_did_custody_type_id",
                table: "actors");

            migrationBuilder.DropForeignKey(
                name: "fk_actors_groups_group_id",
                table: "actors");

            migrationBuilder.DropForeignKey(
                name: "fk_actors_organizations_organization_id",
                table: "actors");

            migrationBuilder.DropForeignKey(
                name: "fk_actors_storage_objects_background_image_id",
                table: "actors");

            migrationBuilder.DropForeignKey(
                name: "fk_actors_storage_objects_banner_picture_id",
                table: "actors");

            migrationBuilder.DropForeignKey(
                name: "fk_actors_storage_objects_profile_picture_id",
                table: "actors");

            migrationBuilder.DropForeignKey(
                name: "fk_actors_tenants_tenant_id",
                table: "actors");

            migrationBuilder.DropForeignKey(
                name: "fk_actors_users_user_id",
                table: "actors");

            migrationBuilder.DropForeignKey(
                name: "fk_event_contact_share_consents_actors_tenant_id_recipient_act",
                table: "event_contact_share_consents");

            migrationBuilder.DropForeignKey(
                name: "fk_event_contact_share_exports_actors_tenant_id_recipient_acto",
                table: "event_contact_share_exports");

            migrationBuilder.DropForeignKey(
                name: "fk_event_reports_actors_tenant_id_reporter_actor_id",
                table: "event_reports");

            migrationBuilder.DropForeignKey(
                name: "fk_event_session_speakers_actors_tenant_id_actor_id",
                table: "event_session_speakers");

            migrationBuilder.DropForeignKey(
                name: "fk_events_actors_tenant_id_actor_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_group_members_groups_group_id",
                table: "group_members");

            migrationBuilder.DropForeignKey(
                name: "fk_group_setting_overrides_groups_group_id",
                table: "group_setting_overrides");

            migrationBuilder.DropForeignKey(
                name: "fk_groups_actors_actor_id",
                table: "groups");

            migrationBuilder.DropForeignKey(
                name: "fk_groups_approval_statuses_approval_status_id",
                table: "groups");

            migrationBuilder.DropForeignKey(
                name: "fk_groups_groups_tenant_id_parent_group_id",
                table: "groups");

            migrationBuilder.DropForeignKey(
                name: "fk_groups_organizations_tenant_id_parent_organization_id",
                table: "groups");

            migrationBuilder.DropForeignKey(
                name: "fk_groups_storage_objects_profile_picture_id",
                table: "groups");

            migrationBuilder.DropForeignKey(
                name: "fk_groups_tenants_tenant_id",
                table: "groups");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_fanout_runs_actors_tenant_id_source_actor_id",
                table: "notification_fanout_runs");

            migrationBuilder.DropForeignKey(
                name: "fk_organization_members_organizations_organization_id",
                table: "organization_members");

            migrationBuilder.DropForeignKey(
                name: "fk_organization_setting_overrides_organizations_organization_id",
                table: "organization_setting_overrides");

            migrationBuilder.DropForeignKey(
                name: "fk_organizations_actors_actor_id",
                table: "organizations");

            migrationBuilder.DropForeignKey(
                name: "fk_organizations_approval_statuses_approval_status_id",
                table: "organizations");

            migrationBuilder.DropForeignKey(
                name: "fk_organizations_tenants_tenant_id",
                table: "organizations");

            migrationBuilder.DropForeignKey(
                name: "fk_users_actors_actor_id",
                table: "users");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_consumers_groups_tenant_id_group_id",
                table: "webhook_consumers");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_consumers_organizations_tenant_id_organization_id",
                table: "webhook_consumers");

            migrationBuilder.DropIndex(
                name: "ix_users_actor_id",
                table: "users");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_organizations_tenant_id_id",
                table: "organizations");

            migrationBuilder.DropIndex(
                name: "ix_organizations_actor_id",
                table: "organizations");

            migrationBuilder.DropIndex(
                name: "ix_organizations_approval_status_id",
                table: "organizations");

            migrationBuilder.DropIndex(
                name: "ix_organizations_tenant",
                table: "organizations");

            migrationBuilder.DropIndex(
                name: "ix_organizations_tenant_active_status",
                table: "organizations");

            migrationBuilder.DropIndex(
                name: "ix_organization_setting_overrides_tenant_id",
                table: "organization_setting_overrides");

            migrationBuilder.DropIndex(
                name: "ix_organization_members_tenant_id",
                table: "organization_members");

            migrationBuilder.DropIndex(
                name: "ix_notification_fanout_runs_tenant_id_source_actor_id",
                table: "notification_fanout_runs");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_groups_tenant_id_id",
                table: "groups");

            migrationBuilder.DropIndex(
                name: "ix_groups_actor_id",
                table: "groups");

            migrationBuilder.DropIndex(
                name: "ix_groups_approval_status_id",
                table: "groups");

            migrationBuilder.DropIndex(
                name: "ix_groups_profile_picture_id",
                table: "groups");

            migrationBuilder.DropIndex(
                name: "ix_groups_tenant_active_status",
                table: "groups");

            migrationBuilder.DropIndex(
                name: "ix_groups_tenant_name",
                table: "groups");

            migrationBuilder.DropIndex(
                name: "ix_groups_tenant_parent_group",
                table: "groups");

            migrationBuilder.DropIndex(
                name: "ix_groups_tenant_parent_organization",
                table: "groups");

            migrationBuilder.DropCheckConstraint(
                name: "ck_groups_no_self_parent",
                table: "groups");

            migrationBuilder.DropCheckConstraint(
                name: "ck_groups_parent_exclusive",
                table: "groups");

            migrationBuilder.DropIndex(
                name: "ix_group_setting_overrides_tenant_id",
                table: "group_setting_overrides");

            migrationBuilder.DropIndex(
                name: "ix_event_session_speakers_tenant_id_actor_id",
                table: "event_session_speakers");

            migrationBuilder.DropIndex(
                name: "ix_event_reports_tenant_id_reporter_actor_id",
                table: "event_reports");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_actors_tenant_id_id",
                table: "actors");

            migrationBuilder.DropIndex(
                name: "ix_actors_background_image_id",
                table: "actors");

            migrationBuilder.DropIndex(
                name: "ix_actors_banner_picture_id",
                table: "actors");

            migrationBuilder.DropIndex(
                name: "ix_actors_did_custody_type_id",
                table: "actors");

            migrationBuilder.DropIndex(
                name: "ix_actors_profile_picture_id",
                table: "actors");

            migrationBuilder.DropIndex(
                name: "ix_actors_user_id_tenant_id",
                table: "actors");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Actor_UserOrOrganization",
                table: "actors");

            migrationBuilder.DropIndex(
                name: "ix_actor_pii_did",
                table: "actor_pii");

            migrationBuilder.DropIndex(
                name: "ix_actor_pii_handle",
                table: "actor_pii");

            migrationBuilder.AddColumn<Guid>(
                name: "external_actor_subject_id",
                table: "actors",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "service_principal_id",
                table: "actors",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "suspended_at",
                table: "actors",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "suspended_by",
                table: "actors",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "event_provenance_type_id",
                table: "events",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "organizer_actor_id",
                table: "events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_publisher_name",
                table: "events",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "submitted_by_user_id",
                table: "events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_suspended",
                table: "actors",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "moderation_reason_code",
                table: "actors",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "actor_merges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    canonical_actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proof_kind = table.Column<int>(type: "integer", nullable: false),
                    evidence_reference = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    merged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    merged_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_actor_merges", x => x.id);
                    table.CheckConstraint("ck_actor_merges_distinct_actors", "source_actor_id <> canonical_actor_id");
                    table.ForeignKey(
                        name: "fk_actor_merges_actors_canonical_actor_id",
                        column: x => x.canonical_actor_id,
                        principalTable: "actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_actor_merges_actors_source_actor_id",
                        column: x => x.source_actor_id,
                        principalTable: "actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "actor_moderation_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<int>(type: "integer", nullable: false),
                    reason_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_actor_moderation_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_actor_moderation_records_actors_actor_id",
                        column: x => x.actor_id,
                        principalTable: "actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "atproto_identities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    did = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false, collation: "C"),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    did_custody_type_id = table.Column<int>(type: "integer", nullable: true),
                    handle = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: true),
                    pds_host = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    signing_key = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_suspended = table.Column<bool>(type: "boolean", nullable: false),
                    suspended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    suspended_by = table.Column<Guid>(type: "uuid", nullable: true),
                    moderation_reason_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    last_resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_atproto_identities", x => x.id);
                    table.ForeignKey(
                        name: "fk_atproto_identities_actors_actor_id",
                        column: x => x.actor_id,
                        principalTable: "actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_atproto_identities_did_custody_types_did_custody_type_id",
                        column: x => x.did_custody_type_id,
                        principalTable: "did_custody_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_organizer_claim_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_organizer_claim_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_provenance_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_provenance_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_public_action_health_states",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_public_action_health_states", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_public_action_kinds",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_public_action_kinds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "external_actor_subjects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_observed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_observed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_external_actor_subjects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organization_tenants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approval_status_id = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false),
                    is_organizer_eligible = table.Column<bool>(type: "boolean", nullable: false),
                    is_suspended = table.Column<bool>(type: "boolean", nullable: false),
                    suspended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    suspended_by = table.Column<Guid>(type: "uuid", nullable: true),
                    moderation_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    display_name_override = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description_override = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    website_url_override = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    contact_email_override = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    profile_picture_id = table.Column<Guid>(type: "uuid", nullable: true),
                    banner_picture_id = table.Column<Guid>(type: "uuid", nullable: true),
                    background_image_id = table.Column<Guid>(type: "uuid", nullable: true),
                    background_color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    background_effect = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    banner_color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approval_notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_tenants", x => x.id);
                    table.UniqueConstraint("ak_organization_tenants_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_organization_tenants_approval_statuses_approval_status_id",
                        column: x => x.approval_status_id,
                        principalTable: "approval_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_organization_tenants_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_organization_tenants_storage_objects_background_image_id",
                        column: x => x.background_image_id,
                        principalTable: "storage_objects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_organization_tenants_storage_objects_banner_picture_id",
                        column: x => x.banner_picture_id,
                        principalTable: "storage_objects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_organization_tenants_storage_objects_profile_picture_id",
                        column: x => x.profile_picture_id,
                        principalTable: "storage_objects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_organization_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_principals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    display_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_principals", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "atproto_identity_moderation_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    atproto_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<int>(type: "integer", nullable: false),
                    reason_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_atproto_identity_moderation_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_atproto_identity_moderation_records_atproto_identities_atpr",
                        column: x => x.atproto_identity_id,
                        principalTable: "atproto_identities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_organizer_claims",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claimant_actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    evidence_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    evidence_reference = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    reviewer_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decision_reason_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    decided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_event_organizer_claims", x => x.id);
                    table.UniqueConstraint("ak_event_organizer_claims_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_event_organizer_claims_actors_claimant_actor_id",
                        column: x => x.claimant_actor_id,
                        principalTable: "actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_organizer_claims_event_organizer_claim_statuses_statu",
                        column: x => x.status_id,
                        principalTable: "event_organizer_claim_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_organizer_claims_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_organizer_claims_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_organizer_claims_users_reviewer_user_id",
                        column: x => x.reviewer_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_public_actions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_public_action_kind_id = table.Column<int>(type: "integer", nullable: false),
                    health_state_id = table.Column<int>(type: "integer", nullable: false),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    destination_domain = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_event_public_actions", x => x.id);
                    table.UniqueConstraint("ak_event_public_actions_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_event_public_actions_event_public_action_health_states_heal",
                        column: x => x.health_state_id,
                        principalTable: "event_public_action_health_states",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_public_actions_event_public_action_kinds_event_public",
                        column: x => x.event_public_action_kind_id,
                        principalTable: "event_public_action_kinds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_public_actions_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_public_actions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "group_tenants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approval_status_id = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false),
                    is_organizer_eligible = table.Column<bool>(type: "boolean", nullable: false),
                    is_suspended = table.Column<bool>(type: "boolean", nullable: false),
                    suspended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    suspended_by = table.Column<Guid>(type: "uuid", nullable: true),
                    moderation_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    display_name_override = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description_override = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    profile_picture_id = table.Column<Guid>(type: "uuid", nullable: true),
                    banner_picture_id = table.Column<Guid>(type: "uuid", nullable: true),
                    background_image_id = table.Column<Guid>(type: "uuid", nullable: true),
                    background_color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    background_effect = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    banner_color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    parent_organization_tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parent_group_tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approval_notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_tenants", x => x.id);
                    table.UniqueConstraint("ak_group_tenants_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_group_tenants_no_self_parent", "parent_group_tenant_id IS NULL OR parent_group_tenant_id <> id");
                    table.CheckConstraint("ck_group_tenants_parent_exclusive", "parent_organization_tenant_id IS NULL OR parent_group_tenant_id IS NULL");
                    table.ForeignKey(
                        name: "fk_group_tenants_approval_statuses_approval_status_id",
                        column: x => x.approval_status_id,
                        principalTable: "approval_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_group_tenants_group_tenants_tenant_id_parent_group_tenant_id",
                        columns: x => new { x.tenant_id, x.parent_group_tenant_id },
                        principalTable: "group_tenants",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_group_tenants_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_group_tenants_organization_tenants_tenant_id_parent_organiz",
                        columns: x => new { x.tenant_id, x.parent_organization_tenant_id },
                        principalTable: "organization_tenants",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_group_tenants_storage_objects_background_image_id",
                        column: x => x.background_image_id,
                        principalTable: "storage_objects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_group_tenants_storage_objects_banner_picture_id",
                        column: x => x.banner_picture_id,
                        principalTable: "storage_objects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_group_tenants_storage_objects_profile_picture_id",
                        column: x => x.profile_picture_id,
                        principalTable: "storage_objects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_group_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "event_organizer_claim_statuses",
                columns: new[] { "id", "description", "full_name", "master_code" },
                values: new object[,]
                {
                    { 1, "Claim is awaiting review", "Pending", "PENDING" },
                    { 2, "Reviewer requested additional evidence", "Evidence required", "EVIDENCE_REQUIRED" },
                    { 3, "Claim grants future organizer authority", "Approved", "APPROVED" },
                    { 4, "Claim was rejected", "Rejected", "REJECTED" },
                    { 5, "Claimant withdrew the claim", "Withdrawn", "WITHDRAWN" },
                    { 6, "Claim expired before approval", "Expired", "EXPIRED" }
                });

            migrationBuilder.InsertData(
                table: "event_provenance_types",
                columns: new[] { "id", "description", "full_name", "master_code" },
                values: new object[,]
                {
                    { 1, "Created by an actor with organizer authority", "Organizer created", "ORGANIZER_CREATED" },
                    { 2, "Submitted by a community member for listing review", "Community reported", "COMMUNITY_REPORTED" },
                    { 3, "Curated by the tenant without organizer authority", "Tenant curated", "TENANT_CURATED" },
                    { 4, "Imported from an external source", "Imported", "IMPORTED" },
                    { 5, "Materialized from a federated source", "Federated", "FEDERATED" }
                });

            migrationBuilder.InsertData(
                table: "event_public_action_health_states",
                columns: new[] { "id", "description", "full_name", "master_code" },
                values: new object[,]
                {
                    { 1, "Action is awaiting moderation review", "Pending review", "PENDING_REVIEW" },
                    { 2, "Action is approved and available", "Active", "ACTIVE" },
                    { 3, "Action destination is unavailable", "Broken", "BROKEN" },
                    { 4, "Action destination failed safety review", "Unsafe", "UNSAFE" },
                    { 5, "Action is intentionally disabled", "Disabled", "DISABLED" },
                    { 6, "Action destination is no longer current", "Expired", "EXPIRED" }
                });

            migrationBuilder.InsertData(
                table: "event_public_action_kinds",
                columns: new[] { "id", "description", "full_name", "master_code" },
                values: new object[,]
                {
                    { 1, "Canonical source for the event listing", "Original source", "ORIGINAL_SOURCE" },
                    { 2, "External page containing event information", "External event page", "EXTERNAL_EVENT_PAGE" },
                    { 3, "External registration destination", "External registration", "EXTERNAL_REGISTRATION" },
                    { 4, "Optional external questionnaire", "Optional questionnaire", "OPTIONAL_QUESTIONNAIRE" },
                    { 5, "External livestream destination", "Livestream", "LIVESTREAM" },
                    { 6, "Organizer-controlled contact destination", "Organizer contact", "ORGANIZER_CONTACT" }
                });

            migrationBuilder.Sql("""
                INSERT INTO did_custody_types (id, master_code, full_name, description)
                VALUES
                    (1, 'CUSTODIAL', 'Custodial', 'Platform manages the DID keys'),
                    (2, 'SELF_CUSTODY', 'Self-Custody', 'User manages their own DID keys')
                ON CONFLICT DO NOTHING;

                CREATE TEMP TABLE __global_actor_map ON COMMIT DROP AS
                WITH did_ranked AS (
                    SELECT actor.id AS source_actor_id,
                           first_value(actor.id) OVER (
                               PARTITION BY pii.did COLLATE "C"
                               ORDER BY actor.id) AS canonical_actor_id
                    FROM actors actor
                    JOIN actor_pii pii ON pii.actor_id = actor.id
                    WHERE pii.did IS NOT NULL AND btrim(pii.did) <> ''
                ),
                user_ranked AS (
                    SELECT actor.id AS source_actor_id,
                           first_value(actor.id) OVER (
                               PARTITION BY actor.user_id
                               ORDER BY actor.id) AS canonical_actor_id
                    FROM actors actor
                    WHERE actor.user_id IS NOT NULL
                )
                SELECT actor.id AS source_actor_id,
                       COALESCE(user_ranked.canonical_actor_id, did_ranked.canonical_actor_id) AS canonical_actor_id,
                       CASE
                           WHEN user_ranked.canonical_actor_id IS NOT NULL THEN 2
                           ELSE 1
                       END AS proof_kind
                FROM actors actor
                LEFT JOIN did_ranked ON did_ranked.source_actor_id = actor.id
                LEFT JOIN user_ranked ON user_ranked.source_actor_id = actor.id
                WHERE COALESCE(user_ranked.canonical_actor_id, did_ranked.canonical_actor_id) IS NOT NULL
                  AND actor.id IS DISTINCT FROM COALESCE(user_ranked.canonical_actor_id, did_ranked.canonical_actor_id);

                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM users "user"
                        LEFT JOIN __global_actor_map actor_map ON actor_map.source_actor_id = "user".actor_id
                        LEFT JOIN __global_actor_map default_actor_map ON default_actor_map.source_actor_id = "user".default_actor_id
                        WHERE "user".actor_id IS NOT NULL
                          AND "user".default_actor_id IS NOT NULL
                          AND COALESCE(actor_map.canonical_actor_id, "user".actor_id)
                              IS DISTINCT FROM COALESCE(default_actor_map.canonical_actor_id, "user".default_actor_id))
                    THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle found a user with incompatible actor and default actor references.';
                    END IF;

                    IF EXISTS (
                        WITH desired_user_owner AS (
                            SELECT "user".id AS user_id,
                                   COALESCE(actor_map.canonical_actor_id, default_actor_map.canonical_actor_id, "user".actor_id, "user".default_actor_id) AS actor_id
                            FROM users "user"
                            LEFT JOIN __global_actor_map actor_map ON actor_map.source_actor_id = "user".actor_id
                            LEFT JOIN __global_actor_map default_actor_map ON default_actor_map.source_actor_id = "user".default_actor_id
                            WHERE COALESCE(actor_map.canonical_actor_id, default_actor_map.canonical_actor_id, "user".actor_id, "user".default_actor_id) IS NOT NULL)
                        SELECT 1
                        FROM desired_user_owner desired
                        JOIN actors actor ON actor.id = desired.actor_id
                        WHERE (actor.user_id IS NOT NULL AND actor.user_id IS DISTINCT FROM desired.user_id)
                           OR actor.organization_id IS NOT NULL
                           OR actor.group_id IS NOT NULL)
                    THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle cannot preserve a user actor reference with conflicting ownership.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM organizations organization
                        LEFT JOIN __global_actor_map actor_map ON actor_map.source_actor_id = organization.actor_id
                        JOIN actors actor ON actor.id = COALESCE(actor_map.canonical_actor_id, organization.actor_id)
                        WHERE organization.actor_id IS NOT NULL
                          AND ((actor.organization_id IS NOT NULL AND actor.organization_id IS DISTINCT FROM organization.id)
                               OR actor.user_id IS NOT NULL
                               OR actor.group_id IS NOT NULL))
                    THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle cannot preserve an organization actor reference with conflicting ownership.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM groups "group"
                        LEFT JOIN __global_actor_map actor_map ON actor_map.source_actor_id = "group".actor_id
                        JOIN actors actor ON actor.id = COALESCE(actor_map.canonical_actor_id, "group".actor_id)
                        WHERE "group".actor_id IS NOT NULL
                          AND ((actor.group_id IS NOT NULL AND actor.group_id IS DISTINCT FROM "group".id)
                               OR actor.user_id IS NOT NULL
                               OR actor.organization_id IS NOT NULL))
                    THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle cannot preserve a group actor reference with conflicting ownership.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM actor_subscriptions subscription
                        LEFT JOIN __global_actor_map actor_map ON actor_map.source_actor_id = subscription.target_actor_id
                        WHERE NOT subscription.is_deleted
                        GROUP BY subscription.tenant_id, subscription.subscriber_tenant_user_id,
                                 COALESCE(actor_map.canonical_actor_id, subscription.target_actor_id)
                        HAVING count(*) > 1)
                    THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle would collapse active actor subscriptions.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM event_contact_share_consents consent
                        LEFT JOIN __global_actor_map actor_map ON actor_map.source_actor_id = consent.recipient_actor_id
                        GROUP BY consent.tenant_id, consent.user_id,
                                 COALESCE(actor_map.canonical_actor_id, consent.recipient_actor_id), consent.purpose_code
                        HAVING count(*) > 1)
                    THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle would collapse contact-share consent evidence.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM event_session_speakers speaker
                        LEFT JOIN __global_actor_map actor_map ON actor_map.source_actor_id = speaker.actor_id
                        GROUP BY speaker.tenant_id, speaker.event_session_id,
                                 COALESCE(actor_map.canonical_actor_id, speaker.actor_id)
                        HAVING count(*) > 1)
                    THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle would collapse event-session speakers.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM notification_fanout_runs fanout
                        LEFT JOIN __global_actor_map actor_map ON actor_map.source_actor_id = fanout.source_actor_id
                        WHERE fanout.fanout_occurrence_id IS NULL
                        GROUP BY fanout.tenant_id, fanout.fanout_kind, fanout.notification_entity_type_id,
                                 fanout.entity_id, COALESCE(actor_map.canonical_actor_id, fanout.source_actor_id)
                        HAVING count(*) > 1)
                    THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle would collapse notification fanout history.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM tenant_users tenant_user
                        LEFT JOIN __global_actor_map actor_map ON actor_map.source_actor_id = tenant_user.actor_id
                        WHERE tenant_user.actor_id IS NOT NULL
                        GROUP BY tenant_user.tenant_id, COALESCE(actor_map.canonical_actor_id, tenant_user.actor_id)
                        HAVING count(*) > 1)
                    THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle would collapse tenant-user actor assignments.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM __global_actor_map actor_map
                        JOIN actors source_actor ON source_actor.id = actor_map.source_actor_id
                        JOIN actors canonical_actor ON canonical_actor.id = actor_map.canonical_actor_id
                        WHERE COALESCE(source_actor.user_id, canonical_actor.user_id, source_actor.updated_by,
                                       source_actor.created_by, canonical_actor.updated_by, canonical_actor.created_by) IS NULL)
                    THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle requires migration provenance before recording an actor merge.';
                    END IF;
                END
                $$;

                UPDATE actor_key_stores target
                SET actor_id = actor_map.canonical_actor_id
                FROM __global_actor_map actor_map
                WHERE target.actor_id = actor_map.source_actor_id;

                UPDATE actor_subscriptions target
                SET target_actor_id = actor_map.canonical_actor_id
                FROM __global_actor_map actor_map
                WHERE target.target_actor_id = actor_map.source_actor_id;

                UPDATE event_series target
                SET actor_id = actor_map.canonical_actor_id
                FROM __global_actor_map actor_map
                WHERE target.actor_id = actor_map.source_actor_id;

                UPDATE ai_conversations target
                SET actor_id = actor_map.canonical_actor_id
                FROM __global_actor_map actor_map
                WHERE target.actor_id = actor_map.source_actor_id;

                UPDATE tenant_users target
                SET actor_id = actor_map.canonical_actor_id
                FROM __global_actor_map actor_map
                WHERE target.actor_id = actor_map.source_actor_id;

                UPDATE events target
                SET actor_id = actor_map.canonical_actor_id
                FROM __global_actor_map actor_map
                WHERE target.actor_id = actor_map.source_actor_id;

                UPDATE event_contact_share_exports target
                SET recipient_actor_id = actor_map.canonical_actor_id
                FROM __global_actor_map actor_map
                WHERE target.recipient_actor_id = actor_map.source_actor_id;

                UPDATE event_contact_share_consents target
                SET recipient_actor_id = actor_map.canonical_actor_id
                FROM __global_actor_map actor_map
                WHERE target.recipient_actor_id = actor_map.source_actor_id;

                UPDATE event_reports target
                SET reporter_actor_id = actor_map.canonical_actor_id
                FROM __global_actor_map actor_map
                WHERE target.reporter_actor_id = actor_map.source_actor_id;

                UPDATE event_session_speakers target
                SET actor_id = actor_map.canonical_actor_id
                FROM __global_actor_map actor_map
                WHERE target.actor_id = actor_map.source_actor_id;

                UPDATE notification_fanout_runs target
                SET source_actor_id = actor_map.canonical_actor_id
                FROM __global_actor_map actor_map
                WHERE target.source_actor_id = actor_map.source_actor_id;

                UPDATE notifications target
                SET source_actor_id = actor_map.canonical_actor_id
                FROM __global_actor_map actor_map
                WHERE target.source_actor_id = actor_map.source_actor_id;

                UPDATE notifications target
                SET recipient_context_actor_id = actor_map.canonical_actor_id
                FROM __global_actor_map actor_map
                WHERE target.recipient_context_actor_id = actor_map.source_actor_id;

                UPDATE ai_proposed_actions target
                SET acting_actor_id = actor_map.canonical_actor_id
                FROM __global_actor_map actor_map
                WHERE target.acting_actor_id = actor_map.source_actor_id;

                UPDATE storage_objects target
                SET actor_id = actor_map.canonical_actor_id
                FROM __global_actor_map actor_map
                WHERE target.actor_id = actor_map.source_actor_id;

                UPDATE users target
                SET actor_id = actor_map.canonical_actor_id
                FROM __global_actor_map actor_map
                WHERE target.actor_id = actor_map.source_actor_id;

                UPDATE users target
                SET default_actor_id = actor_map.canonical_actor_id
                FROM __global_actor_map actor_map
                WHERE target.default_actor_id = actor_map.source_actor_id;

                UPDATE organizations target
                SET actor_id = actor_map.canonical_actor_id
                FROM __global_actor_map actor_map
                WHERE target.actor_id = actor_map.source_actor_id;

                UPDATE groups target
                SET actor_id = actor_map.canonical_actor_id
                FROM __global_actor_map actor_map
                WHERE target.actor_id = actor_map.source_actor_id;

                UPDATE actors actor
                SET user_id = "user".id
                FROM users "user"
                WHERE actor.id = COALESCE("user".actor_id, "user".default_actor_id)
                  AND actor.user_id IS NULL;

                UPDATE actors actor
                SET organization_id = organization.id
                FROM organizations organization
                WHERE actor.id = organization.actor_id
                  AND actor.organization_id IS NULL;

                UPDATE actors actor
                SET group_id = "group".id
                FROM groups "group"
                WHERE actor.id = "group".actor_id
                  AND actor.group_id IS NULL;

                INSERT INTO actor_merges (
                    id, source_actor_id, canonical_actor_id, proof_kind, evidence_reference, merged_at, merged_by)
                SELECT md5('actor-merge:' || actor_map.source_actor_id::text)::uuid,
                       actor_map.source_actor_id,
                       actor_map.canonical_actor_id,
                       actor_map.proof_kind,
                       CASE actor_map.proof_kind
                           WHEN 1 THEN 'migration:verified-did'
                           ELSE 'migration:existing-user-ownership'
                       END,
                       statement_timestamp(),
                       COALESCE(source_actor.user_id, canonical_actor.user_id, source_actor.updated_by,
                                source_actor.created_by, canonical_actor.updated_by, canonical_actor.created_by)
                FROM __global_actor_map actor_map
                JOIN actors source_actor ON source_actor.id = actor_map.source_actor_id
                JOIN actors canonical_actor ON canonical_actor.id = actor_map.canonical_actor_id;

                UPDATE actors actor
                SET user_id = NULL,
                    organization_id = NULL,
                    group_id = NULL
                FROM __global_actor_map actor_map
                WHERE actor.id = actor_map.source_actor_id;

                INSERT INTO external_actor_subjects (
                    id, first_observed_at, last_observed_at, created_at, created_by, updated_at, updated_by,
                    is_deleted, deleted_at, deleted_by, concurrency_stamp)
                SELECT actor.id,
                       COALESCE(actor.indexed_at, actor.created_at),
                       COALESCE(actor.indexed_at, actor.created_at),
                       actor.created_at,
                       actor.created_by,
                       actor.updated_at,
                       actor.updated_by,
                       actor.is_deleted,
                       actor.deleted_at,
                       actor.deleted_by,
                       actor.concurrency_stamp
                FROM actors actor
                WHERE actor.user_id IS NULL
                  AND actor.organization_id IS NULL
                  AND actor.group_id IS NULL;

                UPDATE actors actor
                SET external_actor_subject_id = actor.id
                WHERE actor.user_id IS NULL
                  AND actor.organization_id IS NULL
                  AND actor.group_id IS NULL;

                UPDATE actor_pii pii
                SET profile_picture_uri = storage_object.uri
                FROM actors actor
                JOIN storage_objects storage_object ON storage_object.id = actor.profile_picture_id
                WHERE pii.actor_id = actor.id
                  AND pii.profile_picture_uri IS NULL
                  AND actor.profile_picture_id IS NOT NULL;

                INSERT INTO atproto_identities (
                    id, did, actor_id, did_custody_type_id, handle, pds_host, signing_key, is_active,
                    is_suspended, suspended_at, suspended_by, moderation_reason_code, last_resolved_at,
                    last_seen_at, created_at, created_by, updated_at, updated_by, is_deleted, deleted_at,
                    deleted_by, concurrency_stamp)
                SELECT DISTINCT ON (pii.did COLLATE "C")
                       md5('atproto-identity:' || pii.did)::uuid,
                       pii.did,
                       COALESCE(actor_map.canonical_actor_id, actor.id),
                       actor.did_custody_type_id,
                       pii.handle,
                       actor.pds_host,
                       NULL,
                       NOT actor.is_deleted,
                       FALSE,
                       NULL,
                       NULL,
                       NULL,
                       COALESCE(actor.indexed_at, actor.created_at),
                       actor.indexed_at,
                       actor.created_at,
                       actor.created_by,
                       actor.updated_at,
                       actor.updated_by,
                       actor.is_deleted,
                       actor.deleted_at,
                       actor.deleted_by,
                       actor.concurrency_stamp
                FROM actors actor
                JOIN actor_pii pii ON pii.actor_id = actor.id
                LEFT JOIN __global_actor_map actor_map ON actor_map.source_actor_id = actor.id
                WHERE pii.did IS NOT NULL AND btrim(pii.did) <> ''
                ORDER BY pii.did COLLATE "C", actor.id;

                INSERT INTO organization_tenants (
                    id, tenant_id, organization_id, approval_status_id, is_visible, is_organizer_eligible,
                    is_suspended, suspended_at, suspended_by, moderation_note, display_name_override,
                    description_override, website_url_override, contact_email_override, profile_picture_id,
                    banner_picture_id, background_image_id, background_color, background_effect, banner_color,
                    approved_at, approved_by, approval_notes, created_at, created_by, updated_at, updated_by,
                    is_deleted, deleted_at, deleted_by, concurrency_stamp)
                SELECT organization.id,
                       organization.tenant_id,
                       organization.id,
                       organization.approval_status_id,
                       NOT organization.is_deleted AND organization.approval_status_id = 2,
                       NOT organization.is_deleted AND organization.approval_status_id = 2,
                       FALSE,
                       NULL,
                       NULL,
                       NULL,
                       NULL,
                       NULL,
                       NULL,
                       NULL,
                       actor.profile_picture_id,
                       actor.banner_picture_id,
                       actor.background_image_id,
                       actor.background_color,
                       actor.background_effect,
                       actor.banner_color,
                       organization.approved_at,
                       organization.approved_by,
                       organization.approval_notes,
                       organization.created_at,
                       organization.created_by,
                       organization.updated_at,
                       organization.updated_by,
                       organization.is_deleted,
                       organization.deleted_at,
                       organization.deleted_by,
                       organization.concurrency_stamp
                FROM organizations organization
                LEFT JOIN actors actor ON actor.id = organization.actor_id;

                INSERT INTO group_tenants (
                    id, tenant_id, group_id, approval_status_id, is_visible, is_organizer_eligible, is_suspended,
                    suspended_at, suspended_by, moderation_note, display_name_override, description_override,
                    profile_picture_id, banner_picture_id, background_image_id, background_color, background_effect,
                    banner_color, parent_organization_tenant_id, parent_group_tenant_id, approved_at, approved_by,
                    approval_notes, created_at, created_by, updated_at, updated_by, is_deleted, deleted_at,
                    deleted_by, concurrency_stamp)
                SELECT "group".id,
                       "group".tenant_id,
                       "group".id,
                       "group".approval_status_id,
                       NOT "group".is_deleted AND "group".approval_status_id = 2,
                       NOT "group".is_deleted AND "group".approval_status_id = 2,
                       FALSE,
                       NULL,
                       NULL,
                       NULL,
                       NULL,
                       NULL,
                       COALESCE("group".profile_picture_id, actor.profile_picture_id),
                       actor.banner_picture_id,
                       actor.background_image_id,
                       actor.background_color,
                       actor.background_effect,
                       actor.banner_color,
                       "group".parent_organization_id,
                       "group".parent_group_id,
                       NULL,
                       NULL,
                       NULL,
                       "group".created_at,
                       "group".created_by,
                       "group".updated_at,
                       "group".updated_by,
                       "group".is_deleted,
                       "group".deleted_at,
                       "group".deleted_by,
                       "group".concurrency_stamp
                FROM groups "group"
                LEFT JOIN actors actor ON actor.id = "group".actor_id;

                UPDATE events event
                SET event_provenance_type_id = CASE
                        WHEN event.atproto_record_id IS NOT NULL THEN 5
                        WHEN event.is_user_reported THEN 2
                        WHEN event.event_url IS NOT NULL THEN 4
                        ELSE 1
                    END,
                    submitted_by_user_id = CASE WHEN event.is_user_reported THEN actor.user_id ELSE NULL END,
                    organizer_actor_id = CASE
                        WHEN NOT event.is_user_reported AND event.event_url IS NULL THEN event.actor_id
                        ELSE NULL
                    END
                FROM actors actor
                WHERE actor.id = event.actor_id;

                INSERT INTO event_public_actions (
                    id, tenant_id, event_id, event_public_action_kind_id, health_state_id, url,
                    destination_domain, label, sort_order, is_primary, created_at, created_by, updated_at,
                    updated_by, is_deleted, deleted_at, deleted_by, concurrency_stamp)
                SELECT md5('event-public-action:' || event.id::text)::uuid,
                       event.tenant_id,
                       event.id,
                       1,
                       2,
                       event.event_url,
                       lower((regexp_match(event.event_url, '^https://([^/:?#]+)', 'i'))[1]),
                       NULL,
                       0,
                       TRUE,
                       event.created_at,
                       event.created_by,
                       event.updated_at,
                       event.updated_by,
                       event.is_deleted,
                       event.deleted_at,
                       event.deleted_by,
                       event.concurrency_stamp
                FROM events event
                WHERE event.event_url IS NOT NULL;

                DO $$
                BEGIN
                    IF (SELECT count(*) FROM organization_tenants) <> (SELECT count(*) FROM organizations) THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle organization participation count mismatch.';
                    END IF;

                    IF (SELECT count(*) FROM group_tenants) <> (SELECT count(*) FROM groups) THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle group participation count mismatch.';
                    END IF;

                    IF (SELECT count(*) FROM event_public_actions) <> (SELECT count(*) FROM events WHERE event_url IS NOT NULL) THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle event public-action count mismatch.';
                    END IF;

                    IF (SELECT count(*) FROM atproto_identities) <> (
                        SELECT count(*)
                        FROM (SELECT DISTINCT did COLLATE "C" FROM actor_pii WHERE did IS NOT NULL AND btrim(did) <> '') dids) THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle AT Protocol identity count mismatch.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM organization_members member
                        LEFT JOIN organization_tenants participation
                            ON participation.tenant_id = member.tenant_id
                           AND participation.id = member.organization_id
                        WHERE participation.id IS NULL)
                    THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle found an orphan organization member.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM organization_setting_overrides setting
                        LEFT JOIN organization_tenants participation
                            ON participation.tenant_id = setting.tenant_id
                           AND participation.id = setting.organization_id
                        WHERE participation.id IS NULL)
                    THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle found an orphan organization setting.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM group_members member
                        LEFT JOIN group_tenants participation
                            ON participation.tenant_id = member.tenant_id
                           AND participation.id = member.group_id
                        WHERE participation.id IS NULL)
                    THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle found an orphan group member.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM group_setting_overrides setting
                        LEFT JOIN group_tenants participation
                            ON participation.tenant_id = setting.tenant_id
                           AND participation.id = setting.group_id
                        WHERE participation.id IS NULL)
                    THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle found an orphan group setting.';
                    END IF;

                    IF EXISTS (SELECT 1 FROM events WHERE event_provenance_type_id IS NULL) THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle left an event without provenance.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM actors actor
                        WHERE num_nonnulls(actor.user_id, actor.organization_id, actor.group_id,
                                           actor.external_actor_subject_id, actor.service_principal_id) <> 1)
                    THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle could not establish exactly one global actor owner.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM __global_actor_map actor_map
                        JOIN (
                            SELECT actor_id FROM actor_key_stores
                            UNION ALL SELECT target_actor_id FROM actor_subscriptions
                            UNION ALL SELECT actor_id FROM event_series
                            UNION ALL SELECT actor_id FROM ai_conversations WHERE actor_id IS NOT NULL
                            UNION ALL SELECT actor_id FROM tenant_users WHERE actor_id IS NOT NULL
                            UNION ALL SELECT actor_id FROM events
                            UNION ALL SELECT recipient_actor_id FROM event_contact_share_exports
                            UNION ALL SELECT recipient_actor_id FROM event_contact_share_consents
                            UNION ALL SELECT reporter_actor_id FROM event_reports WHERE reporter_actor_id IS NOT NULL
                            UNION ALL SELECT actor_id FROM event_session_speakers
                            UNION ALL SELECT source_actor_id FROM notification_fanout_runs
                            UNION ALL SELECT source_actor_id FROM notifications WHERE source_actor_id IS NOT NULL
                            UNION ALL SELECT recipient_context_actor_id FROM notifications WHERE recipient_context_actor_id IS NOT NULL
                            UNION ALL SELECT acting_actor_id FROM ai_proposed_actions WHERE acting_actor_id IS NOT NULL
                            UNION ALL SELECT actor_id FROM storage_objects WHERE actor_id IS NOT NULL
                        ) reference_actor ON reference_actor.actor_id = actor_map.source_actor_id)
                    THEN
                        RAISE EXCEPTION 'GlobalizeAtprotoActorLifecycle left a runtime actor reference on a merged source actor.';
                    END IF;
                END
                $$;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "event_provenance_type_id",
                table: "events",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "actor_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "default_actor_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "actor_id",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "approval_notes",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "approval_status_id",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "approved_at",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "approved_by",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "actor_id",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "approval_status_id",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "parent_group_id",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "parent_organization_id",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "profile_picture_id",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "event_url",
                table: "events");

            migrationBuilder.DropColumn(
                name: "is_user_reported",
                table: "events");

            migrationBuilder.DropColumn(
                name: "did_custody_type_id",
                table: "actors");

            migrationBuilder.DropColumn(
                name: "pds_host",
                table: "actors");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "actors");

            migrationBuilder.DropColumn(
                name: "profile_picture_id",
                table: "actors");

            migrationBuilder.DropColumn(
                name: "banner_picture_id",
                table: "actors");

            migrationBuilder.DropColumn(
                name: "background_image_id",
                table: "actors");

            migrationBuilder.DropColumn(
                name: "indexed_at",
                table: "actors");

            migrationBuilder.DropColumn(
                name: "did",
                table: "actor_pii");

            migrationBuilder.DropColumn(
                name: "handle",
                table: "actor_pii");

            migrationBuilder.RenameColumn(
                name: "organization_id",
                table: "organization_setting_overrides",
                newName: "organization_tenant_id");

            migrationBuilder.RenameIndex(
                name: "ix_organization_setting_overrides_organization_id_setting_key",
                table: "organization_setting_overrides",
                newName: "ix_organization_setting_overrides_organization_tenant_id_setti");

            migrationBuilder.RenameColumn(
                name: "organization_id",
                table: "organization_members",
                newName: "organization_tenant_id");

            migrationBuilder.RenameColumn(
                name: "group_id",
                table: "group_setting_overrides",
                newName: "group_tenant_id");

            migrationBuilder.RenameIndex(
                name: "ix_group_setting_overrides_group_id_setting_key",
                table: "group_setting_overrides",
                newName: "ix_group_setting_overrides_group_tenant_id_setting_key");

            migrationBuilder.RenameColumn(
                name: "group_id",
                table: "group_members",
                newName: "group_tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_consumers_group_id",
                table: "webhook_consumers",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_consumers_organization_id",
                table: "webhook_consumers",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_setting_overrides_tenant_id_organization_tenan",
                table: "organization_setting_overrides",
                columns: new[] { "tenant_id", "organization_tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_organization_members_tenant_id_organization_tenant_id",
                table: "organization_members",
                columns: new[] { "tenant_id", "organization_tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_fanout_runs_source_actor_id",
                table: "notification_fanout_runs",
                column: "source_actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_setting_overrides_tenant_id_group_tenant_id",
                table: "group_setting_overrides",
                columns: new[] { "tenant_id", "group_tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_group_members_tenant_id_group_tenant_id",
                table: "group_members",
                columns: new[] { "tenant_id", "group_tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_events_actor_id",
                table: "events",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_events_event_provenance_type_id",
                table: "events",
                column: "event_provenance_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_events_organizer_actor_id",
                table: "events",
                column: "organizer_actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_events_submitted_by_user_id",
                table: "events",
                column: "submitted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_session_speakers_actor_id",
                table: "event_session_speakers",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_reports_reporter_actor_id",
                table: "event_reports",
                column: "reporter_actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_contact_share_exports_recipient_actor_id",
                table: "event_contact_share_exports",
                column: "recipient_actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_contact_share_consents_recipient_actor_id",
                table: "event_contact_share_consents",
                column: "recipient_actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_actors_external_actor_subject_id",
                table: "actors",
                column: "external_actor_subject_id",
                unique: true,
                filter: "external_actor_subject_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_actors_service_principal_id",
                table: "actors",
                column: "service_principal_id",
                unique: true,
                filter: "service_principal_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_actors_user_id",
                table: "actors",
                column: "user_id",
                unique: true,
                filter: "user_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_actors_exactly_one_owner",
                table: "actors",
                sql: "num_nonnulls(user_id, organization_id, group_id, external_actor_subject_id, service_principal_id) = 1");

            migrationBuilder.CreateIndex(
                name: "ix_actor_subscriptions_target_actor_id",
                table: "actor_subscriptions",
                column: "target_actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_actor_merges_canonical_actor_id",
                table: "actor_merges",
                column: "canonical_actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_actor_merges_source_actor_id",
                table: "actor_merges",
                column: "source_actor_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_actor_moderation_records_actor_id",
                table: "actor_moderation_records",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_atproto_identities_actor_id",
                table: "atproto_identities",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_atproto_identities_did",
                table: "atproto_identities",
                column: "did",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_atproto_identities_did_custody_type_id",
                table: "atproto_identities",
                column: "did_custody_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_atproto_identity_moderation_records_atproto_identity_id",
                table: "atproto_identity_moderation_records",
                column: "atproto_identity_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_organizer_claim_statuses_master_code",
                table: "event_organizer_claim_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_organizer_claims_claimant_actor_id",
                table: "event_organizer_claims",
                column: "claimant_actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_organizer_claims_reviewer_user_id",
                table: "event_organizer_claims",
                column: "reviewer_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_organizer_claims_status_id",
                table: "event_organizer_claims",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_organizer_claims_tenant_id_event_id",
                table: "event_organizer_claims",
                columns: new[] { "tenant_id", "event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_provenance_types_master_code",
                table: "event_provenance_types",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_public_action_health_states_master_code",
                table: "event_public_action_health_states",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_public_action_kinds_master_code",
                table: "event_public_action_kinds",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_public_actions_event_public_action_kind_id",
                table: "event_public_actions",
                column: "event_public_action_kind_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_public_actions_health_state_id",
                table: "event_public_actions",
                column: "health_state_id");

            migrationBuilder.CreateIndex(
                name: "ux_event_public_actions_tenant_event_primary",
                table: "event_public_actions",
                columns: new[] { "tenant_id", "event_id" },
                unique: true,
                filter: "is_primary = true AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_group_tenants_approval_status_id",
                table: "group_tenants",
                column: "approval_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_tenants_background_image_id",
                table: "group_tenants",
                column: "background_image_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_tenants_banner_picture_id",
                table: "group_tenants",
                column: "banner_picture_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_tenants_group_id",
                table: "group_tenants",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_tenants_profile_picture_id",
                table: "group_tenants",
                column: "profile_picture_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_tenants_tenant_id_group_id",
                table: "group_tenants",
                columns: new[] { "tenant_id", "group_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_group_tenants_tenant_id_is_deleted_approval_status_id",
                table: "group_tenants",
                columns: new[] { "tenant_id", "is_deleted", "approval_status_id" });

            migrationBuilder.CreateIndex(
                name: "ix_group_tenants_tenant_id_parent_group_tenant_id",
                table: "group_tenants",
                columns: new[] { "tenant_id", "parent_group_tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_group_tenants_tenant_id_parent_organization_tenant_id",
                table: "group_tenants",
                columns: new[] { "tenant_id", "parent_organization_tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_organization_tenants_approval_status_id",
                table: "organization_tenants",
                column: "approval_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_tenants_background_image_id",
                table: "organization_tenants",
                column: "background_image_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_tenants_banner_picture_id",
                table: "organization_tenants",
                column: "banner_picture_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_tenants_organization_id",
                table: "organization_tenants",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_tenants_profile_picture_id",
                table: "organization_tenants",
                column: "profile_picture_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_tenants_tenant_id_is_deleted_approval_status_id",
                table: "organization_tenants",
                columns: new[] { "tenant_id", "is_deleted", "approval_status_id" });

            migrationBuilder.CreateIndex(
                name: "ix_organization_tenants_tenant_id_organization_id",
                table: "organization_tenants",
                columns: new[] { "tenant_id", "organization_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_service_principals_code",
                table: "service_principals",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_actor_subscriptions_actors_target_actor_id",
                table: "actor_subscriptions",
                column: "target_actor_id",
                principalTable: "actors",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_actors_external_actor_subjects_external_actor_subject_id",
                table: "actors",
                column: "external_actor_subject_id",
                principalTable: "external_actor_subjects",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_actors_groups_group_id",
                table: "actors",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_actors_organizations_organization_id",
                table: "actors",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_actors_service_principals_service_principal_id",
                table: "actors",
                column: "service_principal_id",
                principalTable: "service_principals",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_actors_users_user_id",
                table: "actors",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_contact_share_consents_actors_recipient_actor_id",
                table: "event_contact_share_consents",
                column: "recipient_actor_id",
                principalTable: "actors",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_contact_share_exports_actors_recipient_actor_id",
                table: "event_contact_share_exports",
                column: "recipient_actor_id",
                principalTable: "actors",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_reports_actors_reporter_actor_id",
                table: "event_reports",
                column: "reporter_actor_id",
                principalTable: "actors",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_session_speakers_actors_actor_id",
                table: "event_session_speakers",
                column: "actor_id",
                principalTable: "actors",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_events_actors_actor_id",
                table: "events",
                column: "actor_id",
                principalTable: "actors",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_events_actors_organizer_actor_id",
                table: "events",
                column: "organizer_actor_id",
                principalTable: "actors",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_events_event_provenance_types_event_provenance_type_id",
                table: "events",
                column: "event_provenance_type_id",
                principalTable: "event_provenance_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_events_users_submitted_by_user_id",
                table: "events",
                column: "submitted_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_group_members_group_tenants_tenant_id_group_tenant_id",
                table: "group_members",
                columns: new[] { "tenant_id", "group_tenant_id" },
                principalTable: "group_tenants",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_group_setting_overrides_group_tenants_tenant_id_group_tenan",
                table: "group_setting_overrides",
                columns: new[] { "tenant_id", "group_tenant_id" },
                principalTable: "group_tenants",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_fanout_runs_actors_source_actor_id",
                table: "notification_fanout_runs",
                column: "source_actor_id",
                principalTable: "actors",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_organization_members_organization_tenants_tenant_id_organiz",
                table: "organization_members",
                columns: new[] { "tenant_id", "organization_tenant_id" },
                principalTable: "organization_tenants",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_organization_setting_overrides_organization_tenants_tenant_",
                table: "organization_setting_overrides",
                columns: new[] { "tenant_id", "organization_tenant_id" },
                principalTable: "organization_tenants",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_consumers_groups_group_id",
                table: "webhook_consumers",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_consumers_organizations_organization_id",
                table: "webhook_consumers",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "GlobalizeAtprotoActorLifecycle is data-preserving only in the forward direction. Restore a backup taken before this migration instead of downgrading.");
#if false
            migrationBuilder.DropForeignKey(
                name: "fk_actor_subscriptions_actors_target_actor_id",
                table: "actor_subscriptions");

            migrationBuilder.DropForeignKey(
                name: "fk_actors_external_actor_subjects_external_actor_subject_id",
                table: "actors");

            migrationBuilder.DropForeignKey(
                name: "fk_actors_groups_group_id",
                table: "actors");

            migrationBuilder.DropForeignKey(
                name: "fk_actors_organizations_organization_id",
                table: "actors");

            migrationBuilder.DropForeignKey(
                name: "fk_actors_service_principals_service_principal_id",
                table: "actors");

            migrationBuilder.DropForeignKey(
                name: "fk_actors_users_user_id",
                table: "actors");

            migrationBuilder.DropForeignKey(
                name: "fk_event_contact_share_consents_actors_recipient_actor_id",
                table: "event_contact_share_consents");

            migrationBuilder.DropForeignKey(
                name: "fk_event_contact_share_exports_actors_recipient_actor_id",
                table: "event_contact_share_exports");

            migrationBuilder.DropForeignKey(
                name: "fk_event_reports_actors_reporter_actor_id",
                table: "event_reports");

            migrationBuilder.DropForeignKey(
                name: "fk_event_session_speakers_actors_actor_id",
                table: "event_session_speakers");

            migrationBuilder.DropForeignKey(
                name: "fk_events_actors_actor_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_events_actors_organizer_actor_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_events_event_provenance_types_event_provenance_type_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_events_users_submitted_by_user_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_group_members_group_tenants_tenant_id_group_tenant_id",
                table: "group_members");

            migrationBuilder.DropForeignKey(
                name: "fk_group_setting_overrides_group_tenants_tenant_id_group_tenan",
                table: "group_setting_overrides");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_fanout_runs_actors_source_actor_id",
                table: "notification_fanout_runs");

            migrationBuilder.DropForeignKey(
                name: "fk_organization_members_organization_tenants_tenant_id_organiz",
                table: "organization_members");

            migrationBuilder.DropForeignKey(
                name: "fk_organization_setting_overrides_organization_tenants_tenant_",
                table: "organization_setting_overrides");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_consumers_groups_group_id",
                table: "webhook_consumers");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_consumers_organizations_organization_id",
                table: "webhook_consumers");

            migrationBuilder.DropTable(
                name: "actor_merges");

            migrationBuilder.DropTable(
                name: "actor_moderation_records");

            migrationBuilder.DropTable(
                name: "atproto_identity_moderation_records");

            migrationBuilder.DropTable(
                name: "event_organizer_claims");

            migrationBuilder.DropTable(
                name: "event_provenance_types");

            migrationBuilder.DropTable(
                name: "event_public_actions");

            migrationBuilder.DropTable(
                name: "external_actor_subjects");

            migrationBuilder.DropTable(
                name: "group_tenants");

            migrationBuilder.DropTable(
                name: "service_principals");

            migrationBuilder.DropTable(
                name: "atproto_identities");

            migrationBuilder.DropTable(
                name: "event_organizer_claim_statuses");

            migrationBuilder.DropTable(
                name: "event_public_action_health_states");

            migrationBuilder.DropTable(
                name: "event_public_action_kinds");

            migrationBuilder.DropTable(
                name: "organization_tenants");

            migrationBuilder.DropIndex(
                name: "ix_webhook_consumers_group_id",
                table: "webhook_consumers");

            migrationBuilder.DropIndex(
                name: "ix_webhook_consumers_organization_id",
                table: "webhook_consumers");

            migrationBuilder.DropIndex(
                name: "ix_organization_setting_overrides_tenant_id_organization_tenan",
                table: "organization_setting_overrides");

            migrationBuilder.DropIndex(
                name: "ix_organization_members_tenant_id_organization_tenant_id",
                table: "organization_members");

            migrationBuilder.DropIndex(
                name: "ix_notification_fanout_runs_source_actor_id",
                table: "notification_fanout_runs");

            migrationBuilder.DropIndex(
                name: "ix_group_setting_overrides_tenant_id_group_tenant_id",
                table: "group_setting_overrides");

            migrationBuilder.DropIndex(
                name: "ix_group_members_tenant_id_group_tenant_id",
                table: "group_members");

            migrationBuilder.DropIndex(
                name: "ix_events_actor_id",
                table: "events");

            migrationBuilder.DropIndex(
                name: "ix_events_event_provenance_type_id",
                table: "events");

            migrationBuilder.DropIndex(
                name: "ix_events_organizer_actor_id",
                table: "events");

            migrationBuilder.DropIndex(
                name: "ix_events_submitted_by_user_id",
                table: "events");

            migrationBuilder.DropIndex(
                name: "ix_event_session_speakers_actor_id",
                table: "event_session_speakers");

            migrationBuilder.DropIndex(
                name: "ix_event_reports_reporter_actor_id",
                table: "event_reports");

            migrationBuilder.DropIndex(
                name: "ix_event_contact_share_exports_recipient_actor_id",
                table: "event_contact_share_exports");

            migrationBuilder.DropIndex(
                name: "ix_event_contact_share_consents_recipient_actor_id",
                table: "event_contact_share_consents");

            migrationBuilder.DropIndex(
                name: "ix_actors_external_actor_subject_id",
                table: "actors");

            migrationBuilder.DropIndex(
                name: "ix_actors_service_principal_id",
                table: "actors");

            migrationBuilder.DropIndex(
                name: "ix_actors_user_id",
                table: "actors");

            migrationBuilder.DropCheckConstraint(
                name: "ck_actors_exactly_one_owner",
                table: "actors");

            migrationBuilder.DropIndex(
                name: "ix_actor_subscriptions_target_actor_id",
                table: "actor_subscriptions");

            migrationBuilder.DropColumn(
                name: "event_provenance_type_id",
                table: "events");

            migrationBuilder.DropColumn(
                name: "organizer_actor_id",
                table: "events");

            migrationBuilder.DropColumn(
                name: "source_publisher_name",
                table: "events");

            migrationBuilder.DropColumn(
                name: "submitted_by_user_id",
                table: "events");

            migrationBuilder.DropColumn(
                name: "is_suspended",
                table: "actors");

            migrationBuilder.DropColumn(
                name: "moderation_reason_code",
                table: "actors");

            migrationBuilder.RenameColumn(
                name: "organization_tenant_id",
                table: "organization_setting_overrides",
                newName: "organization_id");

            migrationBuilder.RenameIndex(
                name: "ix_organization_setting_overrides_organization_tenant_id_setti",
                table: "organization_setting_overrides",
                newName: "ix_organization_setting_overrides_organization_id_setting_key");

            migrationBuilder.RenameColumn(
                name: "organization_tenant_id",
                table: "organization_members",
                newName: "organization_id");

            migrationBuilder.RenameColumn(
                name: "group_tenant_id",
                table: "group_setting_overrides",
                newName: "group_id");

            migrationBuilder.RenameIndex(
                name: "ix_group_setting_overrides_group_tenant_id_setting_key",
                table: "group_setting_overrides",
                newName: "ix_group_setting_overrides_group_id_setting_key");

            migrationBuilder.RenameColumn(
                name: "group_tenant_id",
                table: "group_members",
                newName: "group_id");

            migrationBuilder.RenameColumn(
                name: "suspended_by",
                table: "actors",
                newName: "profile_picture_id");

            migrationBuilder.RenameColumn(
                name: "suspended_at",
                table: "actors",
                newName: "indexed_at");

            migrationBuilder.RenameColumn(
                name: "service_principal_id",
                table: "actors",
                newName: "banner_picture_id");

            migrationBuilder.RenameColumn(
                name: "external_actor_subject_id",
                table: "actors",
                newName: "background_image_id");

            migrationBuilder.AddColumn<Guid>(
                name: "actor_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "default_actor_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "actor_id",
                table: "organizations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "approval_notes",
                table: "organizations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "approval_status_id",
                table: "organizations",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "approved_at",
                table: "organizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "approved_by",
                table: "organizations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "organizations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "actor_id",
                table: "groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "approval_status_id",
                table: "groups",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "parent_group_id",
                table: "groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "parent_organization_id",
                table: "groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "profile_picture_id",
                table: "groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "groups",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "event_url",
                table: "events",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_user_reported",
                table: "events",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "did_custody_type_id",
                table: "actors",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pds_host",
                table: "actors",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "actors",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "did",
                table: "actor_pii",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "handle",
                table: "actor_pii",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_organizations_tenant_id_id",
                table: "organizations",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_groups_tenant_id_id",
                table: "groups",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_actors_tenant_id_id",
                table: "actors",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_users_actor_id",
                table: "users",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_organizations_actor_id",
                table: "organizations",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_organizations_approval_status_id",
                table: "organizations",
                column: "approval_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_organizations_tenant",
                table: "organizations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_organizations_tenant_active_status",
                table: "organizations",
                columns: new[] { "tenant_id", "is_deleted", "approval_status_id" });

            migrationBuilder.CreateIndex(
                name: "ix_organization_setting_overrides_tenant_id",
                table: "organization_setting_overrides",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_members_tenant_id",
                table: "organization_members",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_fanout_runs_tenant_id_source_actor_id",
                table: "notification_fanout_runs",
                columns: new[] { "tenant_id", "source_actor_id" });

            migrationBuilder.CreateIndex(
                name: "ix_groups_actor_id",
                table: "groups",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_groups_approval_status_id",
                table: "groups",
                column: "approval_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_groups_profile_picture_id",
                table: "groups",
                column: "profile_picture_id");

            migrationBuilder.CreateIndex(
                name: "ix_groups_tenant_active_status",
                table: "groups",
                columns: new[] { "tenant_id", "is_deleted", "approval_status_id" });

            migrationBuilder.CreateIndex(
                name: "ix_groups_tenant_name",
                table: "groups",
                columns: new[] { "tenant_id", "full_name" });

            migrationBuilder.CreateIndex(
                name: "ix_groups_tenant_parent_group",
                table: "groups",
                columns: new[] { "tenant_id", "parent_group_id" });

            migrationBuilder.CreateIndex(
                name: "ix_groups_tenant_parent_organization",
                table: "groups",
                columns: new[] { "tenant_id", "parent_organization_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_groups_no_self_parent",
                table: "groups",
                sql: "parent_group_id IS NULL OR parent_group_id <> id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_groups_parent_exclusive",
                table: "groups",
                sql: "parent_organization_id IS NULL OR parent_group_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_group_setting_overrides_tenant_id",
                table: "group_setting_overrides",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_session_speakers_tenant_id_actor_id",
                table: "event_session_speakers",
                columns: new[] { "tenant_id", "actor_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_reports_tenant_id_reporter_actor_id",
                table: "event_reports",
                columns: new[] { "tenant_id", "reporter_actor_id" });

            migrationBuilder.CreateIndex(
                name: "ix_actors_background_image_id",
                table: "actors",
                column: "background_image_id");

            migrationBuilder.CreateIndex(
                name: "ix_actors_banner_picture_id",
                table: "actors",
                column: "banner_picture_id");

            migrationBuilder.CreateIndex(
                name: "ix_actors_did_custody_type_id",
                table: "actors",
                column: "did_custody_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_actors_profile_picture_id",
                table: "actors",
                column: "profile_picture_id");

            migrationBuilder.CreateIndex(
                name: "ix_actors_user_id_tenant_id",
                table: "actors",
                columns: new[] { "user_id", "tenant_id" },
                unique: true,
                filter: "user_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Actor_UserOrOrganization",
                table: "actors",
                sql: "(user_id IS NOT NULL AND organization_id IS NULL AND group_id IS NULL) OR (user_id IS NULL AND organization_id IS NOT NULL AND group_id IS NULL) OR (user_id IS NULL AND organization_id IS NULL AND group_id IS NOT NULL) OR (user_id IS NULL AND organization_id IS NULL AND group_id IS NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_actor_pii_did",
                table: "actor_pii",
                column: "did");

            migrationBuilder.CreateIndex(
                name: "ix_actor_pii_handle",
                table: "actor_pii",
                column: "handle");

            migrationBuilder.AddForeignKey(
                name: "fk_actor_subscriptions_actors_tenant_id_target_actor_id",
                table: "actor_subscriptions",
                columns: new[] { "tenant_id", "target_actor_id" },
                principalTable: "actors",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_actors_did_custody_types_did_custody_type_id",
                table: "actors",
                column: "did_custody_type_id",
                principalTable: "did_custody_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_actors_groups_group_id",
                table: "actors",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_actors_organizations_organization_id",
                table: "actors",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_actors_storage_objects_background_image_id",
                table: "actors",
                column: "background_image_id",
                principalTable: "storage_objects",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_actors_storage_objects_banner_picture_id",
                table: "actors",
                column: "banner_picture_id",
                principalTable: "storage_objects",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_actors_storage_objects_profile_picture_id",
                table: "actors",
                column: "profile_picture_id",
                principalTable: "storage_objects",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_actors_tenants_tenant_id",
                table: "actors",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_actors_users_user_id",
                table: "actors",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_event_contact_share_consents_actors_tenant_id_recipient_act",
                table: "event_contact_share_consents",
                columns: new[] { "tenant_id", "recipient_actor_id" },
                principalTable: "actors",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_contact_share_exports_actors_tenant_id_recipient_acto",
                table: "event_contact_share_exports",
                columns: new[] { "tenant_id", "recipient_actor_id" },
                principalTable: "actors",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_reports_actors_tenant_id_reporter_actor_id",
                table: "event_reports",
                columns: new[] { "tenant_id", "reporter_actor_id" },
                principalTable: "actors",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_session_speakers_actors_tenant_id_actor_id",
                table: "event_session_speakers",
                columns: new[] { "tenant_id", "actor_id" },
                principalTable: "actors",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_events_actors_tenant_id_actor_id",
                table: "events",
                columns: new[] { "tenant_id", "actor_id" },
                principalTable: "actors",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_group_members_groups_group_id",
                table: "group_members",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_group_setting_overrides_groups_group_id",
                table: "group_setting_overrides",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_groups_actors_actor_id",
                table: "groups",
                column: "actor_id",
                principalTable: "actors",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_groups_approval_statuses_approval_status_id",
                table: "groups",
                column: "approval_status_id",
                principalTable: "approval_statuses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_groups_groups_tenant_id_parent_group_id",
                table: "groups",
                columns: new[] { "tenant_id", "parent_group_id" },
                principalTable: "groups",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_groups_organizations_tenant_id_parent_organization_id",
                table: "groups",
                columns: new[] { "tenant_id", "parent_organization_id" },
                principalTable: "organizations",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_groups_storage_objects_profile_picture_id",
                table: "groups",
                column: "profile_picture_id",
                principalTable: "storage_objects",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_groups_tenants_tenant_id",
                table: "groups",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_fanout_runs_actors_tenant_id_source_actor_id",
                table: "notification_fanout_runs",
                columns: new[] { "tenant_id", "source_actor_id" },
                principalTable: "actors",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_organization_members_organizations_organization_id",
                table: "organization_members",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_organization_setting_overrides_organizations_organization_id",
                table: "organization_setting_overrides",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_organizations_actors_actor_id",
                table: "organizations",
                column: "actor_id",
                principalTable: "actors",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_organizations_approval_statuses_approval_status_id",
                table: "organizations",
                column: "approval_status_id",
                principalTable: "approval_statuses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_organizations_tenants_tenant_id",
                table: "organizations",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_users_actors_actor_id",
                table: "users",
                column: "actor_id",
                principalTable: "actors",
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
                name: "fk_webhook_consumers_organizations_tenant_id_organization_id",
                table: "webhook_consumers",
                columns: new[] { "tenant_id", "organization_id" },
                principalTable: "organizations",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
#endif
        }
    }
}
