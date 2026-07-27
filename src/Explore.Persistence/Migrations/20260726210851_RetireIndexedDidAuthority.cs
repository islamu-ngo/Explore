// ABOUTME: Promotes the legacy IndexedDid cache into exact-DID AtprotoIdentity authority before removing its table.
// ABOUTME: Creates deterministic external Actors for previously unclassified indexed identities and preserves downgrade data.

﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RetireIndexedDidAuthority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM indexed_dids
                        WHERE btrim(did) = ''
                           OR length(did) > 2048
                           OR btrim(pds_host) = ''
                           OR length(pds_host) > 2048
                           OR length(handle) > 253
                           OR length(signing_key) > 2048)
                    THEN
                        RAISE EXCEPTION 'RetireIndexedDidAuthority found legacy identity metadata outside AtprotoIdentity limits.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM indexed_dids indexed
                        JOIN atproto_identities identity
                          ON identity.did COLLATE "C" = indexed.did COLLATE "C"
                        WHERE identity.did IS DISTINCT FROM indexed.did)
                    THEN
                        RAISE EXCEPTION 'RetireIndexedDidAuthority found a non-exact DID collation collision.';
                    END IF;
                END
                $$;

                INSERT INTO actor_types (id, master_code, full_name, description)
                VALUES (3, 'BOT', 'Bot', 'Automated bot actor')
                ON CONFLICT DO NOTHING;

                INSERT INTO external_actor_subjects (
                    id, first_observed_at, last_observed_at, created_at, created_by,
                    updated_at, updated_by, is_deleted, deleted_at, deleted_by, concurrency_stamp)
                SELECT md5('indexed-did-subject:' || indexed.did)::uuid,
                       COALESCE(indexed.last_seen_at, indexed.last_indexed_at),
                       COALESCE(indexed.last_seen_at, indexed.last_indexed_at),
                       indexed.last_indexed_at,
                       NULL,
                       indexed.last_seen_at,
                       NULL,
                       FALSE,
                       NULL,
                       NULL,
                       md5('indexed-did-subject-stamp:' || indexed.did)::uuid
                FROM indexed_dids indexed
                LEFT JOIN atproto_identities identity
                  ON identity.did COLLATE "C" = indexed.did COLLATE "C"
                WHERE identity.id IS NULL
                ON CONFLICT (id) DO NOTHING;

                INSERT INTO actors (
                    id, actor_type_id, user_id, organization_id, group_id, external_actor_subject_id,
                    service_principal_id, is_suspended, suspended_at, suspended_by, moderation_reason_code,
                    description, profile_picture_cid, background_color, background_effect, banner_color,
                    created_at, created_by, updated_at, updated_by, is_deleted, deleted_at, deleted_by,
                    concurrency_stamp)
                SELECT md5('indexed-did-actor:' || indexed.did)::uuid,
                       3,
                       NULL,
                       NULL,
                       NULL,
                       md5('indexed-did-subject:' || indexed.did)::uuid,
                       NULL,
                       FALSE,
                       NULL,
                       NULL,
                       NULL,
                       NULL,
                       NULL,
                       NULL,
                       NULL,
                       NULL,
                       indexed.last_indexed_at,
                       NULL,
                       indexed.last_seen_at,
                       NULL,
                       FALSE,
                       NULL,
                       NULL,
                       md5('indexed-did-actor-stamp:' || indexed.did)::uuid
                FROM indexed_dids indexed
                LEFT JOIN atproto_identities identity
                  ON identity.did COLLATE "C" = indexed.did COLLATE "C"
                WHERE identity.id IS NULL
                ON CONFLICT (id) DO NOTHING;

                INSERT INTO actor_pii (actor_id, display_name, profile_picture_uri)
                SELECT md5('indexed-did-actor:' || indexed.did)::uuid,
                       COALESCE(NULLIF(indexed.handle, ''), indexed.did),
                       NULL
                FROM indexed_dids indexed
                LEFT JOIN atproto_identities identity
                  ON identity.did COLLATE "C" = indexed.did COLLATE "C"
                WHERE identity.id IS NULL
                ON CONFLICT (actor_id) DO NOTHING;

                INSERT INTO atproto_identities (
                    id, did, actor_id, did_custody_type_id, handle, pds_host, signing_key,
                    is_active, is_suspended, suspended_at, suspended_by, moderation_reason_code,
                    last_resolved_at, last_seen_at, created_at, created_by, updated_at, updated_by,
                    is_deleted, deleted_at, deleted_by, concurrency_stamp)
                SELECT md5('atproto-identity:' || indexed.did)::uuid,
                       indexed.did,
                       md5('indexed-did-actor:' || indexed.did)::uuid,
                       NULL,
                       indexed.handle,
                       indexed.pds_host,
                       indexed.signing_key,
                       indexed.is_active,
                       FALSE,
                       NULL,
                       NULL,
                       NULL,
                       indexed.last_indexed_at,
                       indexed.last_seen_at,
                       indexed.last_indexed_at,
                       NULL,
                       indexed.last_seen_at,
                       NULL,
                       FALSE,
                       NULL,
                       NULL,
                       md5('atproto-identity-stamp:' || indexed.did)::uuid
                FROM indexed_dids indexed
                ON CONFLICT (did) DO UPDATE
                SET handle = EXCLUDED.handle,
                    pds_host = EXCLUDED.pds_host,
                    signing_key = EXCLUDED.signing_key,
                    is_active = EXCLUDED.is_active,
                    last_resolved_at = GREATEST(atproto_identities.last_resolved_at, EXCLUDED.last_resolved_at),
                    last_seen_at = CASE
                        WHEN atproto_identities.last_seen_at IS NULL THEN EXCLUDED.last_seen_at
                        WHEN EXCLUDED.last_seen_at IS NULL THEN atproto_identities.last_seen_at
                        ELSE GREATEST(atproto_identities.last_seen_at, EXCLUDED.last_seen_at)
                    END,
                    updated_at = GREATEST(atproto_identities.updated_at, EXCLUDED.updated_at),
                    concurrency_stamp = md5('atproto-identity-stamp:' || EXCLUDED.did)::uuid;

                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM atproto_records record
                        LEFT JOIN atproto_identities identity
                          ON identity.did COLLATE "C" = record.did COLLATE "C"
                        WHERE identity.id IS NULL)
                    THEN
                        RAISE EXCEPTION 'RetireIndexedDidAuthority found an AT Protocol record without exact-DID identity metadata.';
                    END IF;

                    IF (SELECT count(*) FROM indexed_dids) > (
                        SELECT count(*)
                        FROM indexed_dids indexed
                        JOIN atproto_identities identity
                          ON identity.did COLLATE "C" = indexed.did COLLATE "C")
                    THEN
                        RAISE EXCEPTION 'RetireIndexedDidAuthority did not promote every indexed DID.';
                    END IF;
                END
                $$;
                """);

            migrationBuilder.DropTable(
                name: "indexed_dids");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "RetireIndexedDidAuthority is part of the forward-only actor lifecycle cutover. Restore a backup taken before the cutover instead of downgrading.");
        }
    }
}
