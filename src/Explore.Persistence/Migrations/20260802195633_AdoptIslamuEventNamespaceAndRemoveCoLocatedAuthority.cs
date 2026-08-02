using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdoptIslamuEventNamespaceAndRemoveCoLocatedAuthority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_privacy_erasure_policy_coverage_privacy_erasure_intents_int",
                table: "privacy_erasure_policy_coverage");

            migrationBuilder.DropForeignKey(
                name: "fk_privacy_erasure_sagas_privacy_erasure_intents_intent_id",
                table: "privacy_erasure_sagas");

            migrationBuilder.DropForeignKey(
                name: "fk_registration_answers_registration_ticket_assignments_tenant",
                table: "registration_answers");

            migrationBuilder.DropTable(
                name: "authority_counter",
                schema: "privacy_erasure_authority");

            migrationBuilder.DropTable(
                name: "erasure_intents",
                schema: "privacy_erasure_authority");

            migrationBuilder.DropIndex(
                name: "ix_registration_answers_tenant_id_registration_order_id_ticket",
                table: "registration_answers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_registration_answers_subject_shape",
                table: "registration_answers");

            migrationBuilder.EnsureSchema(
                name: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_retention_subject_kinds",
                newName: "webhook_retention_subject_kinds",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_retention_holds",
                newName: "webhook_retention_holds",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_provider_publications",
                newName: "webhook_provider_publications",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_provider_publication_statuses",
                newName: "webhook_provider_publication_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_provider_publication_attempts",
                newName: "webhook_provider_publication_attempts",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_provider_publication_attempt_outcomes",
                newName: "webhook_provider_publication_attempt_outcomes",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_provider_modes",
                newName: "webhook_provider_modes",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_provider_kinds",
                newName: "webhook_provider_kinds",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_provider_capabilities",
                newName: "webhook_provider_capabilities",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_provider_binding_verification_states",
                newName: "webhook_provider_binding_verification_states",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_pending_work_decisions",
                newName: "webhook_pending_work_decisions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_payload_provenances",
                newName: "webhook_payload_provenances",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_messages",
                newName: "webhook_messages",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_local_target_snapshots",
                newName: "webhook_local_target_snapshots",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_local_delivery_statuses",
                newName: "webhook_local_delivery_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_event_types",
                newName: "webhook_event_types",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_endpoints",
                newName: "webhook_endpoints",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_endpoint_subscriptions",
                newName: "webhook_endpoint_subscriptions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_endpoint_statuses",
                newName: "webhook_endpoint_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_delivery_plan_snapshots",
                newName: "webhook_delivery_plan_snapshots",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_delivery_attempts",
                newName: "webhook_delivery_attempts",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_delivery_attempt_outcomes",
                newName: "webhook_delivery_attempt_outcomes",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_consumers",
                newName: "webhook_consumers",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_consumer_statuses",
                newName: "webhook_consumer_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_consumer_provider_bindings",
                newName: "webhook_consumer_provider_bindings",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_consumer_kinds",
                newName: "webhook_consumer_kinds",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_bulk_replay_statuses",
                newName: "webhook_bulk_replay_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_bulk_replay_operations",
                newName: "webhook_bulk_replay_operations",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_audit_target_kinds",
                newName: "webhook_audit_target_kinds",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_audit_scope_kinds",
                newName: "webhook_audit_scope_kinds",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_audit_principal_kinds",
                newName: "webhook_audit_principal_kinds",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_audit_outcomes",
                newName: "webhook_audit_outcomes",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_audit_events",
                newName: "webhook_audit_events",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "webhook_audit_actions",
                newName: "webhook_audit_actions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "web_push_subscriptions",
                newName: "web_push_subscriptions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "web_push_dispatch_outbox",
                newName: "web_push_dispatch_outbox",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "visibility_types",
                newName: "visibility_types",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "users",
                newName: "users",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "user_preferences",
                newName: "user_preferences",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "user_pii",
                newName: "user_pii",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "user_notification_preferences",
                newName: "user_notification_preferences",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "user_external_logins",
                newName: "user_external_logins",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "user_authentication_tokens",
                newName: "user_authentication_tokens",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "user_appearance_profiles",
                newName: "user_appearance_profiles",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "user_appearance_preferences",
                newName: "user_appearance_preferences",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "ui_themes",
                newName: "ui_themes",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "ui_theme_presets",
                newName: "ui_theme_presets",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "ticket_type_entitlements",
                newName: "ticket_type_entitlements",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "ticket_pricing_modes",
                newName: "ticket_pricing_modes",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "ticket_catalog_statuses",
                newName: "ticket_catalog_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "tenants",
                newName: "tenants",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "tenant_users",
                newName: "tenant_users",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "tenant_user_role_grants",
                newName: "tenant_user_role_grants",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "tenant_user_profiles",
                newName: "tenant_user_profiles",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "tenant_statuses",
                newName: "tenant_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "tenant_settings_documents",
                newName: "tenant_settings_documents",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "tenant_setting_overrides",
                newName: "tenant_setting_overrides",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "tenant_policy_sets",
                newName: "tenant_policy_sets",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "tenant_plans",
                newName: "tenant_plans",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "tenant_plan_versions",
                newName: "tenant_plan_versions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "tenant_plan_version_settings",
                newName: "tenant_plan_version_settings",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "tenant_plan_version_quotas",
                newName: "tenant_plan_version_quotas",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "tenant_plan_statuses",
                newName: "tenant_plan_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "tenant_plan_assignments",
                newName: "tenant_plan_assignments",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "tenant_plan_assignment_statuses",
                newName: "tenant_plan_assignment_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "tenant_plan_application_statuses",
                newName: "tenant_plan_application_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "tenant_plan_application_logs",
                newName: "tenant_plan_application_logs",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "tenant_onboarding_states",
                newName: "tenant_onboarding_states",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "tenant_navigation_links",
                newName: "tenant_navigation_links",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "tenant_lifecycle_logs",
                newName: "tenant_lifecycle_logs",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "tenant_invitations",
                newName: "tenant_invitations",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "tenant_footer_links",
                newName: "tenant_footer_links",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "tenant_footer_link_groups",
                newName: "tenant_footer_link_groups",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "tenant_capabilities",
                newName: "tenant_capabilities",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "tags",
                newName: "tags",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "tag_types",
                newName: "tag_types",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "tag_type_tags",
                newName: "tag_type_tags",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "system_settings",
                newName: "system_settings",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "sync_states",
                newName: "sync_states",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "support_access_sessions",
                newName: "support_access_sessions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "support_access_session_statuses",
                newName: "support_access_session_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "support_access_modes",
                newName: "support_access_modes",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "support_access_end_reasons",
                newName: "support_access_end_reasons",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "support_access_audit_events",
                newName: "support_access_audit_events",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "support_access_audit_event_types",
                newName: "support_access_audit_event_types",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "storage_usage_counters",
                newName: "storage_usage_counters",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "storage_upload_sessions",
                newName: "storage_upload_sessions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "storage_objects",
                newName: "storage_objects",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "setting_value_types",
                newName: "setting_value_types",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "setting_scopes",
                newName: "setting_scopes",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "service_principals",
                newName: "service_principals",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "secret_validation_statuses",
                newName: "secret_validation_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "secret_source_types",
                newName: "secret_source_types",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "secret_bindings",
                newName: "secret_bindings",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "schedule_item_kinds",
                newName: "schedule_item_kinds",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "roles",
                newName: "roles",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "role_scopes",
                newName: "role_scopes",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "role_permissions",
                newName: "role_permissions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_workflows",
                newName: "registration_workflows",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_ticket_assignments",
                newName: "registration_ticket_assignments",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_submissions",
                newName: "registration_submissions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_submission_statuses",
                newName: "registration_submission_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_submission_revisions",
                newName: "registration_submission_revisions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_sensitive_answer_values",
                newName: "registration_sensitive_answer_values",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_scopes",
                newName: "registration_scopes",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_requirements",
                newName: "registration_requirements",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_requirement_subject_types",
                newName: "registration_requirement_subject_types",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_requirement_criticalities",
                newName: "registration_requirement_criticalities",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_requirement_completion_effects",
                newName: "registration_requirement_completion_effects",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_participants",
                newName: "registration_participants",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_participant_pii",
                newName: "registration_participant_pii",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_organizer_visibilities",
                newName: "registration_organizer_visibilities",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_orders",
                newName: "registration_orders",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_order_statuses",
                newName: "registration_order_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_order_platform_contributions",
                newName: "registration_order_platform_contributions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_order_pii",
                newName: "registration_order_pii",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_order_lines",
                newName: "registration_order_lines",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_modes",
                newName: "registration_modes",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_inventory_holds",
                newName: "registration_inventory_holds",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_inventory_hold_statuses",
                newName: "registration_inventory_hold_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_forms",
                newName: "registration_forms",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_form_versions",
                newName: "registration_form_versions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_form_statuses",
                newName: "registration_form_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_form_sections",
                newName: "registration_form_sections",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_form_rules",
                newName: "registration_form_rules",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_form_fields",
                newName: "registration_form_fields",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_form_field_options",
                newName: "registration_form_field_options",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_field_types",
                newName: "registration_field_types",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_channels",
                newName: "registration_channels",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_attempts",
                newName: "registration_attempts",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_attempt_statuses",
                newName: "registration_attempt_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_answers",
                newName: "registration_answers",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_answer_sync_modes",
                newName: "registration_answer_sync_modes",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "registration_answer_subject_types",
                newName: "registration_answer_subject_types",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "privacy_erasure_sagas",
                newName: "privacy_erasure_sagas",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "privacy_erasure_replay_checkpoints",
                newName: "privacy_erasure_replay_checkpoints",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "privacy_erasure_provider_work",
                newName: "privacy_erasure_provider_work",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "privacy_erasure_policy_coverage",
                newName: "privacy_erasure_policy_coverage",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "policy_change_outbox",
                newName: "policy_change_outbox",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "platform_user_roles",
                newName: "platform_user_roles",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "platform_fee_policies",
                newName: "platform_fee_policies",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "platform_fee_fixed_charges",
                newName: "platform_fee_fixed_charges",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "platform_contribution_settings",
                newName: "platform_contribution_settings",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "platform_contribution_options",
                newName: "platform_contribution_options",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "permissions",
                newName: "permissions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "pds_sync_outbox",
                newName: "pds_sync_outbox",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "participation_requirement_attachments",
                newName: "participation_requirement_attachments",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "participation_handling_modes",
                newName: "participation_handling_modes",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "participant_types",
                newName: "participant_types",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "participant_data_collection_modes",
                newName: "participant_data_collection_modes",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "owner_types",
                newName: "owner_types",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "outbox_messages",
                newName: "outbox_messages",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "organizations",
                newName: "organizations",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "organization_tenants",
                newName: "organization_tenants",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "organization_tenant_evidence",
                newName: "organization_tenant_evidence",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "organization_setting_overrides",
                newName: "organization_setting_overrides",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "organization_reviews",
                newName: "organization_reviews",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "organization_positions",
                newName: "organization_positions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "organization_policy_sets",
                newName: "organization_policy_sets",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "organization_pii",
                newName: "organization_pii",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "organization_members",
                newName: "organization_members",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "notifications",
                newName: "notifications",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "notification_types",
                newName: "notification_types",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "notification_scope_types",
                newName: "notification_scope_types",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "notification_recipient_kinds",
                newName: "notification_recipient_kinds",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "notification_reasons",
                newName: "notification_reasons",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "notification_preference_profiles",
                newName: "notification_preference_profiles",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "notification_preference_channels",
                newName: "notification_preference_channels",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "notification_preference_categories",
                newName: "notification_preference_categories",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "notification_ownership_types",
                newName: "notification_ownership_types",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "notification_intents",
                newName: "notification_intents",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "notification_intent_statuses",
                newName: "notification_intent_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "notification_fanout_runs",
                newName: "notification_fanout_runs",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "notification_fanout_processor_states",
                newName: "notification_fanout_processor_states",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "notification_fanout_occurrences",
                newName: "notification_fanout_occurrences",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "notification_external_delegations",
                newName: "notification_external_delegations",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "notification_external_delegation_statuses",
                newName: "notification_external_delegation_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "notification_entity_types",
                newName: "notification_entity_types",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "notification_delivery_statuses",
                newName: "notification_delivery_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "notification_delivery_policies",
                newName: "notification_delivery_policies",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "notification_deliveries",
                newName: "notification_deliveries",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "notification_channel_preferences",
                newName: "notification_channel_preferences",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "notification_categories",
                newName: "notification_categories",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "module_definitions",
                newName: "module_definitions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "managed_tenant_provisioning_operations",
                newName: "managed_tenant_provisioning_operations",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "managed_control_plane_registrations",
                newName: "managed_control_plane_registrations",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "madhabs",
                newName: "madhabs",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "locations",
                newName: "locations",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "location_rooms",
                newName: "location_rooms",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "location_privacy_states",
                newName: "location_privacy_states",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "location_pii",
                newName: "location_pii",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "location_kinds",
                newName: "location_kinds",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "location_disclosure_audiences",
                newName: "location_disclosure_audiences",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "languages",
                newName: "languages",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "integration_sync_outbox",
                newName: "integration_sync_outbox",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "instance_policy_sets",
                newName: "instance_policy_sets",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "instance_bootstrap_states",
                newName: "instance_bootstrap_states",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "incoming_webhook_settlement_sources",
                newName: "incoming_webhook_settlement_sources",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "incoming_webhook_redrive_results",
                newName: "incoming_webhook_redrive_results",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "incoming_webhook_redrive_records",
                newName: "incoming_webhook_redrive_records",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "incoming_webhook_processing_attempts",
                newName: "incoming_webhook_processing_attempts",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "incoming_webhook_processing_attempt_outcomes",
                newName: "incoming_webhook_processing_attempt_outcomes",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "incoming_webhook_messages",
                newName: "incoming_webhook_messages",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "incoming_webhook_message_statuses",
                newName: "incoming_webhook_message_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "incoming_webhook_effect_receipts",
                newName: "incoming_webhook_effect_receipts",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "incoming_webhook_effect_outbox",
                newName: "incoming_webhook_effect_outbox",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "identity_access_modes",
                newName: "identity_access_modes",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "idempotency_records",
                newName: "idempotency_records",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "groups",
                newName: "groups",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "group_tenants",
                newName: "group_tenants",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "group_setting_overrides",
                newName: "group_setting_overrides",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "group_positions",
                newName: "group_positions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "group_members",
                newName: "group_members",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "file_types",
                newName: "file_types",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "external_workflow_provider_kinds",
                newName: "external_workflow_provider_kinds",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "external_bindings",
                newName: "external_bindings",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "external_api_keys",
                newName: "external_api_keys",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "external_api_key_statuses",
                newName: "external_api_key_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "external_api_key_quotas",
                newName: "external_api_key_quotas",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "external_api_key_owner_types",
                newName: "external_api_key_owner_types",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "external_api_key_credit_periods",
                newName: "external_api_key_credit_periods",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "external_actor_subjects",
                newName: "external_actor_subjects",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "events",
                newName: "events",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_types",
                newName: "event_types",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_ticket_types",
                newName: "event_ticket_types",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_ticket_catalog_versions",
                newName: "event_ticket_catalog_versions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_templates",
                newName: "event_templates",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_template_custom_property_options",
                newName: "event_template_custom_property_options",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_template_custom_property_definitions",
                newName: "event_template_custom_property_definitions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_tech_aspects",
                newName: "event_tech_aspects",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_tags",
                newName: "event_tags",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_statuses",
                newName: "event_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_sessions",
                newName: "event_sessions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_session_templates",
                newName: "event_session_templates",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_session_template_custom_property_options",
                newName: "event_session_template_custom_property_options",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_session_template_custom_property_definitions",
                newName: "event_session_template_custom_property_definitions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_session_tags",
                newName: "event_session_tags",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_session_statuses",
                newName: "event_session_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_session_speakers",
                newName: "event_session_speakers",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_session_languages",
                newName: "event_session_languages",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_session_kinds",
                newName: "event_session_kinds",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_session_islamic_aspects",
                newName: "event_session_islamic_aspects",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_session_groups",
                newName: "event_session_groups",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_session_group_sessions",
                newName: "event_session_group_sessions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_session_custom_property_values",
                newName: "event_session_custom_property_values",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_session_custom_property_projections",
                newName: "event_session_custom_property_projections",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_session_custom_property_options",
                newName: "event_session_custom_property_options",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_session_custom_property_definitions",
                newName: "event_session_custom_property_definitions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_session_categories",
                newName: "event_session_categories",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_session_agenda_items",
                newName: "event_session_agenda_items",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_series",
                newName: "event_series",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_role_assignments",
                newName: "event_role_assignments",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_reports",
                newName: "event_reports",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_report_targets",
                newName: "event_report_targets",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_report_signals",
                newName: "event_report_signals",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_report_external_links",
                newName: "event_report_external_links",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_report_evidence",
                newName: "event_report_evidence",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_report_decisions",
                newName: "event_report_decisions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_report_decision_executions",
                newName: "event_report_decision_executions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_report_cases",
                newName: "event_report_cases",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_registrations",
                newName: "event_registrations",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_registration_policies",
                newName: "event_registration_policies",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_public_actions",
                newName: "event_public_actions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_public_action_kinds",
                newName: "event_public_action_kinds",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_public_action_health_states",
                newName: "event_public_action_health_states",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_provenance_types",
                newName: "event_provenance_types",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_participation_configurations",
                newName: "event_participation_configurations",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_organizer_claims",
                newName: "event_organizer_claims",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_organizer_claim_statuses",
                newName: "event_organizer_claim_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_moderation_records",
                newName: "event_moderation_records",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_locations",
                newName: "event_locations",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_location_exact_read_audits",
                newName: "event_location_exact_read_audits",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_location_disclosure_audits",
                newName: "event_location_disclosure_audits",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_islamic_aspects",
                newName: "event_islamic_aspects",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_formats",
                newName: "event_formats",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_days",
                newName: "event_days",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_custom_property_values",
                newName: "event_custom_property_values",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_custom_property_projections",
                newName: "event_custom_property_projections",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_custom_property_options",
                newName: "event_custom_property_options",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_custom_property_definitions",
                newName: "event_custom_property_definitions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_contact_share_exports",
                newName: "event_contact_share_exports",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_contact_share_export_items",
                newName: "event_contact_share_export_items",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_contact_share_consents",
                newName: "event_contact_share_consents",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_categories",
                newName: "event_categories",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_capacity_pools",
                newName: "event_capacity_pools",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "event_agenda_items",
                newName: "event_agenda_items",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "entitlement_selection_rules",
                newName: "entitlement_selection_rules",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "entitlement_scope_types",
                newName: "entitlement_scope_types",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "email_dispatch_tenant_controls",
                newName: "email_dispatch_tenant_controls",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "email_dispatch_receipts",
                newName: "email_dispatch_receipts",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "email_dispatch_processor_states",
                newName: "email_dispatch_processor_states",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "email_dispatch_outbox",
                newName: "email_dispatch_outbox",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "email_dispatch_attempts",
                newName: "email_dispatch_attempts",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "did_custody_types",
                newName: "did_custody_types",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "custom_property_values",
                newName: "custom_property_values",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "custom_property_projection_status",
                newName: "custom_property_projection_status",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "custom_property_projection_dirty_scope",
                newName: "custom_property_projection_dirty_scope",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "custom_property_options",
                newName: "custom_property_options",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "custom_property_definitions",
                newName: "custom_property_definitions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "configuration_change_logs",
                newName: "configuration_change_logs",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "category_types",
                newName: "category_types",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "category_type_categories",
                newName: "category_type_categories",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "categories",
                newName: "categories",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "capacity_oversell_policies",
                newName: "capacity_oversell_policies",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "capacity_hold_policies",
                newName: "capacity_hold_policies",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "booking_party_types",
                newName: "booking_party_types",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "audit_logs",
                newName: "audit_logs",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "audience_genders",
                newName: "audience_genders",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "audience_ages",
                newName: "audience_ages",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "atproto_records",
                newName: "atproto_records",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "atproto_record_tenant_presentations",
                newName: "atproto_record_tenant_presentations",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "atproto_outbound_record_ownerships",
                newName: "atproto_outbound_record_ownerships",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "atproto_jetstream_quarantines",
                newName: "atproto_jetstream_quarantines",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "atproto_jetstream_consumer_states",
                newName: "atproto_jetstream_consumer_states",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "atproto_identity_moderation_records",
                newName: "atproto_identity_moderation_records",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "atproto_identities",
                newName: "atproto_identities",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "atproto_event_projections",
                newName: "atproto_event_projections",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "assignment_statuses",
                newName: "assignment_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "approval_statuses",
                newName: "approval_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "app_settings",
                newName: "app_settings",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "analytics_providers",
                newName: "analytics_providers",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "ai_tool_executions",
                newName: "ai_tool_executions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "ai_runs",
                newName: "ai_runs",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "ai_run_statuses",
                newName: "ai_run_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "ai_reference_kinds",
                newName: "ai_reference_kinds",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "ai_provider_kinds",
                newName: "ai_provider_kinds",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "ai_proposed_actions",
                newName: "ai_proposed_actions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "ai_proposed_action_statuses",
                newName: "ai_proposed_action_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "ai_proposed_action_kinds",
                newName: "ai_proposed_action_kinds",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "ai_messages",
                newName: "ai_messages",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "ai_message_roles",
                newName: "ai_message_roles",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "ai_conversations",
                newName: "ai_conversations",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "ai_conversation_statuses",
                newName: "ai_conversation_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "ai_conversation_references",
                newName: "ai_conversation_references",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "ai_consent_grants",
                newName: "ai_consent_grants",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "advance_registration_obligations",
                newName: "advance_registration_obligations",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "actors",
                newName: "actors",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "actor_types",
                newName: "actor_types",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "actor_subscriptions",
                newName: "actor_subscriptions",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "actor_subscription_statuses",
                newName: "actor_subscription_statuses",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "actor_subscription_notification_levels",
                newName: "actor_subscription_notification_levels",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "actor_pii",
                newName: "actor_pii",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "actor_moderation_records",
                newName: "actor_moderation_records",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "actor_merges",
                newName: "actor_merges",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "actor_key_stores",
                newName: "actor_key_stores",
                newSchema: "islamu_event");

            migrationBuilder.RenameTable(
                name: "account_authority_kinds",
                newName: "account_authority_kinds",
                newSchema: "islamu_event");

            migrationBuilder.AddColumn<Guid>(
                name: "ticket_assignment_order_line_id",
                schema: "islamu_event",
                table: "registration_answers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_registration_ticket_assignments_tenant_id_registration_orde1",
                schema: "islamu_event",
                table: "registration_ticket_assignments",
                columns: new[] { "tenant_id", "registration_order_id", "id", "registration_order_line_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_registration_order_lines_tenant_id_registration_order_id_id1",
                schema: "islamu_event",
                table: "registration_order_lines",
                columns: new[] { "tenant_id", "registration_order_id", "id", "ticket_type_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_answers_tenant_id_registration_order_id_ticket",
                schema: "islamu_event",
                table: "registration_answers",
                columns: new[] { "tenant_id", "registration_order_id", "ticket_assignment_order_line_id", "requirement_subject_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_answers_tenant_id_registration_order_id_ticket1",
                schema: "islamu_event",
                table: "registration_answers",
                columns: new[] { "tenant_id", "registration_order_id", "ticket_assignment_subject_id", "ticket_assignment_order_line_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_registration_answers_subject_shape",
                schema: "islamu_event",
                table: "registration_answers",
                sql: "num_nonnulls(order_subject_id, purchaser_subject_id, participant_subject_id, ticket_assignment_subject_id, session_selection_subject_id) = 1 AND ((answer_subject_type_id = 1 AND order_subject_id = registration_order_id AND ticket_assignment_order_line_id IS NULL AND requirement_subject_type_id = 1) OR (answer_subject_type_id = 2 AND purchaser_subject_id = registration_order_id AND ticket_assignment_order_line_id IS NULL AND requirement_subject_type_id IN (1, 4)) OR (answer_subject_type_id = 3 AND participant_subject_id IS NOT NULL AND ticket_assignment_order_line_id IS NULL AND requirement_subject_type_id IN (3, 5)) OR (answer_subject_type_id = 4 AND ticket_assignment_subject_id IS NOT NULL AND ticket_assignment_order_line_id IS NOT NULL AND requirement_subject_id IS NOT NULL AND requirement_subject_type_id = 2) OR (answer_subject_type_id = 5 AND session_selection_subject_id = requirement_subject_id AND ticket_assignment_order_line_id IS NULL AND requirement_subject_type_id = 6))");

            migrationBuilder.AddForeignKey(
                name: "fk_registration_answers_registration_order_lines_tenant_id_reg",
                schema: "islamu_event",
                table: "registration_answers",
                columns: new[] { "tenant_id", "registration_order_id", "ticket_assignment_order_line_id", "requirement_subject_id" },
                principalSchema: "islamu_event",
                principalTable: "registration_order_lines",
                principalColumns: new[] { "tenant_id", "registration_order_id", "id", "ticket_type_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_registration_answers_registration_ticket_assignments_tenant",
                schema: "islamu_event",
                table: "registration_answers",
                columns: new[] { "tenant_id", "registration_order_id", "ticket_assignment_subject_id", "ticket_assignment_order_line_id" },
                principalSchema: "islamu_event",
                principalTable: "registration_ticket_assignments",
                principalColumns: new[] { "tenant_id", "registration_order_id", "id", "registration_order_line_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_registration_answers_registration_order_lines_tenant_id_reg",
                schema: "islamu_event",
                table: "registration_answers");

            migrationBuilder.DropForeignKey(
                name: "fk_registration_answers_registration_ticket_assignments_tenant",
                schema: "islamu_event",
                table: "registration_answers");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_registration_ticket_assignments_tenant_id_registration_orde1",
                schema: "islamu_event",
                table: "registration_ticket_assignments");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_registration_order_lines_tenant_id_registration_order_id_id1",
                schema: "islamu_event",
                table: "registration_order_lines");

            migrationBuilder.DropIndex(
                name: "ix_registration_answers_tenant_id_registration_order_id_ticket",
                schema: "islamu_event",
                table: "registration_answers");

            migrationBuilder.DropIndex(
                name: "ix_registration_answers_tenant_id_registration_order_id_ticket1",
                schema: "islamu_event",
                table: "registration_answers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_registration_answers_subject_shape",
                schema: "islamu_event",
                table: "registration_answers");

            migrationBuilder.DropColumn(
                name: "ticket_assignment_order_line_id",
                schema: "islamu_event",
                table: "registration_answers");

            migrationBuilder.EnsureSchema(
                name: "privacy_erasure_authority");

            migrationBuilder.RenameTable(
                name: "webhook_retention_subject_kinds",
                schema: "islamu_event",
                newName: "webhook_retention_subject_kinds");

            migrationBuilder.RenameTable(
                name: "webhook_retention_holds",
                schema: "islamu_event",
                newName: "webhook_retention_holds");

            migrationBuilder.RenameTable(
                name: "webhook_provider_publications",
                schema: "islamu_event",
                newName: "webhook_provider_publications");

            migrationBuilder.RenameTable(
                name: "webhook_provider_publication_statuses",
                schema: "islamu_event",
                newName: "webhook_provider_publication_statuses");

            migrationBuilder.RenameTable(
                name: "webhook_provider_publication_attempts",
                schema: "islamu_event",
                newName: "webhook_provider_publication_attempts");

            migrationBuilder.RenameTable(
                name: "webhook_provider_publication_attempt_outcomes",
                schema: "islamu_event",
                newName: "webhook_provider_publication_attempt_outcomes");

            migrationBuilder.RenameTable(
                name: "webhook_provider_modes",
                schema: "islamu_event",
                newName: "webhook_provider_modes");

            migrationBuilder.RenameTable(
                name: "webhook_provider_kinds",
                schema: "islamu_event",
                newName: "webhook_provider_kinds");

            migrationBuilder.RenameTable(
                name: "webhook_provider_capabilities",
                schema: "islamu_event",
                newName: "webhook_provider_capabilities");

            migrationBuilder.RenameTable(
                name: "webhook_provider_binding_verification_states",
                schema: "islamu_event",
                newName: "webhook_provider_binding_verification_states");

            migrationBuilder.RenameTable(
                name: "webhook_pending_work_decisions",
                schema: "islamu_event",
                newName: "webhook_pending_work_decisions");

            migrationBuilder.RenameTable(
                name: "webhook_payload_provenances",
                schema: "islamu_event",
                newName: "webhook_payload_provenances");

            migrationBuilder.RenameTable(
                name: "webhook_messages",
                schema: "islamu_event",
                newName: "webhook_messages");

            migrationBuilder.RenameTable(
                name: "webhook_local_target_snapshots",
                schema: "islamu_event",
                newName: "webhook_local_target_snapshots");

            migrationBuilder.RenameTable(
                name: "webhook_local_delivery_statuses",
                schema: "islamu_event",
                newName: "webhook_local_delivery_statuses");

            migrationBuilder.RenameTable(
                name: "webhook_event_types",
                schema: "islamu_event",
                newName: "webhook_event_types");

            migrationBuilder.RenameTable(
                name: "webhook_endpoints",
                schema: "islamu_event",
                newName: "webhook_endpoints");

            migrationBuilder.RenameTable(
                name: "webhook_endpoint_subscriptions",
                schema: "islamu_event",
                newName: "webhook_endpoint_subscriptions");

            migrationBuilder.RenameTable(
                name: "webhook_endpoint_statuses",
                schema: "islamu_event",
                newName: "webhook_endpoint_statuses");

            migrationBuilder.RenameTable(
                name: "webhook_delivery_plan_snapshots",
                schema: "islamu_event",
                newName: "webhook_delivery_plan_snapshots");

            migrationBuilder.RenameTable(
                name: "webhook_delivery_attempts",
                schema: "islamu_event",
                newName: "webhook_delivery_attempts");

            migrationBuilder.RenameTable(
                name: "webhook_delivery_attempt_outcomes",
                schema: "islamu_event",
                newName: "webhook_delivery_attempt_outcomes");

            migrationBuilder.RenameTable(
                name: "webhook_consumers",
                schema: "islamu_event",
                newName: "webhook_consumers");

            migrationBuilder.RenameTable(
                name: "webhook_consumer_statuses",
                schema: "islamu_event",
                newName: "webhook_consumer_statuses");

            migrationBuilder.RenameTable(
                name: "webhook_consumer_provider_bindings",
                schema: "islamu_event",
                newName: "webhook_consumer_provider_bindings");

            migrationBuilder.RenameTable(
                name: "webhook_consumer_kinds",
                schema: "islamu_event",
                newName: "webhook_consumer_kinds");

            migrationBuilder.RenameTable(
                name: "webhook_bulk_replay_statuses",
                schema: "islamu_event",
                newName: "webhook_bulk_replay_statuses");

            migrationBuilder.RenameTable(
                name: "webhook_bulk_replay_operations",
                schema: "islamu_event",
                newName: "webhook_bulk_replay_operations");

            migrationBuilder.RenameTable(
                name: "webhook_audit_target_kinds",
                schema: "islamu_event",
                newName: "webhook_audit_target_kinds");

            migrationBuilder.RenameTable(
                name: "webhook_audit_scope_kinds",
                schema: "islamu_event",
                newName: "webhook_audit_scope_kinds");

            migrationBuilder.RenameTable(
                name: "webhook_audit_principal_kinds",
                schema: "islamu_event",
                newName: "webhook_audit_principal_kinds");

            migrationBuilder.RenameTable(
                name: "webhook_audit_outcomes",
                schema: "islamu_event",
                newName: "webhook_audit_outcomes");

            migrationBuilder.RenameTable(
                name: "webhook_audit_events",
                schema: "islamu_event",
                newName: "webhook_audit_events");

            migrationBuilder.RenameTable(
                name: "webhook_audit_actions",
                schema: "islamu_event",
                newName: "webhook_audit_actions");

            migrationBuilder.RenameTable(
                name: "web_push_subscriptions",
                schema: "islamu_event",
                newName: "web_push_subscriptions");

            migrationBuilder.RenameTable(
                name: "web_push_dispatch_outbox",
                schema: "islamu_event",
                newName: "web_push_dispatch_outbox");

            migrationBuilder.RenameTable(
                name: "visibility_types",
                schema: "islamu_event",
                newName: "visibility_types");

            migrationBuilder.RenameTable(
                name: "users",
                schema: "islamu_event",
                newName: "users");

            migrationBuilder.RenameTable(
                name: "user_preferences",
                schema: "islamu_event",
                newName: "user_preferences");

            migrationBuilder.RenameTable(
                name: "user_pii",
                schema: "islamu_event",
                newName: "user_pii");

            migrationBuilder.RenameTable(
                name: "user_notification_preferences",
                schema: "islamu_event",
                newName: "user_notification_preferences");

            migrationBuilder.RenameTable(
                name: "user_external_logins",
                schema: "islamu_event",
                newName: "user_external_logins");

            migrationBuilder.RenameTable(
                name: "user_authentication_tokens",
                schema: "islamu_event",
                newName: "user_authentication_tokens");

            migrationBuilder.RenameTable(
                name: "user_appearance_profiles",
                schema: "islamu_event",
                newName: "user_appearance_profiles");

            migrationBuilder.RenameTable(
                name: "user_appearance_preferences",
                schema: "islamu_event",
                newName: "user_appearance_preferences");

            migrationBuilder.RenameTable(
                name: "ui_themes",
                schema: "islamu_event",
                newName: "ui_themes");

            migrationBuilder.RenameTable(
                name: "ui_theme_presets",
                schema: "islamu_event",
                newName: "ui_theme_presets");

            migrationBuilder.RenameTable(
                name: "ticket_type_entitlements",
                schema: "islamu_event",
                newName: "ticket_type_entitlements");

            migrationBuilder.RenameTable(
                name: "ticket_pricing_modes",
                schema: "islamu_event",
                newName: "ticket_pricing_modes");

            migrationBuilder.RenameTable(
                name: "ticket_catalog_statuses",
                schema: "islamu_event",
                newName: "ticket_catalog_statuses");

            migrationBuilder.RenameTable(
                name: "tenants",
                schema: "islamu_event",
                newName: "tenants");

            migrationBuilder.RenameTable(
                name: "tenant_users",
                schema: "islamu_event",
                newName: "tenant_users");

            migrationBuilder.RenameTable(
                name: "tenant_user_role_grants",
                schema: "islamu_event",
                newName: "tenant_user_role_grants");

            migrationBuilder.RenameTable(
                name: "tenant_user_profiles",
                schema: "islamu_event",
                newName: "tenant_user_profiles");

            migrationBuilder.RenameTable(
                name: "tenant_statuses",
                schema: "islamu_event",
                newName: "tenant_statuses");

            migrationBuilder.RenameTable(
                name: "tenant_settings_documents",
                schema: "islamu_event",
                newName: "tenant_settings_documents");

            migrationBuilder.RenameTable(
                name: "tenant_setting_overrides",
                schema: "islamu_event",
                newName: "tenant_setting_overrides");

            migrationBuilder.RenameTable(
                name: "tenant_policy_sets",
                schema: "islamu_event",
                newName: "tenant_policy_sets");

            migrationBuilder.RenameTable(
                name: "tenant_plans",
                schema: "islamu_event",
                newName: "tenant_plans");

            migrationBuilder.RenameTable(
                name: "tenant_plan_versions",
                schema: "islamu_event",
                newName: "tenant_plan_versions");

            migrationBuilder.RenameTable(
                name: "tenant_plan_version_settings",
                schema: "islamu_event",
                newName: "tenant_plan_version_settings");

            migrationBuilder.RenameTable(
                name: "tenant_plan_version_quotas",
                schema: "islamu_event",
                newName: "tenant_plan_version_quotas");

            migrationBuilder.RenameTable(
                name: "tenant_plan_statuses",
                schema: "islamu_event",
                newName: "tenant_plan_statuses");

            migrationBuilder.RenameTable(
                name: "tenant_plan_assignments",
                schema: "islamu_event",
                newName: "tenant_plan_assignments");

            migrationBuilder.RenameTable(
                name: "tenant_plan_assignment_statuses",
                schema: "islamu_event",
                newName: "tenant_plan_assignment_statuses");

            migrationBuilder.RenameTable(
                name: "tenant_plan_application_statuses",
                schema: "islamu_event",
                newName: "tenant_plan_application_statuses");

            migrationBuilder.RenameTable(
                name: "tenant_plan_application_logs",
                schema: "islamu_event",
                newName: "tenant_plan_application_logs");

            migrationBuilder.RenameTable(
                name: "tenant_onboarding_states",
                schema: "islamu_event",
                newName: "tenant_onboarding_states");

            migrationBuilder.RenameTable(
                name: "tenant_navigation_links",
                schema: "islamu_event",
                newName: "tenant_navigation_links");

            migrationBuilder.RenameTable(
                name: "tenant_lifecycle_logs",
                schema: "islamu_event",
                newName: "tenant_lifecycle_logs");

            migrationBuilder.RenameTable(
                name: "tenant_invitations",
                schema: "islamu_event",
                newName: "tenant_invitations");

            migrationBuilder.RenameTable(
                name: "tenant_footer_links",
                schema: "islamu_event",
                newName: "tenant_footer_links");

            migrationBuilder.RenameTable(
                name: "tenant_footer_link_groups",
                schema: "islamu_event",
                newName: "tenant_footer_link_groups");

            migrationBuilder.RenameTable(
                name: "tenant_capabilities",
                schema: "islamu_event",
                newName: "tenant_capabilities");

            migrationBuilder.RenameTable(
                name: "tags",
                schema: "islamu_event",
                newName: "tags");

            migrationBuilder.RenameTable(
                name: "tag_types",
                schema: "islamu_event",
                newName: "tag_types");

            migrationBuilder.RenameTable(
                name: "tag_type_tags",
                schema: "islamu_event",
                newName: "tag_type_tags");

            migrationBuilder.RenameTable(
                name: "system_settings",
                schema: "islamu_event",
                newName: "system_settings");

            migrationBuilder.RenameTable(
                name: "sync_states",
                schema: "islamu_event",
                newName: "sync_states");

            migrationBuilder.RenameTable(
                name: "support_access_sessions",
                schema: "islamu_event",
                newName: "support_access_sessions");

            migrationBuilder.RenameTable(
                name: "support_access_session_statuses",
                schema: "islamu_event",
                newName: "support_access_session_statuses");

            migrationBuilder.RenameTable(
                name: "support_access_modes",
                schema: "islamu_event",
                newName: "support_access_modes");

            migrationBuilder.RenameTable(
                name: "support_access_end_reasons",
                schema: "islamu_event",
                newName: "support_access_end_reasons");

            migrationBuilder.RenameTable(
                name: "support_access_audit_events",
                schema: "islamu_event",
                newName: "support_access_audit_events");

            migrationBuilder.RenameTable(
                name: "support_access_audit_event_types",
                schema: "islamu_event",
                newName: "support_access_audit_event_types");

            migrationBuilder.RenameTable(
                name: "storage_usage_counters",
                schema: "islamu_event",
                newName: "storage_usage_counters");

            migrationBuilder.RenameTable(
                name: "storage_upload_sessions",
                schema: "islamu_event",
                newName: "storage_upload_sessions");

            migrationBuilder.RenameTable(
                name: "storage_objects",
                schema: "islamu_event",
                newName: "storage_objects");

            migrationBuilder.RenameTable(
                name: "setting_value_types",
                schema: "islamu_event",
                newName: "setting_value_types");

            migrationBuilder.RenameTable(
                name: "setting_scopes",
                schema: "islamu_event",
                newName: "setting_scopes");

            migrationBuilder.RenameTable(
                name: "service_principals",
                schema: "islamu_event",
                newName: "service_principals");

            migrationBuilder.RenameTable(
                name: "secret_validation_statuses",
                schema: "islamu_event",
                newName: "secret_validation_statuses");

            migrationBuilder.RenameTable(
                name: "secret_source_types",
                schema: "islamu_event",
                newName: "secret_source_types");

            migrationBuilder.RenameTable(
                name: "secret_bindings",
                schema: "islamu_event",
                newName: "secret_bindings");

            migrationBuilder.RenameTable(
                name: "schedule_item_kinds",
                schema: "islamu_event",
                newName: "schedule_item_kinds");

            migrationBuilder.RenameTable(
                name: "roles",
                schema: "islamu_event",
                newName: "roles");

            migrationBuilder.RenameTable(
                name: "role_scopes",
                schema: "islamu_event",
                newName: "role_scopes");

            migrationBuilder.RenameTable(
                name: "role_permissions",
                schema: "islamu_event",
                newName: "role_permissions");

            migrationBuilder.RenameTable(
                name: "registration_workflows",
                schema: "islamu_event",
                newName: "registration_workflows");

            migrationBuilder.RenameTable(
                name: "registration_ticket_assignments",
                schema: "islamu_event",
                newName: "registration_ticket_assignments");

            migrationBuilder.RenameTable(
                name: "registration_submissions",
                schema: "islamu_event",
                newName: "registration_submissions");

            migrationBuilder.RenameTable(
                name: "registration_submission_statuses",
                schema: "islamu_event",
                newName: "registration_submission_statuses");

            migrationBuilder.RenameTable(
                name: "registration_submission_revisions",
                schema: "islamu_event",
                newName: "registration_submission_revisions");

            migrationBuilder.RenameTable(
                name: "registration_sensitive_answer_values",
                schema: "islamu_event",
                newName: "registration_sensitive_answer_values");

            migrationBuilder.RenameTable(
                name: "registration_scopes",
                schema: "islamu_event",
                newName: "registration_scopes");

            migrationBuilder.RenameTable(
                name: "registration_requirements",
                schema: "islamu_event",
                newName: "registration_requirements");

            migrationBuilder.RenameTable(
                name: "registration_requirement_subject_types",
                schema: "islamu_event",
                newName: "registration_requirement_subject_types");

            migrationBuilder.RenameTable(
                name: "registration_requirement_criticalities",
                schema: "islamu_event",
                newName: "registration_requirement_criticalities");

            migrationBuilder.RenameTable(
                name: "registration_requirement_completion_effects",
                schema: "islamu_event",
                newName: "registration_requirement_completion_effects");

            migrationBuilder.RenameTable(
                name: "registration_participants",
                schema: "islamu_event",
                newName: "registration_participants");

            migrationBuilder.RenameTable(
                name: "registration_participant_pii",
                schema: "islamu_event",
                newName: "registration_participant_pii");

            migrationBuilder.RenameTable(
                name: "registration_organizer_visibilities",
                schema: "islamu_event",
                newName: "registration_organizer_visibilities");

            migrationBuilder.RenameTable(
                name: "registration_orders",
                schema: "islamu_event",
                newName: "registration_orders");

            migrationBuilder.RenameTable(
                name: "registration_order_statuses",
                schema: "islamu_event",
                newName: "registration_order_statuses");

            migrationBuilder.RenameTable(
                name: "registration_order_platform_contributions",
                schema: "islamu_event",
                newName: "registration_order_platform_contributions");

            migrationBuilder.RenameTable(
                name: "registration_order_pii",
                schema: "islamu_event",
                newName: "registration_order_pii");

            migrationBuilder.RenameTable(
                name: "registration_order_lines",
                schema: "islamu_event",
                newName: "registration_order_lines");

            migrationBuilder.RenameTable(
                name: "registration_modes",
                schema: "islamu_event",
                newName: "registration_modes");

            migrationBuilder.RenameTable(
                name: "registration_inventory_holds",
                schema: "islamu_event",
                newName: "registration_inventory_holds");

            migrationBuilder.RenameTable(
                name: "registration_inventory_hold_statuses",
                schema: "islamu_event",
                newName: "registration_inventory_hold_statuses");

            migrationBuilder.RenameTable(
                name: "registration_forms",
                schema: "islamu_event",
                newName: "registration_forms");

            migrationBuilder.RenameTable(
                name: "registration_form_versions",
                schema: "islamu_event",
                newName: "registration_form_versions");

            migrationBuilder.RenameTable(
                name: "registration_form_statuses",
                schema: "islamu_event",
                newName: "registration_form_statuses");

            migrationBuilder.RenameTable(
                name: "registration_form_sections",
                schema: "islamu_event",
                newName: "registration_form_sections");

            migrationBuilder.RenameTable(
                name: "registration_form_rules",
                schema: "islamu_event",
                newName: "registration_form_rules");

            migrationBuilder.RenameTable(
                name: "registration_form_fields",
                schema: "islamu_event",
                newName: "registration_form_fields");

            migrationBuilder.RenameTable(
                name: "registration_form_field_options",
                schema: "islamu_event",
                newName: "registration_form_field_options");

            migrationBuilder.RenameTable(
                name: "registration_field_types",
                schema: "islamu_event",
                newName: "registration_field_types");

            migrationBuilder.RenameTable(
                name: "registration_channels",
                schema: "islamu_event",
                newName: "registration_channels");

            migrationBuilder.RenameTable(
                name: "registration_attempts",
                schema: "islamu_event",
                newName: "registration_attempts");

            migrationBuilder.RenameTable(
                name: "registration_attempt_statuses",
                schema: "islamu_event",
                newName: "registration_attempt_statuses");

            migrationBuilder.RenameTable(
                name: "registration_answers",
                schema: "islamu_event",
                newName: "registration_answers");

            migrationBuilder.RenameTable(
                name: "registration_answer_sync_modes",
                schema: "islamu_event",
                newName: "registration_answer_sync_modes");

            migrationBuilder.RenameTable(
                name: "registration_answer_subject_types",
                schema: "islamu_event",
                newName: "registration_answer_subject_types");

            migrationBuilder.RenameTable(
                name: "privacy_erasure_sagas",
                schema: "islamu_event",
                newName: "privacy_erasure_sagas");

            migrationBuilder.RenameTable(
                name: "privacy_erasure_replay_checkpoints",
                schema: "islamu_event",
                newName: "privacy_erasure_replay_checkpoints");

            migrationBuilder.RenameTable(
                name: "privacy_erasure_provider_work",
                schema: "islamu_event",
                newName: "privacy_erasure_provider_work");

            migrationBuilder.RenameTable(
                name: "privacy_erasure_policy_coverage",
                schema: "islamu_event",
                newName: "privacy_erasure_policy_coverage");

            migrationBuilder.RenameTable(
                name: "policy_change_outbox",
                schema: "islamu_event",
                newName: "policy_change_outbox");

            migrationBuilder.RenameTable(
                name: "platform_user_roles",
                schema: "islamu_event",
                newName: "platform_user_roles");

            migrationBuilder.RenameTable(
                name: "platform_fee_policies",
                schema: "islamu_event",
                newName: "platform_fee_policies");

            migrationBuilder.RenameTable(
                name: "platform_fee_fixed_charges",
                schema: "islamu_event",
                newName: "platform_fee_fixed_charges");

            migrationBuilder.RenameTable(
                name: "platform_contribution_settings",
                schema: "islamu_event",
                newName: "platform_contribution_settings");

            migrationBuilder.RenameTable(
                name: "platform_contribution_options",
                schema: "islamu_event",
                newName: "platform_contribution_options");

            migrationBuilder.RenameTable(
                name: "permissions",
                schema: "islamu_event",
                newName: "permissions");

            migrationBuilder.RenameTable(
                name: "pds_sync_outbox",
                schema: "islamu_event",
                newName: "pds_sync_outbox");

            migrationBuilder.RenameTable(
                name: "participation_requirement_attachments",
                schema: "islamu_event",
                newName: "participation_requirement_attachments");

            migrationBuilder.RenameTable(
                name: "participation_handling_modes",
                schema: "islamu_event",
                newName: "participation_handling_modes");

            migrationBuilder.RenameTable(
                name: "participant_types",
                schema: "islamu_event",
                newName: "participant_types");

            migrationBuilder.RenameTable(
                name: "participant_data_collection_modes",
                schema: "islamu_event",
                newName: "participant_data_collection_modes");

            migrationBuilder.RenameTable(
                name: "owner_types",
                schema: "islamu_event",
                newName: "owner_types");

            migrationBuilder.RenameTable(
                name: "outbox_messages",
                schema: "islamu_event",
                newName: "outbox_messages");

            migrationBuilder.RenameTable(
                name: "organizations",
                schema: "islamu_event",
                newName: "organizations");

            migrationBuilder.RenameTable(
                name: "organization_tenants",
                schema: "islamu_event",
                newName: "organization_tenants");

            migrationBuilder.RenameTable(
                name: "organization_tenant_evidence",
                schema: "islamu_event",
                newName: "organization_tenant_evidence");

            migrationBuilder.RenameTable(
                name: "organization_setting_overrides",
                schema: "islamu_event",
                newName: "organization_setting_overrides");

            migrationBuilder.RenameTable(
                name: "organization_reviews",
                schema: "islamu_event",
                newName: "organization_reviews");

            migrationBuilder.RenameTable(
                name: "organization_positions",
                schema: "islamu_event",
                newName: "organization_positions");

            migrationBuilder.RenameTable(
                name: "organization_policy_sets",
                schema: "islamu_event",
                newName: "organization_policy_sets");

            migrationBuilder.RenameTable(
                name: "organization_pii",
                schema: "islamu_event",
                newName: "organization_pii");

            migrationBuilder.RenameTable(
                name: "organization_members",
                schema: "islamu_event",
                newName: "organization_members");

            migrationBuilder.RenameTable(
                name: "notifications",
                schema: "islamu_event",
                newName: "notifications");

            migrationBuilder.RenameTable(
                name: "notification_types",
                schema: "islamu_event",
                newName: "notification_types");

            migrationBuilder.RenameTable(
                name: "notification_scope_types",
                schema: "islamu_event",
                newName: "notification_scope_types");

            migrationBuilder.RenameTable(
                name: "notification_recipient_kinds",
                schema: "islamu_event",
                newName: "notification_recipient_kinds");

            migrationBuilder.RenameTable(
                name: "notification_reasons",
                schema: "islamu_event",
                newName: "notification_reasons");

            migrationBuilder.RenameTable(
                name: "notification_preference_profiles",
                schema: "islamu_event",
                newName: "notification_preference_profiles");

            migrationBuilder.RenameTable(
                name: "notification_preference_channels",
                schema: "islamu_event",
                newName: "notification_preference_channels");

            migrationBuilder.RenameTable(
                name: "notification_preference_categories",
                schema: "islamu_event",
                newName: "notification_preference_categories");

            migrationBuilder.RenameTable(
                name: "notification_ownership_types",
                schema: "islamu_event",
                newName: "notification_ownership_types");

            migrationBuilder.RenameTable(
                name: "notification_intents",
                schema: "islamu_event",
                newName: "notification_intents");

            migrationBuilder.RenameTable(
                name: "notification_intent_statuses",
                schema: "islamu_event",
                newName: "notification_intent_statuses");

            migrationBuilder.RenameTable(
                name: "notification_fanout_runs",
                schema: "islamu_event",
                newName: "notification_fanout_runs");

            migrationBuilder.RenameTable(
                name: "notification_fanout_processor_states",
                schema: "islamu_event",
                newName: "notification_fanout_processor_states");

            migrationBuilder.RenameTable(
                name: "notification_fanout_occurrences",
                schema: "islamu_event",
                newName: "notification_fanout_occurrences");

            migrationBuilder.RenameTable(
                name: "notification_external_delegations",
                schema: "islamu_event",
                newName: "notification_external_delegations");

            migrationBuilder.RenameTable(
                name: "notification_external_delegation_statuses",
                schema: "islamu_event",
                newName: "notification_external_delegation_statuses");

            migrationBuilder.RenameTable(
                name: "notification_entity_types",
                schema: "islamu_event",
                newName: "notification_entity_types");

            migrationBuilder.RenameTable(
                name: "notification_delivery_statuses",
                schema: "islamu_event",
                newName: "notification_delivery_statuses");

            migrationBuilder.RenameTable(
                name: "notification_delivery_policies",
                schema: "islamu_event",
                newName: "notification_delivery_policies");

            migrationBuilder.RenameTable(
                name: "notification_deliveries",
                schema: "islamu_event",
                newName: "notification_deliveries");

            migrationBuilder.RenameTable(
                name: "notification_channel_preferences",
                schema: "islamu_event",
                newName: "notification_channel_preferences");

            migrationBuilder.RenameTable(
                name: "notification_categories",
                schema: "islamu_event",
                newName: "notification_categories");

            migrationBuilder.RenameTable(
                name: "module_definitions",
                schema: "islamu_event",
                newName: "module_definitions");

            migrationBuilder.RenameTable(
                name: "managed_tenant_provisioning_operations",
                schema: "islamu_event",
                newName: "managed_tenant_provisioning_operations");

            migrationBuilder.RenameTable(
                name: "managed_control_plane_registrations",
                schema: "islamu_event",
                newName: "managed_control_plane_registrations");

            migrationBuilder.RenameTable(
                name: "madhabs",
                schema: "islamu_event",
                newName: "madhabs");

            migrationBuilder.RenameTable(
                name: "locations",
                schema: "islamu_event",
                newName: "locations");

            migrationBuilder.RenameTable(
                name: "location_rooms",
                schema: "islamu_event",
                newName: "location_rooms");

            migrationBuilder.RenameTable(
                name: "location_privacy_states",
                schema: "islamu_event",
                newName: "location_privacy_states");

            migrationBuilder.RenameTable(
                name: "location_pii",
                schema: "islamu_event",
                newName: "location_pii");

            migrationBuilder.RenameTable(
                name: "location_kinds",
                schema: "islamu_event",
                newName: "location_kinds");

            migrationBuilder.RenameTable(
                name: "location_disclosure_audiences",
                schema: "islamu_event",
                newName: "location_disclosure_audiences");

            migrationBuilder.RenameTable(
                name: "languages",
                schema: "islamu_event",
                newName: "languages");

            migrationBuilder.RenameTable(
                name: "integration_sync_outbox",
                schema: "islamu_event",
                newName: "integration_sync_outbox");

            migrationBuilder.RenameTable(
                name: "instance_policy_sets",
                schema: "islamu_event",
                newName: "instance_policy_sets");

            migrationBuilder.RenameTable(
                name: "instance_bootstrap_states",
                schema: "islamu_event",
                newName: "instance_bootstrap_states");

            migrationBuilder.RenameTable(
                name: "incoming_webhook_settlement_sources",
                schema: "islamu_event",
                newName: "incoming_webhook_settlement_sources");

            migrationBuilder.RenameTable(
                name: "incoming_webhook_redrive_results",
                schema: "islamu_event",
                newName: "incoming_webhook_redrive_results");

            migrationBuilder.RenameTable(
                name: "incoming_webhook_redrive_records",
                schema: "islamu_event",
                newName: "incoming_webhook_redrive_records");

            migrationBuilder.RenameTable(
                name: "incoming_webhook_processing_attempts",
                schema: "islamu_event",
                newName: "incoming_webhook_processing_attempts");

            migrationBuilder.RenameTable(
                name: "incoming_webhook_processing_attempt_outcomes",
                schema: "islamu_event",
                newName: "incoming_webhook_processing_attempt_outcomes");

            migrationBuilder.RenameTable(
                name: "incoming_webhook_messages",
                schema: "islamu_event",
                newName: "incoming_webhook_messages");

            migrationBuilder.RenameTable(
                name: "incoming_webhook_message_statuses",
                schema: "islamu_event",
                newName: "incoming_webhook_message_statuses");

            migrationBuilder.RenameTable(
                name: "incoming_webhook_effect_receipts",
                schema: "islamu_event",
                newName: "incoming_webhook_effect_receipts");

            migrationBuilder.RenameTable(
                name: "incoming_webhook_effect_outbox",
                schema: "islamu_event",
                newName: "incoming_webhook_effect_outbox");

            migrationBuilder.RenameTable(
                name: "identity_access_modes",
                schema: "islamu_event",
                newName: "identity_access_modes");

            migrationBuilder.RenameTable(
                name: "idempotency_records",
                schema: "islamu_event",
                newName: "idempotency_records");

            migrationBuilder.RenameTable(
                name: "groups",
                schema: "islamu_event",
                newName: "groups");

            migrationBuilder.RenameTable(
                name: "group_tenants",
                schema: "islamu_event",
                newName: "group_tenants");

            migrationBuilder.RenameTable(
                name: "group_setting_overrides",
                schema: "islamu_event",
                newName: "group_setting_overrides");

            migrationBuilder.RenameTable(
                name: "group_positions",
                schema: "islamu_event",
                newName: "group_positions");

            migrationBuilder.RenameTable(
                name: "group_members",
                schema: "islamu_event",
                newName: "group_members");

            migrationBuilder.RenameTable(
                name: "file_types",
                schema: "islamu_event",
                newName: "file_types");

            migrationBuilder.RenameTable(
                name: "external_workflow_provider_kinds",
                schema: "islamu_event",
                newName: "external_workflow_provider_kinds");

            migrationBuilder.RenameTable(
                name: "external_bindings",
                schema: "islamu_event",
                newName: "external_bindings");

            migrationBuilder.RenameTable(
                name: "external_api_keys",
                schema: "islamu_event",
                newName: "external_api_keys");

            migrationBuilder.RenameTable(
                name: "external_api_key_statuses",
                schema: "islamu_event",
                newName: "external_api_key_statuses");

            migrationBuilder.RenameTable(
                name: "external_api_key_quotas",
                schema: "islamu_event",
                newName: "external_api_key_quotas");

            migrationBuilder.RenameTable(
                name: "external_api_key_owner_types",
                schema: "islamu_event",
                newName: "external_api_key_owner_types");

            migrationBuilder.RenameTable(
                name: "external_api_key_credit_periods",
                schema: "islamu_event",
                newName: "external_api_key_credit_periods");

            migrationBuilder.RenameTable(
                name: "external_actor_subjects",
                schema: "islamu_event",
                newName: "external_actor_subjects");

            migrationBuilder.RenameTable(
                name: "events",
                schema: "islamu_event",
                newName: "events");

            migrationBuilder.RenameTable(
                name: "event_types",
                schema: "islamu_event",
                newName: "event_types");

            migrationBuilder.RenameTable(
                name: "event_ticket_types",
                schema: "islamu_event",
                newName: "event_ticket_types");

            migrationBuilder.RenameTable(
                name: "event_ticket_catalog_versions",
                schema: "islamu_event",
                newName: "event_ticket_catalog_versions");

            migrationBuilder.RenameTable(
                name: "event_templates",
                schema: "islamu_event",
                newName: "event_templates");

            migrationBuilder.RenameTable(
                name: "event_template_custom_property_options",
                schema: "islamu_event",
                newName: "event_template_custom_property_options");

            migrationBuilder.RenameTable(
                name: "event_template_custom_property_definitions",
                schema: "islamu_event",
                newName: "event_template_custom_property_definitions");

            migrationBuilder.RenameTable(
                name: "event_tech_aspects",
                schema: "islamu_event",
                newName: "event_tech_aspects");

            migrationBuilder.RenameTable(
                name: "event_tags",
                schema: "islamu_event",
                newName: "event_tags");

            migrationBuilder.RenameTable(
                name: "event_statuses",
                schema: "islamu_event",
                newName: "event_statuses");

            migrationBuilder.RenameTable(
                name: "event_sessions",
                schema: "islamu_event",
                newName: "event_sessions");

            migrationBuilder.RenameTable(
                name: "event_session_templates",
                schema: "islamu_event",
                newName: "event_session_templates");

            migrationBuilder.RenameTable(
                name: "event_session_template_custom_property_options",
                schema: "islamu_event",
                newName: "event_session_template_custom_property_options");

            migrationBuilder.RenameTable(
                name: "event_session_template_custom_property_definitions",
                schema: "islamu_event",
                newName: "event_session_template_custom_property_definitions");

            migrationBuilder.RenameTable(
                name: "event_session_tags",
                schema: "islamu_event",
                newName: "event_session_tags");

            migrationBuilder.RenameTable(
                name: "event_session_statuses",
                schema: "islamu_event",
                newName: "event_session_statuses");

            migrationBuilder.RenameTable(
                name: "event_session_speakers",
                schema: "islamu_event",
                newName: "event_session_speakers");

            migrationBuilder.RenameTable(
                name: "event_session_languages",
                schema: "islamu_event",
                newName: "event_session_languages");

            migrationBuilder.RenameTable(
                name: "event_session_kinds",
                schema: "islamu_event",
                newName: "event_session_kinds");

            migrationBuilder.RenameTable(
                name: "event_session_islamic_aspects",
                schema: "islamu_event",
                newName: "event_session_islamic_aspects");

            migrationBuilder.RenameTable(
                name: "event_session_groups",
                schema: "islamu_event",
                newName: "event_session_groups");

            migrationBuilder.RenameTable(
                name: "event_session_group_sessions",
                schema: "islamu_event",
                newName: "event_session_group_sessions");

            migrationBuilder.RenameTable(
                name: "event_session_custom_property_values",
                schema: "islamu_event",
                newName: "event_session_custom_property_values");

            migrationBuilder.RenameTable(
                name: "event_session_custom_property_projections",
                schema: "islamu_event",
                newName: "event_session_custom_property_projections");

            migrationBuilder.RenameTable(
                name: "event_session_custom_property_options",
                schema: "islamu_event",
                newName: "event_session_custom_property_options");

            migrationBuilder.RenameTable(
                name: "event_session_custom_property_definitions",
                schema: "islamu_event",
                newName: "event_session_custom_property_definitions");

            migrationBuilder.RenameTable(
                name: "event_session_categories",
                schema: "islamu_event",
                newName: "event_session_categories");

            migrationBuilder.RenameTable(
                name: "event_session_agenda_items",
                schema: "islamu_event",
                newName: "event_session_agenda_items");

            migrationBuilder.RenameTable(
                name: "event_series",
                schema: "islamu_event",
                newName: "event_series");

            migrationBuilder.RenameTable(
                name: "event_role_assignments",
                schema: "islamu_event",
                newName: "event_role_assignments");

            migrationBuilder.RenameTable(
                name: "event_reports",
                schema: "islamu_event",
                newName: "event_reports");

            migrationBuilder.RenameTable(
                name: "event_report_targets",
                schema: "islamu_event",
                newName: "event_report_targets");

            migrationBuilder.RenameTable(
                name: "event_report_signals",
                schema: "islamu_event",
                newName: "event_report_signals");

            migrationBuilder.RenameTable(
                name: "event_report_external_links",
                schema: "islamu_event",
                newName: "event_report_external_links");

            migrationBuilder.RenameTable(
                name: "event_report_evidence",
                schema: "islamu_event",
                newName: "event_report_evidence");

            migrationBuilder.RenameTable(
                name: "event_report_decisions",
                schema: "islamu_event",
                newName: "event_report_decisions");

            migrationBuilder.RenameTable(
                name: "event_report_decision_executions",
                schema: "islamu_event",
                newName: "event_report_decision_executions");

            migrationBuilder.RenameTable(
                name: "event_report_cases",
                schema: "islamu_event",
                newName: "event_report_cases");

            migrationBuilder.RenameTable(
                name: "event_registrations",
                schema: "islamu_event",
                newName: "event_registrations");

            migrationBuilder.RenameTable(
                name: "event_registration_policies",
                schema: "islamu_event",
                newName: "event_registration_policies");

            migrationBuilder.RenameTable(
                name: "event_public_actions",
                schema: "islamu_event",
                newName: "event_public_actions");

            migrationBuilder.RenameTable(
                name: "event_public_action_kinds",
                schema: "islamu_event",
                newName: "event_public_action_kinds");

            migrationBuilder.RenameTable(
                name: "event_public_action_health_states",
                schema: "islamu_event",
                newName: "event_public_action_health_states");

            migrationBuilder.RenameTable(
                name: "event_provenance_types",
                schema: "islamu_event",
                newName: "event_provenance_types");

            migrationBuilder.RenameTable(
                name: "event_participation_configurations",
                schema: "islamu_event",
                newName: "event_participation_configurations");

            migrationBuilder.RenameTable(
                name: "event_organizer_claims",
                schema: "islamu_event",
                newName: "event_organizer_claims");

            migrationBuilder.RenameTable(
                name: "event_organizer_claim_statuses",
                schema: "islamu_event",
                newName: "event_organizer_claim_statuses");

            migrationBuilder.RenameTable(
                name: "event_moderation_records",
                schema: "islamu_event",
                newName: "event_moderation_records");

            migrationBuilder.RenameTable(
                name: "event_locations",
                schema: "islamu_event",
                newName: "event_locations");

            migrationBuilder.RenameTable(
                name: "event_location_exact_read_audits",
                schema: "islamu_event",
                newName: "event_location_exact_read_audits");

            migrationBuilder.RenameTable(
                name: "event_location_disclosure_audits",
                schema: "islamu_event",
                newName: "event_location_disclosure_audits");

            migrationBuilder.RenameTable(
                name: "event_islamic_aspects",
                schema: "islamu_event",
                newName: "event_islamic_aspects");

            migrationBuilder.RenameTable(
                name: "event_formats",
                schema: "islamu_event",
                newName: "event_formats");

            migrationBuilder.RenameTable(
                name: "event_days",
                schema: "islamu_event",
                newName: "event_days");

            migrationBuilder.RenameTable(
                name: "event_custom_property_values",
                schema: "islamu_event",
                newName: "event_custom_property_values");

            migrationBuilder.RenameTable(
                name: "event_custom_property_projections",
                schema: "islamu_event",
                newName: "event_custom_property_projections");

            migrationBuilder.RenameTable(
                name: "event_custom_property_options",
                schema: "islamu_event",
                newName: "event_custom_property_options");

            migrationBuilder.RenameTable(
                name: "event_custom_property_definitions",
                schema: "islamu_event",
                newName: "event_custom_property_definitions");

            migrationBuilder.RenameTable(
                name: "event_contact_share_exports",
                schema: "islamu_event",
                newName: "event_contact_share_exports");

            migrationBuilder.RenameTable(
                name: "event_contact_share_export_items",
                schema: "islamu_event",
                newName: "event_contact_share_export_items");

            migrationBuilder.RenameTable(
                name: "event_contact_share_consents",
                schema: "islamu_event",
                newName: "event_contact_share_consents");

            migrationBuilder.RenameTable(
                name: "event_categories",
                schema: "islamu_event",
                newName: "event_categories");

            migrationBuilder.RenameTable(
                name: "event_capacity_pools",
                schema: "islamu_event",
                newName: "event_capacity_pools");

            migrationBuilder.RenameTable(
                name: "event_agenda_items",
                schema: "islamu_event",
                newName: "event_agenda_items");

            migrationBuilder.RenameTable(
                name: "entitlement_selection_rules",
                schema: "islamu_event",
                newName: "entitlement_selection_rules");

            migrationBuilder.RenameTable(
                name: "entitlement_scope_types",
                schema: "islamu_event",
                newName: "entitlement_scope_types");

            migrationBuilder.RenameTable(
                name: "email_dispatch_tenant_controls",
                schema: "islamu_event",
                newName: "email_dispatch_tenant_controls");

            migrationBuilder.RenameTable(
                name: "email_dispatch_receipts",
                schema: "islamu_event",
                newName: "email_dispatch_receipts");

            migrationBuilder.RenameTable(
                name: "email_dispatch_processor_states",
                schema: "islamu_event",
                newName: "email_dispatch_processor_states");

            migrationBuilder.RenameTable(
                name: "email_dispatch_outbox",
                schema: "islamu_event",
                newName: "email_dispatch_outbox");

            migrationBuilder.RenameTable(
                name: "email_dispatch_attempts",
                schema: "islamu_event",
                newName: "email_dispatch_attempts");

            migrationBuilder.RenameTable(
                name: "did_custody_types",
                schema: "islamu_event",
                newName: "did_custody_types");

            migrationBuilder.RenameTable(
                name: "custom_property_values",
                schema: "islamu_event",
                newName: "custom_property_values");

            migrationBuilder.RenameTable(
                name: "custom_property_projection_status",
                schema: "islamu_event",
                newName: "custom_property_projection_status");

            migrationBuilder.RenameTable(
                name: "custom_property_projection_dirty_scope",
                schema: "islamu_event",
                newName: "custom_property_projection_dirty_scope");

            migrationBuilder.RenameTable(
                name: "custom_property_options",
                schema: "islamu_event",
                newName: "custom_property_options");

            migrationBuilder.RenameTable(
                name: "custom_property_definitions",
                schema: "islamu_event",
                newName: "custom_property_definitions");

            migrationBuilder.RenameTable(
                name: "configuration_change_logs",
                schema: "islamu_event",
                newName: "configuration_change_logs");

            migrationBuilder.RenameTable(
                name: "category_types",
                schema: "islamu_event",
                newName: "category_types");

            migrationBuilder.RenameTable(
                name: "category_type_categories",
                schema: "islamu_event",
                newName: "category_type_categories");

            migrationBuilder.RenameTable(
                name: "categories",
                schema: "islamu_event",
                newName: "categories");

            migrationBuilder.RenameTable(
                name: "capacity_oversell_policies",
                schema: "islamu_event",
                newName: "capacity_oversell_policies");

            migrationBuilder.RenameTable(
                name: "capacity_hold_policies",
                schema: "islamu_event",
                newName: "capacity_hold_policies");

            migrationBuilder.RenameTable(
                name: "booking_party_types",
                schema: "islamu_event",
                newName: "booking_party_types");

            migrationBuilder.RenameTable(
                name: "audit_logs",
                schema: "islamu_event",
                newName: "audit_logs");

            migrationBuilder.RenameTable(
                name: "audience_genders",
                schema: "islamu_event",
                newName: "audience_genders");

            migrationBuilder.RenameTable(
                name: "audience_ages",
                schema: "islamu_event",
                newName: "audience_ages");

            migrationBuilder.RenameTable(
                name: "atproto_records",
                schema: "islamu_event",
                newName: "atproto_records");

            migrationBuilder.RenameTable(
                name: "atproto_record_tenant_presentations",
                schema: "islamu_event",
                newName: "atproto_record_tenant_presentations");

            migrationBuilder.RenameTable(
                name: "atproto_outbound_record_ownerships",
                schema: "islamu_event",
                newName: "atproto_outbound_record_ownerships");

            migrationBuilder.RenameTable(
                name: "atproto_jetstream_quarantines",
                schema: "islamu_event",
                newName: "atproto_jetstream_quarantines");

            migrationBuilder.RenameTable(
                name: "atproto_jetstream_consumer_states",
                schema: "islamu_event",
                newName: "atproto_jetstream_consumer_states");

            migrationBuilder.RenameTable(
                name: "atproto_identity_moderation_records",
                schema: "islamu_event",
                newName: "atproto_identity_moderation_records");

            migrationBuilder.RenameTable(
                name: "atproto_identities",
                schema: "islamu_event",
                newName: "atproto_identities");

            migrationBuilder.RenameTable(
                name: "atproto_event_projections",
                schema: "islamu_event",
                newName: "atproto_event_projections");

            migrationBuilder.RenameTable(
                name: "assignment_statuses",
                schema: "islamu_event",
                newName: "assignment_statuses");

            migrationBuilder.RenameTable(
                name: "approval_statuses",
                schema: "islamu_event",
                newName: "approval_statuses");

            migrationBuilder.RenameTable(
                name: "app_settings",
                schema: "islamu_event",
                newName: "app_settings");

            migrationBuilder.RenameTable(
                name: "analytics_providers",
                schema: "islamu_event",
                newName: "analytics_providers");

            migrationBuilder.RenameTable(
                name: "ai_tool_executions",
                schema: "islamu_event",
                newName: "ai_tool_executions");

            migrationBuilder.RenameTable(
                name: "ai_runs",
                schema: "islamu_event",
                newName: "ai_runs");

            migrationBuilder.RenameTable(
                name: "ai_run_statuses",
                schema: "islamu_event",
                newName: "ai_run_statuses");

            migrationBuilder.RenameTable(
                name: "ai_reference_kinds",
                schema: "islamu_event",
                newName: "ai_reference_kinds");

            migrationBuilder.RenameTable(
                name: "ai_provider_kinds",
                schema: "islamu_event",
                newName: "ai_provider_kinds");

            migrationBuilder.RenameTable(
                name: "ai_proposed_actions",
                schema: "islamu_event",
                newName: "ai_proposed_actions");

            migrationBuilder.RenameTable(
                name: "ai_proposed_action_statuses",
                schema: "islamu_event",
                newName: "ai_proposed_action_statuses");

            migrationBuilder.RenameTable(
                name: "ai_proposed_action_kinds",
                schema: "islamu_event",
                newName: "ai_proposed_action_kinds");

            migrationBuilder.RenameTable(
                name: "ai_messages",
                schema: "islamu_event",
                newName: "ai_messages");

            migrationBuilder.RenameTable(
                name: "ai_message_roles",
                schema: "islamu_event",
                newName: "ai_message_roles");

            migrationBuilder.RenameTable(
                name: "ai_conversations",
                schema: "islamu_event",
                newName: "ai_conversations");

            migrationBuilder.RenameTable(
                name: "ai_conversation_statuses",
                schema: "islamu_event",
                newName: "ai_conversation_statuses");

            migrationBuilder.RenameTable(
                name: "ai_conversation_references",
                schema: "islamu_event",
                newName: "ai_conversation_references");

            migrationBuilder.RenameTable(
                name: "ai_consent_grants",
                schema: "islamu_event",
                newName: "ai_consent_grants");

            migrationBuilder.RenameTable(
                name: "advance_registration_obligations",
                schema: "islamu_event",
                newName: "advance_registration_obligations");

            migrationBuilder.RenameTable(
                name: "actors",
                schema: "islamu_event",
                newName: "actors");

            migrationBuilder.RenameTable(
                name: "actor_types",
                schema: "islamu_event",
                newName: "actor_types");

            migrationBuilder.RenameTable(
                name: "actor_subscriptions",
                schema: "islamu_event",
                newName: "actor_subscriptions");

            migrationBuilder.RenameTable(
                name: "actor_subscription_statuses",
                schema: "islamu_event",
                newName: "actor_subscription_statuses");

            migrationBuilder.RenameTable(
                name: "actor_subscription_notification_levels",
                schema: "islamu_event",
                newName: "actor_subscription_notification_levels");

            migrationBuilder.RenameTable(
                name: "actor_pii",
                schema: "islamu_event",
                newName: "actor_pii");

            migrationBuilder.RenameTable(
                name: "actor_moderation_records",
                schema: "islamu_event",
                newName: "actor_moderation_records");

            migrationBuilder.RenameTable(
                name: "actor_merges",
                schema: "islamu_event",
                newName: "actor_merges");

            migrationBuilder.RenameTable(
                name: "actor_key_stores",
                schema: "islamu_event",
                newName: "actor_key_stores");

            migrationBuilder.RenameTable(
                name: "account_authority_kinds",
                schema: "islamu_event",
                newName: "account_authority_kinds");

            migrationBuilder.CreateTable(
                name: "authority_counter",
                schema: "privacy_erasure_authority",
                columns: table => new
                {
                    singleton = table.Column<bool>(type: "boolean", nullable: false),
                    last_sequence = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authority_counter", x => x.singleton);
                    table.CheckConstraint("ck_privacy_erasure_authority_counter_nonnegative", "last_sequence >= 0");
                    table.CheckConstraint("ck_privacy_erasure_authority_counter_singleton", "singleton");
                });

            migrationBuilder.CreateTable(
                name: "erasure_intents",
                schema: "privacy_erasure_authority",
                columns: table => new
                {
                    authority_sequence = table.Column<long>(type: "bigint", nullable: false),
                    intent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_version = table.Column<int>(type: "integer", nullable: false),
                    reason_code = table.Column<short>(type: "smallint", nullable: false),
                    recorded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    requested_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    retention_expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "'infinity'::timestamp with time zone"),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_kind = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_erasure_intents", x => x.authority_sequence);
                    table.UniqueConstraint("ak_privacy_erasure_intents_intent_id", x => x.intent_id);
                    table.CheckConstraint("ck_privacy_erasure_intents_intent_rfc4122_variant", "substring(intent_id::text, 20, 1) IN ('8', '9', 'a', 'b')");
                    table.CheckConstraint("ck_privacy_erasure_intents_intent_uuid_v7", "substring(intent_id::text, 15, 1) = '7'");
                    table.CheckConstraint("ck_privacy_erasure_intents_policy_version", "policy_version > 0");
                    table.CheckConstraint("ck_privacy_erasure_intents_reason", "reason_code BETWEEN 1 AND 3");
                    table.CheckConstraint("ck_privacy_erasure_intents_retention", "retention_expires_at_utc > recorded_at_utc");
                    table.CheckConstraint("ck_privacy_erasure_intents_sequence", "authority_sequence > 0");
                    table.CheckConstraint("ck_privacy_erasure_intents_server_time_order", "recorded_at_utc >= requested_at_utc");
                    table.CheckConstraint("ck_privacy_erasure_intents_subject_kind", "subject_kind = 1");
                    table.CheckConstraint("ck_privacy_erasure_intents_subject_nonempty", "subject_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                });

            migrationBuilder.CreateIndex(
                name: "ix_registration_answers_tenant_id_registration_order_id_ticket",
                table: "registration_answers",
                columns: new[] { "tenant_id", "registration_order_id", "ticket_assignment_subject_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_registration_answers_subject_shape",
                table: "registration_answers",
                sql: "num_nonnulls(order_subject_id, purchaser_subject_id, participant_subject_id, ticket_assignment_subject_id, session_selection_subject_id) = 1 AND ((answer_subject_type_id = 1 AND order_subject_id = registration_order_id AND requirement_subject_type_id = 1) OR (answer_subject_type_id = 2 AND purchaser_subject_id = registration_order_id AND requirement_subject_type_id IN (1, 4)) OR (answer_subject_type_id = 3 AND participant_subject_id IS NOT NULL AND requirement_subject_type_id IN (3, 5)) OR (answer_subject_type_id = 4 AND ticket_assignment_subject_id IS NOT NULL AND requirement_subject_type_id = 2) OR (answer_subject_type_id = 5 AND session_selection_subject_id = requirement_subject_id AND requirement_subject_type_id = 6))");

            migrationBuilder.CreateIndex(
                name: "ix_erasure_intents_intent_id_subject_kind_policy_version",
                schema: "privacy_erasure_authority",
                table: "erasure_intents",
                columns: new[] { "intent_id", "subject_kind", "policy_version" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_privacy_erasure_policy_coverage_privacy_erasure_intents_int",
                table: "privacy_erasure_policy_coverage",
                column: "intent_id",
                principalSchema: "privacy_erasure_authority",
                principalTable: "erasure_intents",
                principalColumn: "intent_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_privacy_erasure_sagas_privacy_erasure_intents_intent_id",
                table: "privacy_erasure_sagas",
                column: "intent_id",
                principalSchema: "privacy_erasure_authority",
                principalTable: "erasure_intents",
                principalColumn: "intent_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_registration_answers_registration_ticket_assignments_tenant",
                table: "registration_answers",
                columns: new[] { "tenant_id", "registration_order_id", "ticket_assignment_subject_id" },
                principalTable: "registration_ticket_assignments",
                principalColumns: new[] { "tenant_id", "registration_order_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
