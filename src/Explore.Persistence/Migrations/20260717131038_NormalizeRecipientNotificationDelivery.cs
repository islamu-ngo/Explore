// ABOUTME: Resets pre-1.0 notification delivery ledgers and installs explicit recipient-channel relationships.
// ABOUTME: Preserves user notifications and unrelated data while intentionally providing no legacy delivery compatibility.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations;

public partial class NormalizeRecipientNotificationDelivery : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM notification_deliveries;
            DELETE FROM email_dispatch_receipts;
            DELETE FROM email_dispatch_attempts;
            DELETE FROM email_dispatch_outbox;
            DELETE FROM notification_external_delegations;
            DELETE FROM notification_intents;

            ALTER TABLE email_dispatch_attempts
                DROP CONSTRAINT IF EXISTS fk_email_dispatch_attempts_email_dispatch_outbox_email_dispatc;
            ALTER TABLE email_dispatch_outbox
                DROP CONSTRAINT IF EXISTS fk_email_dispatch_outbox_users_user_id;
            ALTER TABLE email_dispatch_receipts
                DROP CONSTRAINT IF EXISTS fk_email_dispatch_receipts_email_dispatch_outbox_email_dispatc;
            ALTER TABLE notification_deliveries
                DROP CONSTRAINT IF EXISTS fk_notification_deliveries_email_dispatch_outbox_email_dispatc;
            ALTER TABLE notification_deliveries
                DROP CONSTRAINT IF EXISTS fk_notification_deliveries_notification_intents_notification_i;
            ALTER TABLE notification_intents
                DROP CONSTRAINT IF EXISTS fk_notification_intents_users_user_id;
            ALTER TABLE notification_external_delegations
                DROP CONSTRAINT IF EXISTS fk_notification_external_delegations_notification_intents_noti;

            DROP INDEX IF EXISTS ix_notification_intents_user_id;
            DROP INDEX IF EXISTS ix_notification_deliveries_email_dispatch_outbox_id;
            DROP INDEX IF EXISTS ix_notification_deliveries_notification_intent_id;
            DROP INDEX IF EXISTS ix_notification_deliveries_tenant_email_dispatch_outbox;
            DROP INDEX IF EXISTS ix_notification_deliveries_tenant_intent;
            DROP INDEX IF EXISTS ix_notification_external_delegations_notification_intent_id;
            DROP INDEX IF EXISTS ix_email_dispatch_outbox_user_id;
            DROP INDEX IF EXISTS ux_email_dispatch_outbox_tenant_source_kind;

            ALTER TABLE notification_intents RENAME COLUMN user_id TO recipient_user_id;
            ALTER TABLE notification_intents ALTER COLUMN recipient_user_id SET NOT NULL;
            ALTER TABLE email_dispatch_outbox RENAME COLUMN user_id TO recipient_user_id;
            ALTER TABLE email_dispatch_outbox ALTER COLUMN recipient_user_id SET NOT NULL;

            ALTER TABLE notifications
                ADD COLUMN notification_intent_id uuid NULL;
            ALTER TABLE notification_deliveries
                ADD COLUMN channel_id integer NOT NULL,
                ADD COLUMN consent_purpose character varying(100) NULL,
                ADD COLUMN consent_version integer NULL,
                ADD COLUMN delivery_policy_id integer NOT NULL,
                ADD COLUMN disclosure_level character varying(100) NOT NULL,
                ADD COLUMN is_required boolean NOT NULL,
                ADD COLUMN link_allowed boolean NOT NULL,
                ADD COLUMN notification_id uuid NULL,
                ADD COLUMN policy_version integer NOT NULL,
                ADD COLUMN preference_category_code character varying(100) NULL,
                ADD COLUMN preference_enabled boolean NULL,
                ADD COLUMN recipient_address_source integer NULL,
                ADD COLUMN template_key character varying(160) NOT NULL,
                ADD COLUMN template_version integer NOT NULL;
            ALTER TABLE email_dispatch_outbox
                ADD COLUMN managed_tenant_provisioning_operation_id uuid NULL,
                ADD COLUMN notification_intent_id uuid NOT NULL,
                ADD COLUMN recipient_address_source integer NOT NULL;

            CREATE TABLE notification_delivery_policies
            (
                id integer NOT NULL,
                master_code character varying(100) NOT NULL,
                full_name character varying(200) NOT NULL,
                description character varying(500) NULL,
                CONSTRAINT pk_notification_delivery_policies PRIMARY KEY (id)
            );
            CREATE UNIQUE INDEX ux_notification_delivery_policies_master_code
                ON notification_delivery_policies (master_code);

            INSERT INTO notification_delivery_policies (id, master_code, full_name, description) VALUES
                (1, 'REGISTRATION_STATUS_OPTIONAL', 'Registration status optional', 'Required in-app registration status with optional email'),
                (2, 'CRITICAL_EVENT_UPDATE_OPTIONAL', 'Critical event update optional', 'Required in-app event update with optional email'),
                (3, 'REPORT_CASE_UPDATE', 'Report case update', 'Reporter case update gated by case-update consent'),
                (4, 'REPORT_FOLLOW_UP_CONTACT', 'Report follow-up contact', 'Reporter clarification request gated by follow-up consent'),
                (5, 'MODERATION_AVAILABILITY_REQUIRED', 'Moderation availability required', 'Required operational availability and safety notice'),
                (6, 'MODERATION_CONTEXT_OPTIONAL', 'Moderation context optional', 'Optional contextual moderation notice'),
                (7, 'REMINDER_OPTIONAL', 'Reminder optional', 'Optional reminder delivery'),
                (8, 'TENANT_ADMINISTRATION_REQUIRED', 'Tenant administration required', 'Required tenant administration notification');

            INSERT INTO notification_preference_channels (id, master_code, full_name, description, sort_order) VALUES
                (1, 'email', 'Email', 'Email delivery through ISLAMU Event email dispatch infrastructure', 10),
                (2, 'in_app', 'In-App', 'Durable in-app notification rows surfaced by the notification inbox', 20)
            ON CONFLICT (id) DO UPDATE SET
                master_code = EXCLUDED.master_code,
                full_name = EXCLUDED.full_name,
                description = EXCLUDED.description,
                sort_order = EXCLUDED.sort_order;

            INSERT INTO notification_delivery_statuses (id, master_code, full_name, description) VALUES
                (1, 'PENDING', 'Pending', 'Delivery audit row is pending dispatch linkage'),
                (2, 'QUEUED', 'Queued', 'Delivery has durable channel work queued'),
                (3, 'DELIVERED', 'Delivered', 'Delivery completed successfully'),
                (4, 'SKIPPED', 'Skipped', 'Delivery was skipped by policy or preference'),
                (5, 'FAILED', 'Failed', 'Delivery failed and may be retried or reviewed'),
                (6, 'DEAD_LETTERED', 'Dead lettered', 'Delivery exhausted retry policy and is retained for operator review'),
                (7, 'UNKNOWN', 'Unknown', 'Provider acceptance is uncertain and automatic retry is disabled'),
                (8, 'PARKED', 'Parked', 'Operator parked delivery pending review'),
                (9, 'SUPERSEDED', 'Superseded', 'Newer authoritative work replaced this unsent delivery')
            ON CONFLICT (id) DO UPDATE SET
                master_code = EXCLUDED.master_code,
                full_name = EXCLUDED.full_name,
                description = EXCLUDED.description;

            ALTER TABLE notifications
                ADD CONSTRAINT ak_notifications_tenant_id UNIQUE (tenant_id, id);
            CREATE UNIQUE INDEX ux_notifications_tenant_notification_intent
                ON notifications (tenant_id, notification_intent_id)
                WHERE notification_intent_id IS NOT NULL AND is_deleted = false;
            CREATE UNIQUE INDEX ux_notifications_tenant_id_intent_link
                ON notifications (tenant_id, id, notification_intent_id);
            CREATE INDEX ix_notifications_tenant_id_notification_intent_id_user_id
                ON notifications (tenant_id, notification_intent_id, user_id);

            ALTER TABLE notification_intents
                ADD CONSTRAINT ak_notification_intents_tenant_id UNIQUE (tenant_id, id),
                ADD CONSTRAINT ak_notification_intents_tenant_id_recipient
                    UNIQUE (tenant_id, id, recipient_user_id);
            CREATE INDEX ix_notification_intents_tenant_id_recipient_user_id
                ON notification_intents (tenant_id, recipient_user_id);

            ALTER TABLE notification_deliveries
                ADD CONSTRAINT ak_notification_deliveries_tenant_id_intent_channel
                    UNIQUE (tenant_id, id, notification_intent_id, channel_id);
            CREATE INDEX ix_notification_deliveries_channel_id ON notification_deliveries (channel_id);
            CREATE INDEX ix_notification_deliveries_delivery_policy_id ON notification_deliveries (delivery_policy_id);
            CREATE INDEX ix_notification_deliveries_tenant_id_email_dispatch_outbox_id_
                ON notification_deliveries
                    (tenant_id, email_dispatch_outbox_id, notification_intent_id, recipient_address_source);
            CREATE UNIQUE INDEX ux_notification_deliveries_tenant_email_dispatch_outbox
                ON notification_deliveries (tenant_id, email_dispatch_outbox_id)
                WHERE email_dispatch_outbox_id IS NOT NULL;
            CREATE UNIQUE INDEX ux_notification_deliveries_tenant_intent_channel
                ON notification_deliveries (tenant_id, notification_intent_id, channel_id);
            CREATE UNIQUE INDEX ux_notification_deliveries_tenant_notification
                ON notification_deliveries (tenant_id, notification_id)
                WHERE notification_id IS NOT NULL;

            ALTER TABLE email_dispatch_outbox
                ADD CONSTRAINT ak_email_dispatch_outbox_tenant_id UNIQUE (tenant_id, id),
                ADD CONSTRAINT ak_email_dispatch_outbox_tenant_id_intent UNIQUE (tenant_id, id, notification_intent_id),
                ADD CONSTRAINT ak_email_dispatch_outbox_tenant_id_intent_address_source
                    UNIQUE (tenant_id, id, notification_intent_id, recipient_address_source),
                ADD CONSTRAINT ak_email_dispatch_outbox_tenant_id_publish_event UNIQUE (tenant_id, id, publish_event_id);
            CREATE INDEX ix_email_dispatch_outbox_managed_tenant_provisioning_operation
                ON email_dispatch_outbox (managed_tenant_provisioning_operation_id);
            CREATE INDEX ix_email_dispatch_outbox_tenant_id_recipient_user_id
                ON email_dispatch_outbox (tenant_id, recipient_user_id);
            CREATE INDEX ix_email_dispatch_outbox_tenant_id_notification_intent_id_reci
                ON email_dispatch_outbox (tenant_id, notification_intent_id, recipient_user_id);
            CREATE UNIQUE INDEX ux_email_dispatch_outbox_tenant_intent
                ON email_dispatch_outbox (tenant_id, notification_intent_id);
            CREATE UNIQUE INDEX ux_managed_tenant_provisioning_operations_tenant_id
                ON managed_tenant_provisioning_operations (tenant_id, id);

            CREATE INDEX ix_email_dispatch_attempts_tenant_id_email_dispatch_outbox_id
                ON email_dispatch_attempts (tenant_id, email_dispatch_outbox_id);
            CREATE INDEX ix_email_dispatch_receipts_tenant_id_email_dispatch_outbox_id_
                ON email_dispatch_receipts (tenant_id, email_dispatch_outbox_id, publish_event_id);
            CREATE UNIQUE INDEX ux_email_dispatch_receipts_tenant_outbox
                ON email_dispatch_receipts (tenant_id, email_dispatch_outbox_id);

            ALTER TABLE notification_deliveries
                ADD CONSTRAINT ck_notification_deliveries_channel_link CHECK
                (
                    NOT (email_dispatch_outbox_id IS NOT NULL AND notification_id IS NOT NULL)
                    AND (email_dispatch_outbox_id IS NULL
                        OR (channel_id = 1 AND recipient_address_source IS NOT NULL))
                    AND (notification_id IS NULL OR channel_id = 2)
                    AND (channel_id <> 2 OR recipient_address_source IS NULL)
                    AND (email_dispatch_outbox_id IS NOT NULL OR recipient_address_source IS NULL)
                );
            ALTER TABLE email_dispatch_outbox
                ADD CONSTRAINT ck_email_dispatch_outbox_recipient_authority CHECK
                (
                    (recipient_address_source = 1 AND managed_tenant_provisioning_operation_id IS NULL AND kind <> 8)
                    OR
                    (recipient_address_source = 2 AND managed_tenant_provisioning_operation_id IS NOT NULL
                        AND kind = 8 AND source_type = 'managed_tenant_provisioning'
                        AND source_id = managed_tenant_provisioning_operation_id)
                );
            ALTER TABLE email_dispatch_outbox
                ADD CONSTRAINT ck_email_dispatch_outbox_processing_fence CHECK
                ((status = 2) = (processing_started_at IS NOT NULL AND processing_lease_token IS NOT NULL));
            ALTER TABLE email_dispatch_outbox
                ADD CONSTRAINT ck_email_dispatch_outbox_unknown_terminal CHECK
                (status <> 7 OR (unknown_at IS NOT NULL AND next_attempt_at IS NULL
                    AND processing_started_at IS NULL AND processing_lease_token IS NULL));
            ALTER TABLE email_dispatch_attempts
                ADD CONSTRAINT ck_email_dispatch_attempts_provider_handoff_fence CHECK
                (failure_category <> 'provider_handoff_started'
                    OR (outcome = 3 AND completed_at IS NULL AND provider_message_id IS NULL));

            ALTER TABLE email_dispatch_attempts
                ADD CONSTRAINT fk_email_dispatch_attempts_email_dispatch_outbox_tenant_id_ema
                FOREIGN KEY (tenant_id, email_dispatch_outbox_id)
                REFERENCES email_dispatch_outbox (tenant_id, id) ON DELETE CASCADE;
            ALTER TABLE email_dispatch_outbox
                ADD CONSTRAINT fk_email_dispatch_outbox_managed_tenant_provisioning_operation
                FOREIGN KEY (managed_tenant_provisioning_operation_id)
                REFERENCES managed_tenant_provisioning_operations (id) ON DELETE RESTRICT;
            ALTER TABLE email_dispatch_outbox
                ADD CONSTRAINT fk_email_dispatch_outbox_managed_operation_tenant
                FOREIGN KEY (tenant_id, managed_tenant_provisioning_operation_id)
                REFERENCES managed_tenant_provisioning_operations (tenant_id, id) ON DELETE RESTRICT;
            ALTER TABLE email_dispatch_outbox
                ADD CONSTRAINT fk_email_dispatch_outbox_tenant_users_tenant_id_recipient_user
                FOREIGN KEY (tenant_id, recipient_user_id)
                REFERENCES tenant_users (tenant_id, user_id) ON DELETE RESTRICT;
            ALTER TABLE email_dispatch_outbox
                ADD CONSTRAINT fk_email_dispatch_outbox_recipient_matches_intent
                FOREIGN KEY (tenant_id, notification_intent_id, recipient_user_id)
                REFERENCES notification_intents (tenant_id, id, recipient_user_id) ON DELETE RESTRICT;
            ALTER TABLE email_dispatch_receipts
                ADD CONSTRAINT fk_email_dispatch_receipts_email_dispatch_outbox_tenant_id_ema
                FOREIGN KEY (tenant_id, email_dispatch_outbox_id, publish_event_id)
                REFERENCES email_dispatch_outbox (tenant_id, id, publish_event_id) ON DELETE CASCADE;
            ALTER TABLE notification_deliveries
                ADD CONSTRAINT fk_notification_deliveries_email_dispatch_outbox_tenant_id_ema
                FOREIGN KEY (tenant_id, email_dispatch_outbox_id, notification_intent_id, recipient_address_source)
                REFERENCES email_dispatch_outbox
                    (tenant_id, id, notification_intent_id, recipient_address_source) ON DELETE RESTRICT;
            ALTER TABLE notification_deliveries
                ADD CONSTRAINT fk_notification_deliveries_notification_delivery_policy_delive
                FOREIGN KEY (delivery_policy_id)
                REFERENCES notification_delivery_policies (id) ON DELETE RESTRICT;
            ALTER TABLE notification_deliveries
                ADD CONSTRAINT fk_notification_deliveries_notification_intents_tenant_id_noti
                FOREIGN KEY (tenant_id, notification_intent_id)
                REFERENCES notification_intents (tenant_id, id) ON DELETE RESTRICT;
            ALTER TABLE notification_deliveries
                ADD CONSTRAINT fk_notification_deliveries_notification_preference_channels_ch
                FOREIGN KEY (channel_id)
                REFERENCES notification_preference_channels (id) ON DELETE RESTRICT;
            ALTER TABLE notification_deliveries
                ADD CONSTRAINT fk_notification_deliveries_notification_tenant
                FOREIGN KEY (tenant_id, notification_id)
                REFERENCES notifications (tenant_id, id) ON DELETE RESTRICT;
            ALTER TABLE notification_deliveries
                ADD CONSTRAINT fk_notification_deliveries_notification_same_intent
                FOREIGN KEY (tenant_id, notification_id, notification_intent_id)
                REFERENCES notifications (tenant_id, id, notification_intent_id) ON DELETE RESTRICT;
            ALTER TABLE notification_intents
                ADD CONSTRAINT fk_notification_intents_tenant_users_tenant_id_recipient_user_
                FOREIGN KEY (tenant_id, recipient_user_id)
                REFERENCES tenant_users (tenant_id, user_id) ON DELETE RESTRICT;
            ALTER TABLE notification_external_delegations
                ADD CONSTRAINT fk_notification_external_delegations_tenant_intent
                FOREIGN KEY (tenant_id, notification_intent_id)
                REFERENCES notification_intents (tenant_id, id) ON DELETE RESTRICT;
            ALTER TABLE notifications
                ADD CONSTRAINT fk_notifications_recipient_matches_intent
                FOREIGN KEY (tenant_id, notification_intent_id, user_id)
                REFERENCES notification_intents (tenant_id, id, recipient_user_id) ON DELETE RESTRICT;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM notification_deliveries;
            DELETE FROM email_dispatch_receipts;
            DELETE FROM email_dispatch_attempts;
            DELETE FROM email_dispatch_outbox;
            DELETE FROM notification_external_delegations;
            UPDATE notifications SET notification_intent_id = NULL;
            DELETE FROM notification_intents;

            ALTER TABLE email_dispatch_attempts DROP CONSTRAINT IF EXISTS fk_email_dispatch_attempts_email_dispatch_outbox_tenant_id_ema;
            ALTER TABLE email_dispatch_outbox DROP CONSTRAINT IF EXISTS fk_email_dispatch_outbox_managed_tenant_provisioning_operation;
            ALTER TABLE email_dispatch_outbox DROP CONSTRAINT IF EXISTS fk_email_dispatch_outbox_managed_operation_tenant;
            ALTER TABLE email_dispatch_outbox DROP CONSTRAINT IF EXISTS fk_email_dispatch_outbox_tenant_users_tenant_id_recipient_user;
            ALTER TABLE email_dispatch_outbox DROP CONSTRAINT IF EXISTS fk_email_dispatch_outbox_recipient_matches_intent;
            ALTER TABLE email_dispatch_receipts DROP CONSTRAINT IF EXISTS fk_email_dispatch_receipts_email_dispatch_outbox_tenant_id_ema;
            ALTER TABLE notification_deliveries DROP CONSTRAINT IF EXISTS fk_notification_deliveries_email_dispatch_outbox_tenant_id_ema;
            ALTER TABLE notification_deliveries DROP CONSTRAINT IF EXISTS fk_notification_deliveries_notification_delivery_policy_delive;
            ALTER TABLE notification_deliveries DROP CONSTRAINT IF EXISTS fk_notification_deliveries_notification_intents_tenant_id_noti;
            ALTER TABLE notification_deliveries DROP CONSTRAINT IF EXISTS fk_notification_deliveries_notification_preference_channels_ch;
            ALTER TABLE notification_deliveries DROP CONSTRAINT IF EXISTS fk_notification_deliveries_notification_tenant;
            ALTER TABLE notification_deliveries DROP CONSTRAINT IF EXISTS fk_notification_deliveries_notification_same_intent;
            ALTER TABLE notification_intents DROP CONSTRAINT IF EXISTS fk_notification_intents_tenant_users_tenant_id_recipient_user_;
            ALTER TABLE notification_external_delegations DROP CONSTRAINT IF EXISTS fk_notification_external_delegations_tenant_intent;
            ALTER TABLE notifications DROP CONSTRAINT IF EXISTS fk_notifications_recipient_matches_intent;

            ALTER TABLE email_dispatch_attempts DROP CONSTRAINT IF EXISTS ck_email_dispatch_attempts_provider_handoff_fence;
            ALTER TABLE email_dispatch_outbox DROP CONSTRAINT IF EXISTS ck_email_dispatch_outbox_recipient_authority;
            ALTER TABLE email_dispatch_outbox DROP CONSTRAINT IF EXISTS ck_email_dispatch_outbox_processing_fence;
            ALTER TABLE email_dispatch_outbox DROP CONSTRAINT IF EXISTS ck_email_dispatch_outbox_unknown_terminal;
            ALTER TABLE notification_deliveries DROP CONSTRAINT IF EXISTS ck_notification_deliveries_channel_link;

            DROP INDEX IF EXISTS ix_email_dispatch_attempts_tenant_id_email_dispatch_outbox_id;
            DROP INDEX IF EXISTS ix_email_dispatch_receipts_tenant_id_email_dispatch_outbox_id_;
            DROP INDEX IF EXISTS ux_email_dispatch_receipts_tenant_outbox;
            DROP INDEX IF EXISTS ix_email_dispatch_outbox_managed_tenant_provisioning_operation;
            DROP INDEX IF EXISTS ix_email_dispatch_outbox_tenant_id_recipient_user_id;
            DROP INDEX IF EXISTS ix_email_dispatch_outbox_tenant_id_notification_intent_id_reci;
            DROP INDEX IF EXISTS ux_email_dispatch_outbox_tenant_intent;
            DROP INDEX IF EXISTS ux_managed_tenant_provisioning_operations_tenant_id;
            DROP INDEX IF EXISTS ix_notification_deliveries_channel_id;
            DROP INDEX IF EXISTS ix_notification_deliveries_delivery_policy_id;
            DROP INDEX IF EXISTS ix_notification_deliveries_notification_id;
            DROP INDEX IF EXISTS ix_notification_deliveries_tenant_id_email_dispatch_outbox_id_;
            DROP INDEX IF EXISTS ux_notification_deliveries_tenant_email_dispatch_outbox;
            DROP INDEX IF EXISTS ux_notification_deliveries_tenant_intent_channel;
            DROP INDEX IF EXISTS ux_notification_deliveries_tenant_notification;
            DROP INDEX IF EXISTS ix_notification_intents_tenant_id_recipient_user_id;
            DROP INDEX IF EXISTS ux_notifications_tenant_notification_intent;
            DROP INDEX IF EXISTS ux_notifications_tenant_id_intent_link;
            DROP INDEX IF EXISTS ix_notifications_tenant_id_notification_intent_id_user_id;

            ALTER TABLE email_dispatch_outbox DROP CONSTRAINT IF EXISTS ak_email_dispatch_outbox_tenant_id_publish_event;
            ALTER TABLE email_dispatch_outbox DROP CONSTRAINT IF EXISTS ak_email_dispatch_outbox_tenant_id_intent_address_source;
            ALTER TABLE email_dispatch_outbox DROP CONSTRAINT IF EXISTS ak_email_dispatch_outbox_tenant_id_intent;
            ALTER TABLE email_dispatch_outbox DROP CONSTRAINT IF EXISTS ak_email_dispatch_outbox_tenant_id;
            ALTER TABLE notification_deliveries DROP CONSTRAINT IF EXISTS ak_notification_deliveries_tenant_id_intent_channel;
            ALTER TABLE notification_intents DROP CONSTRAINT IF EXISTS ak_notification_intents_tenant_id;
            ALTER TABLE notification_intents DROP CONSTRAINT IF EXISTS ak_notification_intents_tenant_id_recipient;
            ALTER TABLE notifications DROP CONSTRAINT IF EXISTS ak_notifications_tenant_id;

            ALTER TABLE notifications DROP COLUMN notification_intent_id;
            ALTER TABLE notification_deliveries
                DROP COLUMN channel_id,
                DROP COLUMN consent_purpose,
                DROP COLUMN consent_version,
                DROP COLUMN delivery_policy_id,
                DROP COLUMN disclosure_level,
                DROP COLUMN is_required,
                DROP COLUMN link_allowed,
                DROP COLUMN notification_id,
                DROP COLUMN policy_version,
                DROP COLUMN preference_category_code,
                DROP COLUMN preference_enabled,
                DROP COLUMN recipient_address_source,
                DROP COLUMN template_key,
                DROP COLUMN template_version;
            ALTER TABLE email_dispatch_outbox
                DROP COLUMN managed_tenant_provisioning_operation_id,
                DROP COLUMN notification_intent_id,
                DROP COLUMN recipient_address_source;

            DROP TABLE notification_delivery_policies;

            ALTER TABLE notification_intents ALTER COLUMN recipient_user_id DROP NOT NULL;
            ALTER TABLE notification_intents RENAME COLUMN recipient_user_id TO user_id;
            ALTER TABLE email_dispatch_outbox ALTER COLUMN recipient_user_id DROP NOT NULL;
            ALTER TABLE email_dispatch_outbox RENAME COLUMN recipient_user_id TO user_id;

            UPDATE notification_preference_channels
            SET master_code = 'in-app',
                full_name = 'In-App',
                description = 'In-application notifications',
                sort_order = 20
            WHERE id = 2;

            UPDATE notification_delivery_statuses
            SET master_code = 'LINKED_TO_EMAIL_DISPATCH',
                full_name = 'Linked to email dispatch',
                description = 'Delivery has a linked EmailDispatchOutbox row'
            WHERE id = 2;
            UPDATE notification_delivery_statuses
            SET master_code = 'SENT',
                full_name = 'Sent',
                description = 'Delivery was sent successfully'
            WHERE id = 3;
            DELETE FROM notification_delivery_statuses WHERE id IN (7, 8, 9);

            CREATE INDEX ix_notification_intents_user_id ON notification_intents (user_id);
            CREATE INDEX ix_notification_deliveries_email_dispatch_outbox_id ON notification_deliveries (email_dispatch_outbox_id);
            CREATE INDEX ix_notification_deliveries_notification_intent_id ON notification_deliveries (notification_intent_id);
            CREATE INDEX ix_notification_deliveries_tenant_email_dispatch_outbox ON notification_deliveries (tenant_id, email_dispatch_outbox_id);
            CREATE INDEX ix_notification_deliveries_tenant_intent ON notification_deliveries (tenant_id, notification_intent_id);
            CREATE INDEX ix_notification_external_delegations_notification_intent_id
                ON notification_external_delegations (notification_intent_id);
            CREATE INDEX ix_email_dispatch_outbox_user_id ON email_dispatch_outbox (user_id);
            CREATE UNIQUE INDEX ux_email_dispatch_outbox_tenant_source_kind
                ON email_dispatch_outbox (tenant_id, source_type, source_id, kind)
                WHERE is_deleted = false;

            ALTER TABLE email_dispatch_attempts
                ADD CONSTRAINT fk_email_dispatch_attempts_email_dispatch_outbox_email_dispatc
                FOREIGN KEY (email_dispatch_outbox_id) REFERENCES email_dispatch_outbox (id) ON DELETE CASCADE;
            ALTER TABLE email_dispatch_outbox
                ADD CONSTRAINT fk_email_dispatch_outbox_users_user_id
                FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT;
            ALTER TABLE email_dispatch_receipts
                ADD CONSTRAINT fk_email_dispatch_receipts_email_dispatch_outbox_email_dispatc
                FOREIGN KEY (email_dispatch_outbox_id) REFERENCES email_dispatch_outbox (id) ON DELETE CASCADE;
            ALTER TABLE notification_deliveries
                ADD CONSTRAINT fk_notification_deliveries_email_dispatch_outbox_email_dispatc
                FOREIGN KEY (email_dispatch_outbox_id) REFERENCES email_dispatch_outbox (id) ON DELETE RESTRICT;
            ALTER TABLE notification_deliveries
                ADD CONSTRAINT fk_notification_deliveries_notification_intents_notification_i
                FOREIGN KEY (notification_intent_id) REFERENCES notification_intents (id) ON DELETE RESTRICT;
            ALTER TABLE notification_intents
                ADD CONSTRAINT fk_notification_intents_users_user_id
                FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT;
            ALTER TABLE notification_external_delegations
                ADD CONSTRAINT fk_notification_external_delegations_notification_intents_noti
                FOREIGN KEY (notification_intent_id) REFERENCES notification_intents (id) ON DELETE RESTRICT;
            """);
    }
}
