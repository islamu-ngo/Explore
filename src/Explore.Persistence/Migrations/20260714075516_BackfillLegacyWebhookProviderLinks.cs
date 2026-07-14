using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackfillLegacyWebhookProviderLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $migration$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM webhook_provider_links
                        WHERE provider <> 1)
                    THEN
                        RAISE EXCEPTION 'Legacy webhook provider links contain an unsupported provider.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM webhook_provider_links l
                        LEFT JOIN webhook_messages m
                          ON m.tenant_id = l.tenant_id
                         AND m.id = l.message_id
                        LEFT JOIN webhook_endpoints e
                          ON e.tenant_id = l.tenant_id
                         AND e.id = l.endpoint_id
                        WHERE (l.consumer_id IS NOT NULL
                               AND m.consumer_id IS NOT NULL
                               AND l.consumer_id <> m.consumer_id)
                           OR (l.consumer_id IS NOT NULL
                               AND e.consumer_id IS NOT NULL
                               AND l.consumer_id <> e.consumer_id)
                           OR (m.consumer_id IS NOT NULL
                               AND e.consumer_id IS NOT NULL
                               AND m.consumer_id <> e.consumer_id))
                    THEN
                        RAISE EXCEPTION 'Legacy webhook provider links contain conflicting consumer ownership.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM webhook_provider_links l
                        LEFT JOIN webhook_messages m
                          ON m.tenant_id = l.tenant_id
                         AND m.id = l.message_id
                        LEFT JOIN webhook_endpoints e
                          ON e.tenant_id = l.tenant_id
                         AND e.id = l.endpoint_id
                        WHERE COALESCE(l.consumer_id, m.consumer_id, e.consumer_id) IS NULL)
                    THEN
                        RAISE EXCEPTION 'Legacy webhook provider links contain evidence without a consumer owner.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM webhook_provider_links
                        WHERE external_app_id IS NOT NULL
                          AND NULLIF(BTRIM(external_app_id), '') IS NULL)
                    THEN
                        RAISE EXCEPTION 'Legacy webhook provider links contain a blank application identifier.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM webhook_provider_links
                        WHERE external_endpoint_id IS NOT NULL
                          AND NULLIF(BTRIM(external_endpoint_id), '') IS NULL)
                    THEN
                        RAISE EXCEPTION 'Legacy webhook provider links contain a blank endpoint identifier.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM webhook_provider_links
                        WHERE external_endpoint_id IS NOT NULL
                          AND endpoint_id IS NULL)
                    THEN
                        RAISE EXCEPTION 'Legacy webhook provider endpoint evidence has no endpoint owner.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM webhook_provider_links
                        WHERE message_id IS NULL
                          AND external_app_id IS NULL
                          AND external_endpoint_id IS NULL)
                    THEN
                        RAISE EXCEPTION 'Legacy webhook provider links contain rows with no durable destination.';
                    END IF;
                END
                $migration$;

                DO $migration$
                BEGIN
                    IF EXISTS (
                        WITH effective_links AS (
                            SELECT l.tenant_id,
                                   COALESCE(l.consumer_id, m.consumer_id, e.consumer_id) AS consumer_id,
                                   NULLIF(BTRIM(l.external_app_id), '') AS external_app_id
                            FROM webhook_provider_links l
                            LEFT JOIN webhook_messages m
                              ON m.tenant_id = l.tenant_id
                             AND m.id = l.message_id
                            LEFT JOIN webhook_endpoints e
                              ON e.tenant_id = l.tenant_id
                             AND e.id = l.endpoint_id
                            WHERE l.provider = 1
                        )
                        SELECT 1
                        FROM effective_links
                        WHERE external_app_id IS NOT NULL
                        GROUP BY tenant_id, consumer_id
                        HAVING COUNT(DISTINCT external_app_id) > 1)
                    THEN
                        RAISE EXCEPTION 'Legacy webhook consumer maps to multiple provider applications.';
                    END IF;

                    IF EXISTS (
                        WITH effective_links AS (
                            SELECT l.tenant_id,
                                   COALESCE(l.consumer_id, m.consumer_id, e.consumer_id) AS consumer_id,
                                   NULLIF(BTRIM(l.external_app_id), '') AS external_app_id
                            FROM webhook_provider_links l
                            LEFT JOIN webhook_messages m
                              ON m.tenant_id = l.tenant_id
                             AND m.id = l.message_id
                            LEFT JOIN webhook_endpoints e
                              ON e.tenant_id = l.tenant_id
                             AND e.id = l.endpoint_id
                            WHERE l.provider = 1
                        )
                        SELECT 1
                        FROM effective_links
                        WHERE external_app_id IS NOT NULL
                        GROUP BY tenant_id, external_app_id
                        HAVING COUNT(DISTINCT consumer_id) > 1)
                    THEN
                        RAISE EXCEPTION 'Legacy webhook provider application maps to multiple consumers.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM webhook_provider_links
                        WHERE endpoint_id IS NOT NULL
                          AND external_endpoint_id IS NOT NULL
                        GROUP BY tenant_id, endpoint_id
                        HAVING COUNT(DISTINCT BTRIM(external_endpoint_id)) > 1)
                    THEN
                        RAISE EXCEPTION 'Legacy webhook endpoint maps to multiple provider endpoints.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM webhook_provider_links l
                        JOIN webhook_endpoints e
                          ON e.tenant_id = l.tenant_id
                         AND e.id = l.endpoint_id
                        WHERE l.external_endpoint_id IS NOT NULL
                          AND e.provider_endpoint_id IS NOT NULL
                          AND e.provider_endpoint_id <> BTRIM(l.external_endpoint_id))
                    THEN
                        RAISE EXCEPTION 'Legacy webhook endpoint evidence conflicts with the endpoint snapshot.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM webhook_provider_links
                        WHERE message_id IS NOT NULL
                          AND NULLIF(BTRIM(external_message_id), '') IS NOT NULL
                        GROUP BY tenant_id, message_id
                        HAVING COUNT(DISTINCT BTRIM(external_message_id)) > 1)
                    THEN
                        RAISE EXCEPTION 'Legacy webhook message maps to multiple provider message identifiers.';
                    END IF;
                END
                $migration$;

                INSERT INTO webhook_delivery_plan_snapshots (
                    id,
                    tenant_id,
                    webhook_message_id,
                    webhook_consumer_id,
                    provider_mode_id,
                    configuration_version,
                    event_contract_version,
                    retention_policy,
                    retention_policy_version,
                    payload_retention_until_utc,
                    materialized_at_utc,
                    created_at)
                SELECT uuidv7(),
                       m.tenant_id,
                       m.id,
                       m.consumer_id,
                       c.provider_mode_id,
                       'legacy-current-consumer-v1',
                       'legacy-json-v1',
                       'legacy-message-retention',
                       '1',
                       m.payload_retention_until,
                       m.materialized_at,
                       m.materialized_at
                FROM webhook_messages m
                JOIN webhook_consumers c
                  ON c.tenant_id = m.tenant_id
                 AND c.id = m.consumer_id
                WHERE m.consumer_id IS NOT NULL
                ON CONFLICT (tenant_id, webhook_message_id) DO NOTHING;

                WITH effective_links AS (
                    SELECT l.tenant_id,
                           COALESCE(l.consumer_id, m.consumer_id, e.consumer_id) AS consumer_id,
                           NULLIF(BTRIM(l.external_app_id), '') AS external_app_id,
                           l.created_at
                    FROM webhook_provider_links l
                    LEFT JOIN webhook_messages m
                      ON m.tenant_id = l.tenant_id
                     AND m.id = l.message_id
                    LEFT JOIN webhook_endpoints e
                      ON e.tenant_id = l.tenant_id
                     AND e.id = l.endpoint_id
                    WHERE l.provider = 1
                ),
                binding_sources AS (
                    SELECT tenant_id,
                           consumer_id,
                           MIN(external_app_id) FILTER (WHERE external_app_id IS NOT NULL) AS external_app_id,
                           MIN(created_at) AS observed_at
                    FROM effective_links
                    GROUP BY tenant_id, consumer_id
                ),
                generated_bindings AS (
                    SELECT uuidv7() AS id,
                           uuidv7() AS instance_id,
                           tenant_id,
                           consumer_id,
                           external_app_id,
                           observed_at
                    FROM binding_sources
                )
                INSERT INTO webhook_consumer_provider_bindings (
                    id,
                    tenant_id,
                    webhook_consumer_id,
                    instance_id,
                    provider_kind_id,
                    provider_version,
                    provider_environment,
                    normalized_environment,
                    application_uid,
                    normalized_application_uid,
                    external_application_id,
                    normalized_external_application_id,
                    verification_state_id,
                    capabilities,
                    governance_allowed_capabilities,
                    capability_resolution_version,
                    capabilities_resolved_at_utc,
                    is_enabled,
                    concurrency_version,
                    verification_fence,
                    created_at)
                SELECT id,
                       tenant_id,
                       consumer_id,
                       instance_id,
                       2,
                       'legacy-unknown',
                       'legacy-unverified',
                       'LEGACY-UNVERIFIED',
                       'islamu-' || REPLACE(instance_id::text, '-', '') ||
                           '-consumer-' || REPLACE(consumer_id::text, '-', ''),
                       UPPER('islamu-' || REPLACE(instance_id::text, '-', '') ||
                           '-consumer-' || REPLACE(consumer_id::text, '-', '')),
                       external_app_id,
                       UPPER(external_app_id),
                       1,
                       0,
                       0,
                       'legacy-unverified-v1',
                       observed_at,
                       FALSE,
                       1,
                       1,
                       observed_at
                FROM generated_bindings
                ON CONFLICT (
                    tenant_id,
                    webhook_consumer_id,
                    provider_kind_id,
                    normalized_environment) DO NOTHING;

                WITH effective_links AS (
                    SELECT l.tenant_id,
                           COALESCE(l.consumer_id, m.consumer_id, e.consumer_id) AS consumer_id,
                           NULLIF(BTRIM(l.external_app_id), '') AS external_app_id
                    FROM webhook_provider_links l
                    LEFT JOIN webhook_messages m
                      ON m.tenant_id = l.tenant_id
                     AND m.id = l.message_id
                    LEFT JOIN webhook_endpoints e
                      ON e.tenant_id = l.tenant_id
                     AND e.id = l.endpoint_id
                    WHERE l.provider = 1
                ),
                binding_sources AS (
                    SELECT tenant_id,
                           consumer_id,
                           MIN(external_app_id) FILTER (WHERE external_app_id IS NOT NULL) AS external_app_id
                    FROM effective_links
                    GROUP BY tenant_id, consumer_id
                )
                UPDATE webhook_consumer_provider_bindings b
                SET external_application_id = s.external_app_id,
                    normalized_external_application_id = UPPER(s.external_app_id)
                FROM binding_sources s
                WHERE b.tenant_id = s.tenant_id
                  AND b.webhook_consumer_id = s.consumer_id
                  AND b.provider_kind_id = 2
                  AND b.normalized_environment = 'LEGACY-UNVERIFIED'
                  AND b.verification_state_id = 1
                  AND b.external_application_id IS NULL
                  AND s.external_app_id IS NOT NULL;

                DO $migration$
                BEGIN
                    IF EXISTS (
                        WITH effective_links AS (
                            SELECT l.tenant_id,
                                   COALESCE(l.consumer_id, m.consumer_id, e.consumer_id) AS consumer_id,
                                   NULLIF(BTRIM(l.external_app_id), '') AS external_app_id
                            FROM webhook_provider_links l
                            LEFT JOIN webhook_messages m
                              ON m.tenant_id = l.tenant_id
                             AND m.id = l.message_id
                            LEFT JOIN webhook_endpoints e
                              ON e.tenant_id = l.tenant_id
                             AND e.id = l.endpoint_id
                            WHERE l.provider = 1
                        )
                        SELECT 1
                        FROM effective_links l
                        JOIN webhook_consumer_provider_bindings b
                          ON b.tenant_id = l.tenant_id
                         AND b.webhook_consumer_id = l.consumer_id
                         AND b.provider_kind_id = 2
                         AND b.normalized_environment = 'LEGACY-UNVERIFIED'
                        WHERE l.external_app_id IS NOT NULL
                          AND b.external_application_id <> l.external_app_id)
                    THEN
                        RAISE EXCEPTION 'Legacy webhook application evidence conflicts with an existing binding.';
                    END IF;
                END
                $migration$;

                WITH endpoint_sources AS (
                    SELECT tenant_id,
                           endpoint_id,
                           MIN(BTRIM(external_endpoint_id)) AS external_endpoint_id,
                           MAX(COALESCE(last_synced_at, updated_at, created_at)) AS observed_at
                    FROM webhook_provider_links
                    WHERE provider = 1
                      AND endpoint_id IS NOT NULL
                      AND external_endpoint_id IS NOT NULL
                    GROUP BY tenant_id, endpoint_id
                )
                UPDATE webhook_endpoints e
                SET provider_endpoint_id = s.external_endpoint_id,
                    updated_at = COALESCE(e.updated_at, s.observed_at)
                FROM endpoint_sources s
                WHERE e.tenant_id = s.tenant_id
                  AND e.id = s.endpoint_id
                  AND e.provider_endpoint_id IS NULL;

                WITH publication_links AS (
                    SELECT l.*,
                           COALESCE(l.consumer_id, m.consumer_id, e.consumer_id) AS effective_consumer_id
                    FROM webhook_provider_links l
                    JOIN webhook_messages m
                      ON m.tenant_id = l.tenant_id
                     AND m.id = l.message_id
                    LEFT JOIN webhook_endpoints e
                      ON e.tenant_id = l.tenant_id
                     AND e.id = l.endpoint_id
                    WHERE l.provider = 1
                      AND l.message_id IS NOT NULL
                ),
                publication_evidence AS (
                    SELECT tenant_id,
                           message_id,
                           effective_consumer_id AS consumer_id,
                           (ARRAY_AGG(id ORDER BY created_at, id))[1] AS source_link_id,
                           MIN(NULLIF(BTRIM(external_message_id), '')) FILTER (
                               WHERE NULLIF(BTRIM(external_message_id), '') IS NOT NULL) AS external_message_id,
                           BOOL_OR(sync_state = 2
                               AND NULLIF(BTRIM(external_message_id), '') IS NOT NULL) AS has_queued_evidence,
                           BOOL_AND(sync_state = 4) AS all_disabled,
                           GREATEST(MAX(retry_count), 0) AS retry_count,
                           MIN(NULLIF(BTRIM(last_error_category), '')) FILTER (
                               WHERE NULLIF(BTRIM(last_error_category), '') IS NOT NULL) AS failure_category,
                           MAX(COALESCE(last_synced_at, updated_at, created_at)) AS observed_at
                    FROM publication_links
                    GROUP BY tenant_id, message_id, effective_consumer_id
                ),
                publication_sources AS (
                    SELECT evidence.*,
                           m.payload_hash,
                           p.id AS plan_id,
                           p.provider_mode_id,
                           p.configuration_version,
                           p.retention_policy_version,
                           p.payload_retention_until_utc,
                           p.materialized_at_utc,
                           b.id AS binding_id,
                           b.provider_version,
                           b.application_uid,
                           b.external_application_id,
                           b.provider_environment
                    FROM publication_evidence evidence
                    JOIN webhook_messages m
                      ON m.tenant_id = evidence.tenant_id
                     AND m.id = evidence.message_id
                     AND m.consumer_id = evidence.consumer_id
                    JOIN webhook_delivery_plan_snapshots p
                      ON p.tenant_id = evidence.tenant_id
                     AND p.webhook_message_id = evidence.message_id
                     AND p.webhook_consumer_id = evidence.consumer_id
                    JOIN webhook_consumer_provider_bindings b
                      ON b.tenant_id = evidence.tenant_id
                     AND b.webhook_consumer_id = evidence.consumer_id
                     AND b.provider_kind_id = 2
                     AND b.normalized_environment = 'LEGACY-UNVERIFIED'
                )
                INSERT INTO webhook_provider_publications (
                    id,
                    tenant_id,
                    webhook_message_id,
                    webhook_delivery_plan_snapshot_id,
                    provider_kind_id,
                    provider_binding_id,
                    provider_version,
                    provider_event_id,
                    idempotency_key,
                    request_hash,
                    application_uid,
                    provider_application_id,
                    provider_environment,
                    credential_reference,
                    credential_version,
                    mode_snapshot_id,
                    provider_configuration_version,
                    event_contract_version,
                    retention_policy_version,
                    payload_retention_until,
                    publication_retention_until,
                    idempotency_valid_until,
                    status_id,
                    external_provider_message_id,
                    automatic_publication_attempt_count,
                    automatic_reconciliation_attempt_count,
                    failure_category,
                    safe_detail,
                    publication_fence,
                    concurrency_version,
                    prepared_at,
                    provider_queued_at,
                    manual_reconciliation_at,
                    abandoned_at,
                    created_at)
                SELECT source_link_id,
                       tenant_id,
                       message_id,
                       plan_id,
                       2,
                       binding_id,
                       provider_version,
                       message_id::text,
                       message_id::text,
                       payload_hash,
                       application_uid,
                       external_application_id,
                       provider_environment,
                       'legacy-unavailable',
                       'legacy-unavailable',
                       provider_mode_id,
                       configuration_version,
                       1,
                       retention_policy_version,
                       GREATEST(
                           payload_retention_until_utc,
                           materialized_at_utc + INTERVAL '1 microsecond'),
                       GREATEST(
                           payload_retention_until_utc,
                           materialized_at_utc + INTERVAL '1 microsecond'),
                       materialized_at_utc + INTERVAL '12 hours',
                       CASE
                           WHEN has_queued_evidence THEN 3
                           WHEN all_disabled THEN 8
                           ELSE 7
                       END,
                       external_message_id,
                       retry_count,
                       0,
                       CASE
                           WHEN has_queued_evidence THEN NULL
                           WHEN all_disabled THEN 'legacy_provider_link_disabled'
                           ELSE COALESCE(failure_category, 'legacy_provider_link_unresolved')
                       END,
                       CASE
                           WHEN has_queued_evidence THEN NULL
                           ELSE 'Migrated legacy provider evidence requires explicit ownership verification.'
                       END,
                       0,
                       1,
                       materialized_at_utc,
                       CASE WHEN has_queued_evidence THEN observed_at END,
                       CASE WHEN NOT has_queued_evidence AND NOT all_disabled THEN observed_at END,
                       CASE WHEN all_disabled AND NOT has_queued_evidence THEN observed_at END,
                       materialized_at_utc
                FROM publication_sources
                ON CONFLICT DO NOTHING;

                INSERT INTO webhook_provider_publication_attempts (
                    id,
                    tenant_id,
                    webhook_provider_publication_id,
                    attempt_number,
                    publication_fence,
                    outcome_id,
                    started_at,
                    recorded_at,
                    external_provider_message_id,
                    failure_category,
                    safe_detail,
                    created_at)
                SELECT uuidv7(),
                       p.tenant_id,
                       p.id,
                       1,
                       0,
                       CASE p.status_id
                           WHEN 3 THEN 2
                           WHEN 8 THEN 10
                           ELSE 8
                       END,
                       COALESCE(
                           p.provider_queued_at,
                           p.manual_reconciliation_at,
                           p.abandoned_at,
                           p.prepared_at),
                       COALESCE(
                           p.provider_queued_at,
                           p.manual_reconciliation_at,
                           p.abandoned_at,
                           p.prepared_at),
                       CASE WHEN p.status_id = 3 THEN p.external_provider_message_id END,
                       CASE WHEN p.status_id = 3 THEN NULL ELSE p.failure_category END,
                       CASE WHEN p.status_id = 3 THEN NULL ELSE p.safe_detail END,
                       COALESCE(
                           p.provider_queued_at,
                           p.manual_reconciliation_at,
                           p.abandoned_at,
                           p.prepared_at)
                FROM webhook_provider_publications p
                WHERE p.provider_kind_id = 2
                  AND p.provider_environment = 'legacy-unverified'
                  AND p.credential_reference = 'legacy-unavailable'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM webhook_provider_publication_attempts a
                      WHERE a.tenant_id = p.tenant_id
                        AND a.webhook_provider_publication_id = p.id);

                DO $migration$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM webhook_provider_links l
                        WHERE l.message_id IS NOT NULL
                          AND NOT EXISTS (
                              SELECT 1
                              FROM webhook_provider_publications p
                              WHERE p.tenant_id = l.tenant_id
                                AND p.webhook_message_id = l.message_id
                                AND p.provider_kind_id = 2))
                    THEN
                        RAISE EXCEPTION 'Legacy webhook message evidence was not migrated to a publication.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM webhook_provider_links l
                        JOIN webhook_endpoints e
                          ON e.tenant_id = l.tenant_id
                         AND e.id = l.endpoint_id
                        WHERE l.external_endpoint_id IS NOT NULL
                          AND e.provider_endpoint_id <> BTRIM(l.external_endpoint_id))
                    THEN
                        RAISE EXCEPTION 'Legacy webhook endpoint evidence was not migrated.';
                    END IF;
                END
                $migration$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
