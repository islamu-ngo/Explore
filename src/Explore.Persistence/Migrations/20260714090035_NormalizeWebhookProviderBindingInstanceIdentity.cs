using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeWebhookProviderBindingInstanceIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $migration$
                DECLARE
                    completed_instance_count integer;
                    canonical_instance_id uuid;
                BEGIN
                    SELECT COUNT(*)
                    INTO completed_instance_count
                    FROM instance_bootstrap_states
                    WHERE is_completed = TRUE;

                    SELECT id
                    INTO canonical_instance_id
                    FROM instance_bootstrap_states
                    WHERE is_completed = TRUE
                    ORDER BY id
                    LIMIT 1;

                    IF EXISTS (SELECT 1 FROM webhook_consumer_provider_bindings)
                       AND completed_instance_count <> 1 THEN
                        RAISE EXCEPTION
                            'Webhook provider binding normalization requires exactly one completed instance bootstrap identity; found %.',
                            completed_instance_count;
                    END IF;

                    IF completed_instance_count = 1 THEN
                        INSERT INTO audit_logs (
                            id,
                            entity_type,
                            entity_id,
                            action,
                            old_values,
                            new_values,
                            affected_columns,
                            actor_id,
                            timestamp,
                            tenant_id)
                        SELECT uuidv7(),
                               'WebhookConsumerProviderBinding',
                               binding.id::text,
                               'WebhookBindingIdentityNormalized',
                               jsonb_build_object(
                                   'instanceId', binding.instance_id,
                                   'applicationUid', binding.application_uid,
                                   'normalizedApplicationUid', binding.normalized_application_uid,
                                   'verificationStateId', binding.verification_state_id,
                                   'verifiedTenantId', binding.verified_tenant_id,
                                   'verifiedWebhookConsumerId', binding.verified_webhook_consumer_id,
                                   'verifiedAtUtc', binding.verified_at_utc,
                                   'isEnabled', binding.is_enabled,
                                   'concurrencyVersion', binding.concurrency_version,
                                   'verificationFence', binding.verification_fence,
                                   'updatedAt', binding.updated_at),
                               jsonb_build_object(
                                   'migration', '20260714090035',
                                   'canonicalInstanceId', canonical_instance_id,
                                   'applicationUid',
                                       'islamu-' || REPLACE(canonical_instance_id::text, '-', '') ||
                                       '-consumer-' || REPLACE(binding.webhook_consumer_id::text, '-', ''),
                                   'outcome', 'invalidated_for_reverification'),
                               jsonb_build_array(
                                   'InstanceId',
                                   'ApplicationUid',
                                   'NormalizedApplicationUid',
                                   'VerificationStateId',
                                   'VerifiedTenantId',
                                   'VerifiedWebhookConsumerId',
                                   'VerifiedAtUtc',
                                   'IsEnabled',
                                   'ConcurrencyVersion',
                                   'VerificationFence'),
                               NULL,
                               NOW(),
                               binding.tenant_id
                        FROM webhook_consumer_provider_bindings AS binding
                        WHERE binding.instance_id <> canonical_instance_id
                           OR binding.application_uid <>
                              'islamu-' || REPLACE(canonical_instance_id::text, '-', '') ||
                              '-consumer-' || REPLACE(binding.webhook_consumer_id::text, '-', '')
                           OR binding.normalized_application_uid <>
                              UPPER(
                                  'islamu-' || REPLACE(canonical_instance_id::text, '-', '') ||
                                  '-consumer-' || REPLACE(binding.webhook_consumer_id::text, '-', ''));

                        UPDATE webhook_consumer_provider_bindings AS binding
                        SET instance_id = canonical_instance_id,
                            application_uid =
                                'islamu-' || REPLACE(canonical_instance_id::text, '-', '') ||
                                '-consumer-' || REPLACE(binding.webhook_consumer_id::text, '-', ''),
                            normalized_application_uid = UPPER(
                                'islamu-' || REPLACE(canonical_instance_id::text, '-', '') ||
                                '-consumer-' || REPLACE(binding.webhook_consumer_id::text, '-', '')),
                            verification_state_id = CASE
                                WHEN binding.verification_state_id IN (4, 5)
                                    THEN binding.verification_state_id
                                ELSE 1
                            END,
                            verified_tenant_id = CASE
                                WHEN binding.verification_state_id IN (4, 5)
                                    THEN binding.verified_tenant_id
                                ELSE NULL
                            END,
                            verified_webhook_consumer_id = CASE
                                WHEN binding.verification_state_id IN (4, 5)
                                    THEN binding.verified_webhook_consumer_id
                                ELSE NULL
                            END,
                            verified_at_utc = CASE
                                WHEN binding.verification_state_id IN (4, 5)
                                    THEN binding.verified_at_utc
                                ELSE NULL
                            END,
                            is_enabled = FALSE,
                            concurrency_version = binding.concurrency_version + 1,
                            verification_fence = binding.verification_fence + 1,
                            updated_at = NOW()
                        WHERE binding.instance_id <> canonical_instance_id
                           OR binding.application_uid <>
                              'islamu-' || REPLACE(canonical_instance_id::text, '-', '') ||
                              '-consumer-' || REPLACE(binding.webhook_consumer_id::text, '-', '')
                           OR binding.normalized_application_uid <>
                              UPPER(
                                  'islamu-' || REPLACE(canonical_instance_id::text, '-', '') ||
                                  '-consumer-' || REPLACE(binding.webhook_consumer_id::text, '-', ''));
                    END IF;
                END
                $migration$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                WITH migration_evidence AS (
                    SELECT DISTINCT ON (entity_id)
                           entity_id,
                           old_values
                    FROM audit_logs
                    WHERE action = 'WebhookBindingIdentityNormalized'
                      AND new_values ->> 'migration' = '20260714090035'
                    ORDER BY entity_id, timestamp DESC, id DESC
                )
                UPDATE webhook_consumer_provider_bindings AS binding
                SET instance_id = (evidence.old_values ->> 'instanceId')::uuid,
                    application_uid = evidence.old_values ->> 'applicationUid',
                    normalized_application_uid = evidence.old_values ->> 'normalizedApplicationUid',
                    verification_state_id = (evidence.old_values ->> 'verificationStateId')::integer,
                    verified_tenant_id = (evidence.old_values ->> 'verifiedTenantId')::uuid,
                    verified_webhook_consumer_id =
                        (evidence.old_values ->> 'verifiedWebhookConsumerId')::uuid,
                    verified_at_utc = (evidence.old_values ->> 'verifiedAtUtc')::timestamp with time zone,
                    is_enabled = (evidence.old_values ->> 'isEnabled')::boolean,
                    concurrency_version = (evidence.old_values ->> 'concurrencyVersion')::bigint,
                    verification_fence = (evidence.old_values ->> 'verificationFence')::bigint,
                    updated_at = (evidence.old_values ->> 'updatedAt')::timestamp with time zone
                FROM migration_evidence AS evidence
                WHERE binding.id::text = evidence.entity_id;

                DELETE FROM audit_logs
                WHERE action = 'WebhookBindingIdentityNormalized'
                  AND new_values ->> 'migration' = '20260714090035';
                """);
        }
    }
}
