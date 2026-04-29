// ABOUTME: DBML schema reference for the ISLAMU Event platform database.
// ABOUTME: Generated from domain entities and EF Core configurations — entities are the source of truth.

Project islamu_event {
  database_type: 'PostgreSQL'
  Note: 'ISLAMU Event multi-tenant platform. ATProto federation, Clean Architecture, modular event composition.'
}

// ============================================================
// Lookup / Reference Tables (int PK, ValueGeneratedNever)
// ============================================================

Table "actor_types" {
  "id" int [pk, not null]
  "full_name" varchar(200) [not null]
  "master_code" varchar(100) [not null]
  "description" varchar(500)

  Note: 'Lookup: classifies federated actors. Values: User(1), Organization(2), Group(3), Bot(4). Seeded.'
}

Table "analytics_providers" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  Note: 'Lookup: supported analytics engines. Values: PostHog(1), Plausible(2), GoogleAnalytics(3). Seeded.'
}

Table "approval_statuses" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  Note: 'Lookup: entity approval lifecycle. Values: Pending(1), Approved(2), Rejected(3), Deferred(4). Seeded.'
}

Table "audience_ages" {
  "id" int [pk, not null]
  "master_code" text [not null]
  "full_name" text [not null]
  "description" text
  "min_age" int
  "max_age" int

  Note: 'Lookup: age-group targeting. Values: AllAges(1), Kids(2), Teens(3), Adults(4). Seeded.'
}

Table "audience_genders" {
  "id" int [pk, not null]
  "master_code" text [not null]
  "full_name" text [not null]
  "description" text

  Note: 'Lookup: gender-based targeting. Values: Mixed(1), MenOnly(2), WomenOnly(3). Seeded.'
}

Table "category_types" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  Note: 'Lookup: classifies categories for different contexts (Events, Organizations, Users). Seeded.'
}

Table "did_custody_types" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  Note: 'Lookup: ATProto DID key management model. Values: SelfCustody(1), PlatformManaged(2). Seeded.'
}

Table "event_formats" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  Note: 'Lookup: event delivery mode. Values: InPerson(1), Virtual(2), Hybrid(3). Seeded.'
}

Table "event_statuses" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  Note: 'Lookup: event lifecycle state. Values: Draft(1), Published(2), Cancelled(3), Completed(4), Archived(5). Seeded.'
}

Table "event_types" {
  "id" int [pk, not null]
  "full_name" varchar(500) [not null]
  "master_code" varchar(500) [not null]
  "description" varchar(500)
  "tenant_id" uuid

  indexes {
    tenant_id
    master_code [unique, name: 'ix_event_types_master_code', note: 'filter: tenant_id IS NULL']
    (tenant_id, master_code) [unique, name: 'ix_event_types_tenant_master_code', note: 'filter: tenant_id IS NOT NULL']
  }

  Note: 'Lookup: categorizes events (e.g. Conference, Webinar, Workshop). TenantId null = instance-wide default.'
}

Table "file_types" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  Note: 'Lookup: media/document classifications. Values: Image(1), Video(2), Document(3), Archive(4). Seeded.'
}

Table "group_positions" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  Note: 'Lookup: formal roles within a group. Values: Lead(1), Admin(2), Moderator(3), Member(4). Seeded.'
}

Table "languages" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  Note: 'Lookup: ISO language codes for localization and content metadata. Seeded.'
}

Table "madhabs" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  Note: 'Lookup: Islamic schools of jurisprudence. Values: Hanafi(1), Maliki(2), Shafii(3), Hanbali(4), Other(5). Seeded.'
}

Table "organization_positions" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  Note: 'Lookup: formal roles within an organization. Values: CEO(1), Admin(2), Staff(3), Volunteer(4). Seeded.'
}

Table "owner_types" {
  "id" int [pk, not null]
  "master_code" text [not null]
  "full_name" text [not null]
  "description" text

  Note: 'Lookup: classifies entities that can own resources. Values: User(1), Organization(2), Group(3), Tenant(4), InstanceAdmin(5). Seeded.'
}

Table "registration_modes" {
  "id" int [pk, not null]
  "master_code" varchar(50) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  Note: 'Lookup: registration policy. Values: Open(1), ApprovalRequired(2), InviteOnly(3), Closed(4). Seeded.'
}

Table "tag_types" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  Note: 'Lookup: classifies tags for different contexts. Seeded.'
}

Table "tenant_statuses" {
  "id" int [pk, not null]
  "master_code" varchar(50) [not null]
  "full_name" varchar(100) [not null]
  "description" varchar(500)
  "is_active_state" boolean [not null]

  Note: 'Lookup: tenant lifecycle. Values: Active(1), Suspended(2), Provisioning(3), Deactivated(4). Seeded.'
}

Table "visibility_types" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  Note: 'Lookup: discovery level. Values: Public(1), Private(2), Unlisted(3), MembersOnly(4). Seeded.'
}

Table "schedule_item_kinds" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  Note: 'Lookup: classifies agenda items. Values: Intro(1), Talk(2), QAndA(3), Break(4), Prayer(5), Outro(6), Logistics(7), Custom(8). Seeded.'
}

Table "event_registration_policies" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  Note: 'Lookup: defines which registration scopes are allowed for an event. Values: WholeEventOnly(1), WholeDayOnly(2), SessionSelectionOnly(3), WholeEventOrDay(4), WholeEventOrSession(5), Flexible(6). Seeded.'
}

Table "registration_scopes" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  Note: 'Lookup: granularity of a registration intent. Values: Event(1), Day(2), SessionSelection(3). Seeded.'
}

Table "external_api_key_statuses" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  Note: 'Lookup: external API key state. Values: Active(1), Revoked(2), Expired(3). Seeded.'
}

Table "external_api_key_credit_periods" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  Note: 'Lookup: credit reset frequency. Values: None(1), Daily(2), Weekly(3), Monthly(4). Seeded.'
}

Table "notification_types" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  Note: 'Lookup: notification classification. Seeded.'
}

Table "notification_entity_types" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  Note: 'Lookup: type of entity a notification relates to. Seeded.'
}

Table "notification_reasons" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  Note: 'Lookup: why a notification was triggered. Seeded.'
}

// ============================================================
// System / Configuration Tables
// ============================================================

Table "app_settings" {
  "config_key" varchar(256) [pk, not null]
  "encrypted_value" text [not null]
  "key_version" int [not null]
  "encrypted_at" timestamptz [not null]
  "encrypted_by" uuid
  "is_sensitive" boolean [not null]
  "description" varchar(1000)
  "category" varchar(100)
  "value_type" int [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "row_version" bytea [not null]

  indexes {
    category [name: 'ix_app_settings_category']
    is_sensitive [name: 'ix_app_settings_is_sensitive']
  }

  Note: 'Encrypted system settings. Check: CK_AppSettings_NoHighValueSecrets — blocks database/security keys.'
}

Table "system_settings" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "setting_key" varchar(256) [not null]
  "value" text [not null]
  "value_type" int [not null]
  "is_locked" boolean [not null]
  "allowed_values" jsonb
  "description" varchar(1000)
  "category" varchar(100)
  "display_order" int [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    setting_key [unique]
  }
}

Table "configuration_change_logs" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "user_id" uuid [not null]
  "timestamp" timestamptz [not null]
  "setting_key" varchar(256) [not null]
  "old_value" text
  "new_value" text [not null]
  "scope" int [not null]
  "scope_id" uuid
  "action_type" varchar(50) [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    user_id
    setting_key
    timestamp
    (scope, scope_id)
  }
}

Table "secret_bindings" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "setting_key" varchar(256) [not null]
  "scope" int [not null]
  "scope_id" uuid
  "secret_name" varchar(256) [not null]
  "vault_provider" varchar(100) [not null]
  "source_type" int [not null]
  "is_active" boolean [not null]
  "last_validated_at" timestamptz
  "validation_status" int [not null]
  "validation_error" text
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    setting_key [name: 'ix_secret_bindings_setting_key_instance_unique', note: 'filter: scope = 0']
    (setting_key, scope_id) [unique, name: 'ix_secret_bindings_setting_key_scope_id_tenant_unique', note: 'filter: scope = 1']
    (scope, scope_id) [name: 'ix_secret_bindings_scope_scope_id']
  }

  Note: 'Maps application settings to external vault secrets (KeyVault, AWS Secrets, etc.).'
}

// ============================================================
// RBAC (Roles & Permissions)
// ============================================================

Table "roles" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)
  "scope" int [not null]
  "is_system" boolean [not null]

  indexes {
    master_code [unique, name: 'ix_roles_mastercode']
    scope [name: 'ix_roles_scope']
  }
}

Table "permissions" {
  "id" int [pk, not null, note: 'auto-increment']
  "resource_kind" varchar(100) [not null]
  "action" varchar(50) [not null]
  "field_scope" varchar(100)
  "master_code" varchar(200) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)
  "group_name" varchar(100) [not null]
  "scope" int [not null]
  "is_system" boolean [not null]
  "is_filtered" boolean [not null]
  "is_active" boolean [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    master_code [unique, name: 'ix_permissions_mastercode']
    (resource_kind, action) [name: 'ix_permissions_resource_action']
    scope [name: 'ix_permissions_scope']
  }
}

Table "role_permissions" {
  "role_id" int [pk, not null]
  "permission_id" int [pk, not null]
  "granted_at" timestamptz [not null]
  "granted_by" uuid

  indexes {
    role_id [name: 'ix_rolepermissions_role']
    permission_id [name: 'ix_rolepermissions_permission']
  }
}

// ============================================================
// Federation / AT Protocol
// ============================================================

Table "atproto_records" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "did" varchar(255) [not null]
  "collection" varchar(255) [not null]
  "record_key" varchar(255) [not null]
  "cid" varchar(255)
  "uri" varchar(500)
  "indexed_at" timestamptz

  indexes {
    (did, collection, record_key) [unique, name: 'ix_atproto_records_unique']
  }
}

Table "indexed_dids" {
  "did" varchar(255) [pk, not null]
  "handle" varchar(255)
  "pds_host" varchar(500) [not null]
  "signing_key" text
  "is_active" boolean [not null]
  "last_indexed_at" timestamptz [not null]
  "last_seen_at" timestamptz

  indexes {
    did [unique]
  }
}

Table "sync_states" {
  "id" int [pk, not null]
  "service" varchar(500) [not null]
  "cursor" bigint [not null]
  "last_seq_time" timestamptz
  "updated_at" timestamptz [not null]

  indexes {
    service [unique]
  }
}

Table "pds_sync_outbox" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "did" varchar(255) [not null]
  "collection" varchar(255) [not null]
  "record_key" varchar(255) [not null]
  "operation" int [not null]
  "payload" jsonb
  "pds_host" varchar(255)
  "status" int [not null]
  "created_at" timestamptz [not null]
  "processed_at" timestamptz
  "retry_count" int [not null]
  "last_error" varchar(2000)
  "next_retry_at" timestamptz
  "source_entity_type" varchar(100)
  "source_entity_id" uuid
  "max_retries" int [not null, default: 10, note: 'dead-letter threshold']
  "dead_lettered_at" timestamptz [note: 'when entry was quarantined after exhausting retries']

  indexes {
    (status, next_retry_at, created_at) [name: 'IX_PdsSyncOutbox_WorkerPoll']
    did [name: 'IX_PdsSyncOutbox_Did']
    (source_entity_type, source_entity_id) [name: 'IX_PdsSyncOutbox_SourceEntity']
    (did, collection, record_key, operation, created_at) [unique, name: 'IX_PdsSyncOutbox_Unique']
  }

  Note: 'Transactional outbox for ATProto PDS sync. Dead-letter after max retries.'
}

// ============================================================
// General Outbox
// ============================================================

Table "outbox_messages" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "aggregate_type" varchar(200) [not null]
  "aggregate_id" uuid [not null]
  "event_type" varchar(200) [not null]
  "payload" jsonb
  "status" int [not null, default: 1]
  "created_at" timestamptz [not null]
  "processed_at" timestamptz
  "retry_count" integer [not null, default: 0]
  "last_error" varchar(2000)
  "next_retry_at" timestamptz
  "max_retries" integer [not null, default: 10]
  "dead_lettered_at" timestamptz

  indexes {
    (status, next_retry_at, created_at) [name: 'IX_OutboxMessages_WorkerPoll']
    (aggregate_type, aggregate_id) [name: 'IX_OutboxMessages_Aggregate']
    (aggregate_type, aggregate_id, event_type, created_at) [name: 'IX_OutboxMessages_Dedup']
  }

  Note: 'General transactional outbox. OutboxProcessor polls for pending messages with exponential backoff retry. Dead-letter after max_retries exhausted.'
}

// ============================================================
// Modules
// ============================================================

Table "module_definitions" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "module_key" varchar(50) [not null]
  "name" varchar(100) [not null]
  "description" varchar(500)
  "wizard_schema_url" varchar(500)
  "icon_name" varchar(50)
  "display_order" int [not null]
  "is_active" boolean [not null]
  "category" varchar(50)
  "created_at" timestamptz [not null]
  "updated_at" timestamptz

  indexes {
    module_key [unique]
  }
}

// ============================================================
// Bootstrap
// ============================================================

Table "instance_bootstrap_states" {
  "id" uuid [pk, not null, note: 'uuidv7 app-side']
  "is_completed" boolean [not null]
  "created_at" timestamptz [not null]
  "completed_at" timestamptz
  "completed_by_user_id" uuid
  "selected_deployment_mode" varchar(32)

  indexes {
    is_completed [unique, name: 'ix_instance_bootstrap_state_completed_unique', note: 'filter: is_completed = true']
  }
}

Table "instance_policy_sets" {
  "id" uuid [pk, not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "xmin" xid [not null, note: 'rowversion / optimistic concurrency']
  "branding_policy" jsonb [not null]
  "domains_policy" jsonb [not null]
  "events_policy" jsonb [not null]
  "modules_policy" jsonb [not null]
  "organizations_policy" jsonb [not null]
  "render_policy" jsonb [not null]
  "tenant_delegation_policy" jsonb [not null]
}

// ============================================================
// Tenants
// ============================================================

Table "tenants" {
  "id" uuid [pk, not null, note: 'uuidv7 app-side']
  "full_name" varchar(500) [not null]
  "slug" varchar(200) [not null]
  "description" varchar(500)
  "tenant_status_id" int [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    slug [unique]
  }

  Note: 'Multi-tenant root entity. All scoped entities reference this.'
}

Table "tenant_settings" {
  "id" uuid [pk, not null, note: 'uuidv7 app-side']
  "tenant_id" uuid [not null]
  "event_publishing_policy" int [not null]
  "allow_public_organization_registration" boolean [not null]
  "require_organization_verification" boolean [not null]
  "allow_public_group_creation" boolean [not null]
  "require_group_approval" boolean [not null]
  "default_organization_id" uuid
  "default_group_id" uuid
  "concurrency_stamp" uuid [not null, note: 'optimistic concurrency token, app-managed']

  Note: 'Tenant-level governance config. Concurrency-protected.'
}

Table "tenant_setting_overrides"{
  "id" uuid [pk, not null, note: 'uuidv7 app-side']
  "tenant_id" uuid [not null]
  "setting_key" varchar(256) [not null]
  "value" text [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    (tenant_id, setting_key) [unique, name: 'ix_tenant_setting_overrides_tenant_id_setting_key']
  }
}

Table "tenant_navigation_links" {
  "id" uuid [pk, not null, note: 'uuidv7 app-side']
  "tenant_id" uuid [not null]
  "label" varchar(50) [not null]
  "url" varchar(500) [not null]
  "icon" varchar(100)
  "order" int [not null]
  "open_in_new_tab" boolean [not null]
  "is_active" boolean [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
}

// ============================================================
// Tenant Footer Links
// ============================================================

Table "tenant_footer_link_groups" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "tenant_id" uuid
  "title" varchar(200) [not null]
  "order" int [not null]
  "is_active" boolean [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid

  indexes {
    tenant_id [name: 'ix_tenant_footer_link_groups_tenant_id']
    (tenant_id, order) [name: 'ix_tenant_footer_link_groups_tenant_id_order']
  }

  Note: 'TenantId null = instance-default group.'
}

Table "tenant_footer_links" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "footer_link_group_id" uuid [not null]
  "label" varchar(200) [not null]
  "url" varchar(2000) [not null]
  "open_in_new_tab" boolean [not null]
  "order" int [not null]
  "is_active" boolean [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid

  indexes {
    (footer_link_group_id, order) [name: 'ix_tenant_footer_links_footer_link_group_id_order']
  }
}

// ============================================================
// Appearance (UI Themes)
// ============================================================

Table "ui_themes" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "tenant_id" uuid
  "theme_key" varchar(128) [not null]
  "display_name" varchar(200) [not null]
  "description" varchar(1000)
  "is_active" boolean [not null]
  "is_default" boolean [not null]
  "sort_order" int [not null]
  "light_primary" varchar(7) [not null]
  "light_secondary" varchar(7) [not null]
  "light_background" varchar(7) [not null]
  "light_surface" varchar(7) [not null]
  "light_appbar_background" varchar(32) [not null]
  "light_appbar_text" varchar(7) [not null]
  "light_drawer_background" varchar(32) [not null]
  "light_drawer_text" varchar(7) [not null]
  "light_drawer_icon" varchar(7) [not null]
  "light_text_primary" varchar(7) [not null]
  "light_text_secondary" varchar(7) [not null]
  "light_info" varchar(7) [not null]
  "light_success" varchar(7) [not null]
  "light_warning" varchar(7) [not null]
  "light_error" varchar(7) [not null]
  "light_lines_default" varchar(7) [not null]
  "light_divider" varchar(32) [not null]
  "dark_primary" varchar(7) [not null]
  "dark_secondary" varchar(7) [not null]
  "dark_background" varchar(7) [not null]
  "dark_surface" varchar(7) [not null]
  "dark_appbar_background" varchar(32) [not null]
  "dark_appbar_text" varchar(7) [not null]
  "dark_drawer_background" varchar(32) [not null]
  "dark_drawer_text" varchar(7) [not null]
  "dark_drawer_icon" varchar(7) [not null]
  "dark_text_primary" varchar(7) [not null]
  "dark_text_secondary" varchar(7) [not null]
  "dark_info" varchar(7) [not null]
  "dark_success" varchar(7) [not null]
  "dark_warning" varchar(7) [not null]
  "dark_error" varchar(7) [not null]
  "dark_lines_default" varchar(7) [not null]
  "dark_divider" varchar(32) [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "xmin" xid [not null, note: 'rowversion / optimistic concurrency']

  indexes {
    theme_key [unique, name: 'ix_ui_themes_theme_key', note: 'filter: tenant_id IS NULL']
    (tenant_id, theme_key) [unique, name: 'ix_ui_themes_tenant_id_theme_key', note: 'filter: tenant_id IS NOT NULL']
    is_default [unique, name: 'ix_ui_themes_is_default', note: 'filter: tenant_id IS NULL AND is_default = true']
    (tenant_id, is_default) [unique, name: 'ix_ui_themes_tenant_id_is_default', note: 'filter: tenant_id IS NOT NULL AND is_default = true']
  }
}

Table "ui_theme_presets" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "tenant_id" uuid
  "theme_key" varchar(128) [not null]
  "display_name" varchar(200) [not null]
  "description" varchar(1000)
  "is_active" boolean [not null]
  "is_archived" boolean [not null]
  "sort_order" int [not null]
  "origin" int [not null]
  "light_primary" varchar(7) [not null]
  "light_secondary" varchar(7) [not null]
  "light_background" varchar(7) [not null]
  "light_surface" varchar(7) [not null]
  "light_appbar_background" varchar(32) [not null]
  "light_appbar_text" varchar(7) [not null]
  "light_drawer_background" varchar(32) [not null]
  "light_drawer_text" varchar(7) [not null]
  "light_drawer_icon" varchar(7) [not null]
  "light_text_primary" varchar(7) [not null]
  "light_text_secondary" varchar(7) [not null]
  "light_info" varchar(7) [not null]
  "light_success" varchar(7) [not null]
  "light_warning" varchar(7) [not null]
  "light_error" varchar(7) [not null]
  "light_lines_default" varchar(7) [not null]
  "light_divider" varchar(32) [not null]
  "dark_primary" varchar(7) [not null]
  "dark_secondary" varchar(7) [not null]
  "dark_background" varchar(7) [not null]
  "dark_surface" varchar(7) [not null]
  "dark_appbar_background" varchar(32) [not null]
  "dark_appbar_text" varchar(7) [not null]
  "dark_drawer_background" varchar(32) [not null]
  "dark_drawer_text" varchar(7) [not null]
  "dark_drawer_icon" varchar(7) [not null]
  "dark_text_primary" varchar(7) [not null]
  "dark_text_secondary" varchar(7) [not null]
  "dark_info" varchar(7) [not null]
  "dark_success" varchar(7) [not null]
  "dark_warning" varchar(7) [not null]
  "dark_error" varchar(7) [not null]
  "dark_lines_default" varchar(7) [not null]
  "dark_divider" varchar(32) [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid

  indexes {
    theme_key [unique, name: 'ix_ui_theme_presets_theme_key', note: 'filter: tenant_id IS NULL']
    (tenant_id, theme_key) [unique, name: 'ix_ui_theme_presets_tenant_id_theme_key', note: 'filter: tenant_id IS NOT NULL']
    (tenant_id, is_active) [name: 'ix_ui_theme_presets_tenant_id_is_active']
  }
}

Table "tenant_onboarding_states" {
  "id" uuid [pk, not null, note: 'uuidv7 app-side']
  "tenant_id" uuid [not null]
  "is_completed" boolean [not null]
  "current_step" int [not null]
  "total_steps" int [not null]
  "completed_steps_json" jsonb
  "created_at" timestamptz [not null]
  "completed_at" timestamptz
  "completed_by_user_id" uuid

  indexes {
    tenant_id [unique]
  }
}

Table "tenant_policy_sets" {
  "id" uuid [pk, not null]
  "tenant_id" uuid [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "xmin" xid [not null, note: 'rowversion / optimistic concurrency']
  "events_policy" jsonb [not null]
  "organizations_policy" jsonb [not null]
  "branding_policy" jsonb [not null]
  "render_policy" jsonb [not null]

  indexes {
    tenant_id [unique, name: 'ix_tenant_policy_sets_tenant_id']
  }
}

Table "tenant_lifecycle_logs" {
  "id" uuid [pk, not null, note: 'uuidv7 app-side']
  "tenant_id" uuid [not null]
  "old_status_id" int
  "new_status_id" int [not null]
  "transitioned_by_user_id" uuid [not null]
  "reason" varchar(1000)
  "transitioned_at" timestamptz [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
}

Table "tenant_capabilities" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "tenant_id" uuid [not null]
  "module_id" uuid [not null]
  "is_enabled" boolean [not null]
  "enabled_at" timestamptz [not null]
  "enabled_by" uuid
  "configuration_json" jsonb
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    (tenant_id, module_id) [unique]
  }
}

Table "tenant_invitations" {
  "id" uuid [pk, not null, note: 'uuidv7 app-side']
  "tenant_id" uuid [not null]
  "email" varchar(320) [not null]
  "role_id" int [not null]
  "token" varchar(512) [not null]
  "expires_at" timestamptz [not null]
  "is_accepted" boolean [not null]
  "accepted_at" timestamptz
  "accepted_by_user_id" uuid
  "invited_by_user_id" uuid [not null]
  "allowed_domain" varchar(255)
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    token [unique]
    (tenant_id, email)
  }
}

Table "tenant_members" {
  "id" uuid [pk, not null, note: 'uuidv7 app-side']
  "user_id" uuid [not null]
  "tenant_id" uuid [not null]
  "role_id" int [not null]
  "granted_at" timestamptz [not null]
  "granted_by" uuid
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    (user_id, tenant_id) [unique, name: 'ix_tenantmembers_user_tenant']
  }
}

Table "external_api_keys" {
  "id" uuid [pk, not null]
  "tenant_id" uuid
  "name" varchar(200) [not null]
  "description" varchar(1000)
  "key_id" varchar(64) [not null]
  "secret_hash" varchar(500) [not null]
  "scopes" varchar(1000) [not null]
  "owner_type" int [not null]
  "owner_id" uuid [not null]
  "external_api_key_status_id" int [not null]
  "external_api_key_credit_period_id" int [not null]
  "credit_limit" int
  "max_rollover_credits" int
  "expires_at" timestamptz
  "last_used_at" timestamptz
  "last_used_ip" varchar(64)
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    key_id [unique, name: 'ix_external_api_keys_key_id']
    (tenant_id, owner_type, owner_id) [name: 'ix_external_api_keys_tenant_id_owner_type_owner_id']
    (tenant_id, external_api_key_status_id) [name: 'ix_external_api_keys_tenant_id_external_api_key_status_id']
  }
}

Table "external_api_key_quotas" {
  "id" uuid [pk, not null]
  "external_api_key_id" uuid [not null]
  "period_start" timestamptz [not null]
  "period_end" timestamptz [not null]
  "credits_allowed" int [not null]
  "credits_consumed" int [not null]
  "credits_remaining" int [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    (external_api_key_id, period_start) [unique]
  }
}

// ============================================================
// Taxonomy (Categories, Tags)
// ============================================================

Table "categories" {
  "id" uuid [pk, not null, note: 'uuidv7 app-side']
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "parent_id" uuid
  "tenant_id" uuid [not null]

  Indexes {
    (tenant_id, master_code) [unique]
  }
}

Table "category_type_categories" {
  "id" uuid [pk, not null]
  "category_id" uuid [not null]
  "category_type_id" int [not null]
  "tenant_id" uuid [not null]
}

Table "tags" {
  "id" uuid [pk, not null, note: 'uuidv7 app-side']
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "tenant_id" uuid [not null]

  Indexes {
    (tenant_id, master_code) [unique, name: 'ix_tags_tenant_master_code']
  }
}

Table "tag_type_tags" {
  "id" uuid [pk, not null]
  "tag_id" uuid [not null]
  "tag_type_id" int [not null]
  "tenant_id" uuid [not null]
}

// ============================================================
// Custom Properties (EAV)
// ============================================================

Table "custom_property_definitions" {
  "id" uuid [pk, not null]
  "entity_type_name" varchar(50) [not null]
  "tenant_id" uuid [not null]
  "namespace" varchar(100) [not null]
  "key" varchar(100) [not null]
  "display_name" varchar(200) [not null]
  "description" varchar(500)
  "property_type" int [not null]
  "is_required" boolean [not null]
  "is_multi" boolean [not null]
  "is_active" boolean [not null]
  "sort_order" int [not null]
  "exposure_level" int [not null]
  "is_searchable" boolean [not null]
  "is_filterable" boolean [not null]
  "is_exportable" boolean [not null]
  "is_moderation_relevant" boolean [not null]
  "is_analytics_relevant" boolean [not null]
  "is_system_owned" boolean [not null]
  "default_text_value" varchar(1000)
  "default_number_value" decimal(19,4)
  "default_boolean_value" boolean
  "default_date_time_value" timestamptz
  "default_option_id" uuid
  "min_length" int
  "max_length" int
  "regex_pattern" varchar(1000)
  "min_number" decimal(19,4)
  "max_number" decimal(19,4)
  "min_date_time" timestamptz
  "max_date_time" timestamptz
  "allowed_url_schemes" varchar(500)
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (tenant_id, entity_type_name, namespace, key) [unique, name: 'ix_cpd_tenant_entity_namespace_key']
    (tenant_id, entity_type_name, is_active) [name: 'ix_cpd_tenant_entity_active']
    (tenant_id, entity_type_name, is_searchable, is_filterable) [name: 'ix_cpd_tenant_entity_search_filter']
  }
}

Table "custom_property_options" {
  "id" uuid [pk, not null]
  "custom_property_definition_id" uuid [not null]
  "namespace" varchar(100) [not null]
  "key" varchar(100) [not null]
  "display_name" varchar(200) [not null]
  "description" varchar(500)
  "value" varchar(500) [not null]
  "is_default" boolean [not null]
  "is_active" boolean [not null]
  "sort_order" int [not null]
  "parent_option_id" uuid
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (custom_property_definition_id, sort_order) [name: 'ix_cpo_definition_sort']
    (custom_property_definition_id, namespace, key) [unique, name: 'ix_cpo_definition_namespace_key']
  }
}

Table "custom_property_values" {
  "id" uuid [pk, not null]
  "custom_property_definition_id" uuid [not null]
  "entity_id" uuid [not null]
  "ordinal" int [not null]
  "text_value" varchar(4000)
  "number_value" decimal(19,4)
  "boolean_value" boolean
  "date_time_value" timestamptz
  "option_id" uuid
  "tenant_id" uuid [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (tenant_id, entity_id) [name: 'ix_cpv_tenant_entity']
    (custom_property_definition_id, entity_id, ordinal) [unique, name: 'ix_cpv_definition_entity_ordinal']
    (tenant_id, custom_property_definition_id) [name: 'ix_cpv_tenant_definition']
  }
}

// ============================================================
// Event Templates & Event Custom Properties
// ============================================================

Table "event_templates" {
  "id" uuid [pk, not null]
  "tenant_id" uuid [not null]
  "template_key" varchar(100) [not null]
  "display_name" varchar(200) [not null]
  "description" varchar(500)
  "event_type_id" int
  "version" int [not null]
  "is_published" boolean [not null]
  "is_active" boolean [not null]
  "sort_order" int [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (tenant_id, template_key, version) [unique, name: 'ix_event_templates_tenant_key_version']
    (tenant_id, is_published, is_active) [name: 'ix_event_templates_tenant_published_active']
  }
}

Table "event_template_custom_property_definitions" {
  "id" uuid [pk, not null]
  "event_template_id" uuid [not null]
  "tenant_id" uuid [not null]
  "namespace" varchar(100) [not null]
  "key" varchar(100) [not null]
  "display_name" varchar(200) [not null]
  "description" varchar(500)
  "property_type" int [not null]
  "is_required" boolean [not null]
  "is_multi" boolean [not null]
  "is_active" boolean [not null]
  "sort_order" int [not null]
  "exposure_level" int [not null]
  "is_searchable" boolean [not null]
  "is_filterable" boolean [not null]
  "is_exportable" boolean [not null]
  "is_moderation_relevant" boolean [not null]
  "is_analytics_relevant" boolean [not null]
  "is_system_owned" boolean [not null]
  "default_text_value" varchar(1000)
  "default_number_value" decimal(19,4)
  "default_boolean_value" boolean
  "default_date_time_value" timestamptz
  "default_option_id" uuid
  "min_length" int
  "max_length" int
  "regex_pattern" varchar(1000)
  "min_number" decimal(19,4)
  "max_number" decimal(19,4)
  "min_date_time" timestamptz
  "max_date_time" timestamptz
  "allowed_url_schemes" varchar(500)
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (event_template_id, namespace, key) [unique, name: 'ix_etcpd_template_namespace_key']
    (tenant_id, is_searchable, is_filterable) [name: 'ix_etcpd_tenant_search_filter']
  }
}

Table "event_template_custom_property_options" {
  "id" uuid [pk, not null]
  "event_template_custom_property_definition_id" uuid [not null]
  "namespace" varchar(100) [not null]
  "key" varchar(100) [not null]
  "display_name" varchar(200) [not null]
  "description" varchar(500)
  "value" varchar(500) [not null]
  "is_default" boolean [not null]
  "is_active" boolean [not null]
  "sort_order" int [not null]
  "parent_option_id" uuid
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (event_template_custom_property_definition_id, sort_order) [name: 'ix_etcpo_definition_sort']
    (event_template_custom_property_definition_id, namespace, key) [unique, name: 'ix_etcpo_definition_namespace_key']
  }
}

Table "event_custom_property_definitions" {
  "id" uuid [pk, not null]
  "event_id" uuid [not null]
  "tenant_id" uuid [not null]
  "namespace" varchar(100) [not null]
  "key" varchar(100) [not null]
  "display_name" varchar(200) [not null]
  "description" varchar(500)
  "property_type" int [not null]
  "is_required" boolean [not null]
  "is_multi" boolean [not null]
  "is_active" boolean [not null]
  "sort_order" int [not null]
  "exposure_level" int [not null]
  "is_searchable" boolean [not null]
  "is_filterable" boolean [not null]
  "is_exportable" boolean [not null]
  "is_moderation_relevant" boolean [not null]
  "is_analytics_relevant" boolean [not null]
  "is_system_owned" boolean [not null]
  "default_text_value" varchar(1000)
  "default_number_value" decimal(19,4)
  "default_boolean_value" boolean
  "default_date_time_value" timestamptz
  "default_option_id" uuid
  "min_length" int
  "max_length" int
  "regex_pattern" varchar(1000)
  "min_number" decimal(19,4)
  "max_number" decimal(19,4)
  "min_date_time" timestamptz
  "max_date_time" timestamptz
  "allowed_url_schemes" varchar(500)
  "source_template_id" uuid
  "source_template_key" varchar(100)
  "source_template_version" int
  "source_template_definition_id" uuid
  "instantiated_at" timestamptz [not null]
  "last_synced_from_template_at" timestamptz
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (event_id, namespace, key) [unique, name: 'ix_ecpd_event_namespace_key']
    (tenant_id, event_id, is_searchable, is_filterable) [name: 'ix_ecpd_tenant_event_search_filter']
  }
}

Table "event_custom_property_options" {
  "id" uuid [pk, not null]
  "event_custom_property_definition_id" uuid [not null]
  "namespace" varchar(100) [not null]
  "key" varchar(100) [not null]
  "display_name" varchar(200) [not null]
  "description" varchar(500)
  "value" varchar(500) [not null]
  "is_default" boolean [not null]
  "is_active" boolean [not null]
  "sort_order" int [not null]
  "parent_option_id" uuid
  "source_template_option_id" uuid
  "source_template_version" int
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (event_custom_property_definition_id, sort_order) [name: 'ix_ecpo_definition_sort']
    (event_custom_property_definition_id, namespace, key) [unique, name: 'ix_ecpo_definition_namespace_key']
  }
}

Table "event_custom_property_values" {
  "id" uuid [pk, not null]
  "event_custom_property_definition_id" uuid [not null]
  "event_id" uuid [not null]
  "tenant_id" uuid [not null]
  "ordinal" int [not null]
  "text_value" varchar(4000)
  "number_value" decimal(19,4)
  "boolean_value" boolean
  "date_time_value" timestamptz
  "option_id" uuid
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (tenant_id, event_id) [name: 'ix_ecpv_tenant_event']
    (event_custom_property_definition_id, event_id, ordinal) [unique, name: 'ix_ecpv_definition_event_ordinal']
  }
}

Table "event_custom_property_projections" {
  "id" uuid [pk, not null]
  "event_custom_property_definition_id" uuid [not null]
  "event_custom_property_value_id" uuid [not null]
  "event_id" uuid [not null]
  "tenant_id" uuid [not null]
  "namespace" varchar(100) [not null]
  "key" varchar(100) [not null]
  "property_type" int [not null]
  "exposure_level" int [not null]
  "is_searchable" boolean [not null]
  "is_filterable" boolean [not null]
  "is_exportable" boolean [not null]
  "is_moderation_relevant" boolean [not null]
  "is_analytics_relevant" boolean [not null]
  "ordinal" int [not null]
  "option_id" uuid
  "text_value" varchar(4000)
  "number_value" decimal(19,4)
  "boolean_value" boolean
  "date_time_value" timestamptz
  "normalized_value" varchar(4000)
  "updated_at" timestamptz [not null]

  indexes {
    event_custom_property_value_id [unique, name: 'ix_ecpp_value']
    (tenant_id, namespace, key, normalized_value) [name: 'ix_ecpp_tenant_namespace_key_normalized']
    (tenant_id, exposure_level) [name: 'ix_ecpp_tenant_exposure']
    (tenant_id, event_id, namespace, key, ordinal) [name: 'ix_ecpp_tenant_event_namespace_key_ordinal']
  }
}

// ============================================================
// Event Session Templates & Session Custom Properties
// ============================================================

Table "event_session_templates" {
  "id" uuid [pk, not null]
  "event_template_id" uuid [not null]
  "tenant_id" uuid [not null]
  "session_template_key" varchar(100) [not null]
  "display_name" varchar(200) [not null]
  "description" varchar(500)
  "version" int [not null]
  "is_published" boolean [not null]
  "is_active" boolean [not null]
  "sort_order" int [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (event_template_id, session_template_key, version) [unique, name: 'ix_est_template_key_version']
    (tenant_id, is_published, is_active) [name: 'ix_est_tenant_published_active']
  }
}

Table "event_session_template_custom_property_definitions" {
  "id" uuid [pk, not null]
  "event_session_template_id" uuid [not null]
  "tenant_id" uuid [not null]
  "namespace" varchar(100) [not null]
  "key" varchar(100) [not null]
  "display_name" varchar(200) [not null]
  "description" varchar(500)
  "property_type" int [not null]
  "is_required" boolean [not null]
  "is_multi" boolean [not null]
  "is_active" boolean [not null]
  "sort_order" int [not null]
  "exposure_level" int [not null]
  "is_searchable" boolean [not null]
  "is_filterable" boolean [not null]
  "is_exportable" boolean [not null]
  "is_moderation_relevant" boolean [not null]
  "is_analytics_relevant" boolean [not null]
  "is_system_owned" boolean [not null]
  "default_text_value" varchar(1000)
  "default_number_value" decimal(19,4)
  "default_boolean_value" boolean
  "default_date_time_value" timestamptz
  "default_option_id" uuid
  "min_length" int
  "max_length" int
  "regex_pattern" varchar(1000)
  "min_number" decimal(19,4)
  "max_number" decimal(19,4)
  "min_date_time" timestamptz
  "max_date_time" timestamptz
  "allowed_url_schemes" varchar(500)
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (event_session_template_id, namespace, key) [unique, name: 'ix_estcpd_template_namespace_key']
    (tenant_id, is_searchable, is_filterable) [name: 'ix_estcpd_tenant_search_filter']
  }
}

Table "event_session_template_custom_property_options" {
  "id" uuid [pk, not null]
  "event_session_template_custom_property_definition_id" uuid [not null]
  "namespace" varchar(100) [not null]
  "key" varchar(100) [not null]
  "display_name" varchar(200) [not null]
  "description" varchar(500)
  "value" varchar(500) [not null]
  "is_default" boolean [not null]
  "is_active" boolean [not null]
  "sort_order" int [not null]
  "parent_option_id" uuid
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (event_session_template_custom_property_definition_id, sort_order) [name: 'ix_estcpo_definition_sort']
    (event_session_template_custom_property_definition_id, namespace, key) [unique, name: 'ix_estcpo_definition_namespace_key']
  }
}

Table "event_session_custom_property_definitions" {
  "id" uuid [pk, not null]
  "event_session_id" uuid [not null]
  "tenant_id" uuid [not null]
  "namespace" varchar(100) [not null]
  "key" varchar(100) [not null]
  "display_name" varchar(200) [not null]
  "description" varchar(500)
  "property_type" int [not null]
  "is_required" boolean [not null]
  "is_multi" boolean [not null]
  "is_active" boolean [not null]
  "sort_order" int [not null]
  "exposure_level" int [not null]
  "is_searchable" boolean [not null]
  "is_filterable" boolean [not null]
  "is_exportable" boolean [not null]
  "is_moderation_relevant" boolean [not null]
  "is_analytics_relevant" boolean [not null]
  "is_system_owned" boolean [not null]
  "default_text_value" varchar(1000)
  "default_number_value" decimal(19,4)
  "default_boolean_value" boolean
  "default_date_time_value" timestamptz
  "default_option_id" uuid
  "min_length" int
  "max_length" int
  "regex_pattern" varchar(1000)
  "min_number" decimal(19,4)
  "max_number" decimal(19,4)
  "min_date_time" timestamptz
  "max_date_time" timestamptz
  "allowed_url_schemes" varchar(500)
  "source_template_id" uuid
  "source_template_key" varchar(100)
  "source_template_version" int
  "source_template_definition_id" uuid
  "instantiated_at" timestamptz [not null]
  "last_synced_from_template_at" timestamptz
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (event_session_id, namespace, key) [unique, name: 'ix_escpd_session_namespace_key']
    (tenant_id, event_session_id, is_searchable, is_filterable) [name: 'ix_escpd_tenant_session_search_filter']
  }
}

Table "event_session_custom_property_options" {
  "id" uuid [pk, not null]
  "event_session_custom_property_definition_id" uuid [not null]
  "namespace" varchar(100) [not null]
  "key" varchar(100) [not null]
  "display_name" varchar(200) [not null]
  "description" varchar(500)
  "value" varchar(500) [not null]
  "is_default" boolean [not null]
  "is_active" boolean [not null]
  "sort_order" int [not null]
  "parent_option_id" uuid
  "source_template_option_id" uuid
  "source_template_version" int
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (event_session_custom_property_definition_id, sort_order) [name: 'ix_escpo_definition_sort']
    (event_session_custom_property_definition_id, namespace, key) [unique, name: 'ix_escpo_definition_namespace_key']
  }
}

Table "event_session_custom_property_values" {
  "id" uuid [pk, not null]
  "event_session_custom_property_definition_id" uuid [not null]
  "event_session_id" uuid [not null]
  "tenant_id" uuid [not null]
  "ordinal" int [not null]
  "text_value" varchar(4000)
  "number_value" decimal(19,4)
  "boolean_value" boolean
  "date_time_value" timestamptz
  "option_id" uuid
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (tenant_id, event_session_id) [name: 'ix_escpv_tenant_session']
    (event_session_custom_property_definition_id, event_session_id, ordinal) [unique, name: 'ix_escpv_definition_session_ordinal']
  }
}

Table "event_session_custom_property_projections" {
  "id" uuid [pk, not null]
  "event_session_custom_property_definition_id" uuid [not null]
  "event_session_custom_property_value_id" uuid [not null]
  "event_session_id" uuid [not null]
  "tenant_id" uuid [not null]
  "namespace" varchar(100) [not null]
  "key" varchar(100) [not null]
  "property_type" int [not null]
  "exposure_level" int [not null]
  "is_searchable" boolean [not null]
  "is_filterable" boolean [not null]
  "is_exportable" boolean [not null]
  "is_moderation_relevant" boolean [not null]
  "is_analytics_relevant" boolean [not null]
  "ordinal" int [not null]
  "option_id" uuid
  "text_value" varchar(4000)
  "number_value" decimal(19,4)
  "boolean_value" boolean
  "date_time_value" timestamptz
  "normalized_value" varchar(4000)
  "updated_at" timestamptz [not null]

  indexes {
    event_session_custom_property_value_id [unique, name: 'ix_escpp_value']
    (tenant_id, namespace, key, normalized_value) [name: 'ix_escpp_tenant_namespace_key_normalized']
    (tenant_id, exposure_level) [name: 'ix_escpp_tenant_exposure']
    (tenant_id, event_session_id, namespace, key, ordinal) [name: 'ix_escpp_tenant_session_namespace_key_ordinal']
  }
}

// ============================================================
// Locations
// ============================================================

Table "locations" {
  "id" uuid [pk, not null, note: 'uuidv7 app-side']
  "full_name" varchar(500) [not null]
  "country" varchar(500) [not null]
  "city" varchar(500) [not null]
  "tenant_id" uuid [not null]
  "timezone" varchar(500)

  indexes {
    tenant_id
    (tenant_id, city) [name: 'ix_locations_tenant_city']
    (tenant_id, country) [name: 'ix_locations_tenant_country']
  }
}

Table "location_rooms" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "location_id" uuid [not null]
  "tenant_id" uuid [not null]
  "name" varchar(200) [not null]
  "slug" varchar(200)
  "description" varchar(2000)
  "capacity" int
  "sort_order" int [not null, default: 0]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (tenant_id, location_id, name) [unique, name: 'ix_location_rooms_tenant_location_name']
    (tenant_id, location_id, sort_order) [name: 'ix_location_rooms_tenant_location_sort']
  }

  Note: 'Sub-venue within a location (e.g. Conference Room A, Main Hall). Used for room-based agenda grid layout. Soft-deletable, tenant-scoped.'
}

Table "location_pii" {
  "location_id" uuid [pk, not null, note: 'shared PK with locations']
  "address" varchar(500) [not null]
  "postcode" varchar(500) [not null]
  "latitude" doubleprecision
  "longitude" doubleprecision
}

// ============================================================
// Actors
// ============================================================

Table "actors" {
  "id" uuid [pk, not null, note: 'uuidv7 app-side']
  "actor_type_id" int [not null]
  "user_id" uuid
  "organization_id" uuid
  "group_id" uuid
  "tenant_id" uuid [not null]
  "profile_picture_id" uuid
  "banner_picture_id" uuid
  "background_color" varchar(50)
  "background_effect" varchar(50)
  "banner_color" varchar(50)
  "did_custody_type_id" int
  "pds_host" varchar(500)
  "description" varchar(500)
  "indexed_at" timestamptz
  "profile_picture_cid" varchar(500)
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid

  indexes {
    user_id [unique, note: 'filtered: user_id IS NOT NULL']
    organization_id [unique, note: 'filtered: organization_id IS NOT NULL']
    group_id [unique, note: 'filtered: group_id IS NOT NULL']
    profile_picture_id [name: 'ix_actors_profile_picture_id']
    banner_picture_id [name: 'ix_actors_banner_picture_id']
    (tenant_id) [name: 'ix_actors_tenant_id']
  }

  Note: 'Federated identity (User|Org|Group|Bot). Check: CK_Actor_UserOrOrganization ensures exactly one owner FK or none (bot/service).'
}

Table "actor_pii" {
  "actor_id" uuid [pk, not null, note: 'shared PK with actors']
  "display_name" varchar(500) [not null]
  "did" varchar(500)
  "handle" varchar(500)
  "profile_picture_uri" varchar(500)

  indexes {
    did [name: 'ix_actor_pii_did']
    handle [name: 'ix_actor_pii_handle']
  }
}

Table "actor_key_stores" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "actor_id" uuid [not null]
  "tenant_id" uuid [not null]
  "key_purpose" varchar(50) [not null]
  "private_key_encrypted" text [not null]
  "public_key" varchar(500) [not null]
  "is_active" boolean
  "created_at" timestamptz
}

// ============================================================
// Users
// ============================================================

Table "users" {
  "id" uuid [pk, not null, note: 'uuidv7 app-side']
  "actor_id" uuid
  "auth_provider" varchar(500)
  "auth_provider_id" varchar(500)
  "default_actor_id" uuid
  "email_verified" boolean
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid

  Note: 'Platform user identity. PII stored in separate user_pii table.'
}

Table "user_pii" {
  "user_id" uuid [pk, not null, note: 'shared PK with users']
  "email" varchar(320) [not null]
  "first_name" varchar(500) [not null]
  "last_name" varchar(500) [not null]

  indexes {
    email [unique]
  }
}

Table "platform_user_roles" {
  "id" uuid [pk, not null, note: 'uuidv7 app-side']
  "user_id" uuid [not null]
  "role_id" int [not null]
  "granted_at" timestamptz [not null]
  "granted_by" uuid

  indexes {
    (user_id, role_id) [unique]
    user_id
  }
}

Table "user_authentication_tokens" {
  "id" uuid [pk, not null, note: 'uuidv7 app-side']
  "user_id" uuid [not null]
  "tenant_id" uuid [not null]
  "provider" varchar(500) [not null]
  "access_token" varchar(500)
  "refresh_token" varchar(500)
  "pds_host" varchar(500)
  "dpop_key" varchar(500)
  "id_token" varchar(500)
  "expires_at" timestamptz
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
}

Table "user_external_logins" {
  "id" uuid [pk, not null, note: 'uuidv7 app-side']
  "user_id" uuid [not null]
  "tenant_id" uuid [not null]
  "provider" varchar(255)
  "provider_key" varchar(500)
  "provider_display_name" varchar(500)
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
}

Table "user_preferences" {
  "id" uuid [pk, not null, note: 'uuidv7 app-side']
  "tenant_id" uuid [not null]
  "user_id" uuid [not null]
  "setting_key" varchar(256) [not null]
  "value" text [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    (tenant_id, user_id, setting_key) [unique, name: 'ix_user_preferences_tenant_id_user_id_setting_key']
  }
}

Table "user_notification_preferences" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "tenant_id" uuid [not null]
  "user_id" uuid [not null]
  "category" varchar(100) [not null]
  "is_email_enabled" boolean [not null]
  "is_push_enabled" boolean [not null]
  "is_in_app_enabled" boolean [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    (tenant_id, user_id, category) [unique]
  }
}

Table "user_appearance_profiles" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "user_id" uuid [not null]
  "tenant_id" uuid
  "name" varchar(200) [not null]
  "description" varchar(1000)
  "is_default" boolean [not null]
  "is_archived" boolean [not null]
  "theme_mode" int [not null]
  "theme_key" varchar(128)
  "source_preset_id" uuid
  "light_primary" varchar(7)
  "light_secondary" varchar(7)
  "light_background" varchar(7)
  "light_surface" varchar(7)
  "dark_primary" varchar(7)
  "dark_secondary" varchar(7)
  "dark_background" varchar(7)
  "dark_surface" varchar(7)
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    (user_id, tenant_id, name)
    (user_id, tenant_id, is_archived)
    (user_id, tenant_id, is_default) [unique, name: 'ix_user_appearance_profiles_is_default', note: 'filter: is_default = true AND is_archived = false']
    (user_id, source_preset_id)
  }
}

Table "user_appearance_preferences" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "user_id" uuid [not null]
  "tenant_id" uuid
  "active_profile_id" uuid
  "resolution_source" int [not null]
  "last_resolved_at" timestamptz [not null]

  indexes {
    (user_id, tenant_id) [unique]
  }
}

// ============================================================
// Organizations
// ============================================================

Table "organizations" {
  "id" uuid [pk, not null, note: 'uuidv7 app-side']
  "website_url" varchar(2048)
  "approval_status_id" int [not null]
  "tenant_id" uuid [not null]
  "actor_id" uuid
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "approved_at" timestamptz
  "approved_by" uuid
  "approval_notes" text
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (tenant_id, is_deleted, approval_status_id) [name: 'ix_organizations_tenant_active_status']
    tenant_id [name: 'ix_organizations_tenant']
  }

  Note: 'Approval-gated org. Soft-deletable, concurrency-protected.'
}

Table "organization_pii" {
  "organization_id" uuid [pk, not null, note: 'shared PK with organization']
  "full_name" varchar(500) [not null]
  "email" varchar(320)
  "country" varchar(200)
  "city" varchar(200)
  "address" varchar(500)
  "postcode" varchar(50)

  indexes {
    full_name [name: 'ix_organization_pii_name']
  }
}

Table "organization_members" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "organization_id" uuid [not null]
  "user_id" uuid [not null]
  "role_id" int [not null]
  "organization_position_id" int
  "tenant_id" uuid [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid

  indexes {
    (organization_id, user_id) [unique, name: 'ix_orgmembers_org_user']
    user_id [name: 'ix_orgmembers_user']
  }
}

Table "organization_reviews" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "organization_id" uuid [not null]
  "event_id" uuid [not null]
  "user_id" uuid [not null]
  "reviewer_name" varchar(200) [not null]
  "rating" int [not null]
  "comment" varchar(2000)
  "tenant_id" uuid [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
}

Table "organization_setting_overrides" {
  "id" uuid [pk, not null, note: 'uuidv7 app-side']
  "tenant_id" uuid [not null]
  "organization_id" uuid [not null]
  "setting_key" varchar(256) [not null]
  "value" text [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    (organization_id, setting_key) [unique]
  }
}

Table "organization_policy_sets" {
  "id" uuid [pk, not null]
  "organization_id" uuid [not null]
  "tenant_id" uuid [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "xmin" xid [not null, note: 'rowversion / optimistic concurrency']
  "events_policy" jsonb [not null]

  indexes {
    organization_id [unique, name: 'ix_organization_policy_sets_organization_id']
  }
}

// ============================================================
// Groups
// ============================================================

Table "groups" {
  "id" uuid [pk, not null, note: 'uuidv7 app-side']
  "full_name" varchar(500) [not null]
  "description" varchar(5000)
  "profile_picture_id" uuid
  "approval_status_id" int [not null]
  "tenant_id" uuid [not null]
  "actor_id" uuid
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (tenant_id, is_deleted, approval_status_id) [name: 'ix_groups_tenant_active']
  }

  Note: 'Community groups. Approval-gated, soft-deletable, concurrency-protected.'
}

Table "group_members" {
  "id" uuid [pk, not null, note: 'uuidv7 app-side']
  "group_id" uuid [not null]
  "user_id" uuid [not null]
  "role_id" int [not null]
  "group_position_id" int
  "tenant_id" uuid [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid

  indexes {
    (group_id, user_id) [unique]
  }
}

Table "group_setting_overrides" {
  "id" uuid [pk, not null, note: 'uuidv7 app-side']
  "tenant_id" uuid [not null]
  "group_id" uuid [not null]
  "setting_key" varchar(256) [not null]
  "value" text [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    (group_id, setting_key) [unique]
  }
}

// ============================================================
// Storage
// ============================================================

Table "storage_objects" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "file_type_id" int [not null]
  "uri" varchar(1000) [not null]
  "full_name" varchar(500) [not null]
  "extension" varchar(50) [not null]
  "size" bigint [not null]
  "tenant_id" uuid [not null]
  "actor_id" uuid
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    tenant_id
    actor_id
  }

  Note: 'File metadata. Actual blobs stored in external object storage.'
}

// ============================================================
// Events
// ============================================================

Table "event_series" {
  "id" uuid [pk, not null]
  "title" varchar(200) [not null]
  "slug" varchar(200)
  "description" varchar(2000)
  "featured_image_id" uuid
  "actor_id" uuid [not null]
  "is_published" boolean [not null]
  "total_views" int [not null]
  "visibility_type_id" int [not null]
  "start_date_utc" timestamptz
  "end_date_utc" timestamptz
  "tenant_id" uuid [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (tenant_id, slug) [unique, name: 'ix_event_series_tenant_id_slug']
    (tenant_id, is_published) [name: 'ix_event_series_tenant_id_is_published']
    (tenant_id, total_views) [name: 'ix_event_series_tenant_id_total_views']
  }
}

Table "events" {
  "id" uuid [pk, not null, note: 'uuidv7 app-side']
  "event_type_id" int
  "title" varchar(200) [not null]
  "subtitle" varchar(200)
  "description" varchar(5000)
  "audience_gender_id" int
  "audience_age_id" int
  "actor_id" uuid [not null]
  "price" decimal(19,4)
  "currency_code" varchar(3)
  "featured_image_id" uuid
  "total_views" int [not null]
  "is_registration_required" boolean [not null]
  "is_user_reported" boolean [not null]
  "event_url" varchar(2048)
  "madhab_id" int
  "tenant_id" uuid [not null]
  "slug" varchar(200)
  "visibility_type_id" int [not null]
  "session_count" int
  "event_status_id" int [not null]
  "external_registration_url" varchar(2048)
  "first_session_date" date
  "last_session_date" date
  "timezone" varchar(100)
  "first_session_start_utc" timestamptz
  "last_session_start_utc" timestamptz
  "event_time_zone_id" text
  "event_series_id" uuid
  "series_order" int
  "registration_policy_id" int [note: 'FK to event_registration_policies. Null = Flexible.']
  "event_format_id" int [not null]
  "atproto_record_id" uuid
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null]
  "background_color" varchar(50)
  "background_effect" varchar(50)
  "background_image_id" uuid

  indexes {
    (tenant_id, is_deleted, event_status_id) [name: 'ix_events_tenant_active_status']
    (tenant_id, actor_id, created_at) [name: 'ix_events_tenant_actor_created']
    (tenant_id, first_session_date, last_session_date) [name: 'ix_events_tenant_daterange']
    (tenant_id, event_type_id) [name: 'ix_events_tenant_eventtype']
    (tenant_id, slug) [name: 'ix_events_tenant_slug']
  }

  Note: 'Core aggregate. Check: CK_Event_NonNegativePrice (price >= 0). Soft-deletable, tenant-scoped, concurrency-protected.'
}

Table "event_islamic_aspects" {
  "id" uuid [pk, not null, note: 'shared PK with events']
  "madhab_id" int
  "reference_prayer" int
  "prayer_time_offset" int
  "gender_mode" int [not null]
  "includes_quran_recitation" boolean [not null]
  "primary_language_id" int
}

Table "event_tech_aspects" {
  "id" uuid [pk, not null, note: 'shared PK with events']
  "github_repo_url" varchar(2048)
  "hackathon_track" varchar(200)
  "skill_level" int [not null]
  "tech_stack_tags" varchar(1000)
  "requires_laptop" boolean [not null]
  "is_coding_competition" boolean [not null]
  "max_team_size" int
  "prize_pool" decimal(19,4)
  "prize_currency_code" varchar(3)
}

Table "event_days" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "event_id" uuid [not null]
  "tenant_id" uuid [not null]
  "local_date" date [not null, note: 'calendar date in event timezone']
  "label" varchar(200)
  "description" varchar(2000)
  "banner_text" varchar(500)
  "banner_image_id" uuid
  "is_published" boolean [not null, default: false]
  "sort_order" int [not null, default: 0]
  "allows_day_scope_registration" boolean [not null, default: false]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (tenant_id, event_id, local_date) [unique, name: 'ix_event_days_tenant_event_local_date', note: 'partial: is_deleted = false']
    (tenant_id, event_id, sort_order) [name: 'ix_event_days_tenant_event_sort']
    (tenant_id, event_id, is_published) [name: 'ix_event_days_tenant_event_published']
  }

  Note: 'Calendar-day grouping for multi-day events.'
}

Table "event_agenda_items" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "event_id" uuid [not null]
  "event_day_id" uuid
  "tenant_id" uuid [not null]
  "title" varchar(500) [not null]
  "description" varchar(2000)
  "start_time" timestamptz [not null]
  "end_time" timestamptz [not null]
  "local_start_date" date [not null]
  "local_end_date" date [not null]
  "local_start_time" time [not null]
  "local_end_time" time [not null]
  "local_start_minute_of_day" int [not null]
  "local_end_minute_of_day" int [not null]
  "location_id" uuid
  "room_id" uuid
  "kind_id" int
  "sort_order" int [not null, default: 0]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (tenant_id, event_id, local_start_date, local_start_minute_of_day) [name: 'ix_event_agenda_items_tenant_event_local_start']
    (tenant_id, event_id, sort_order) [name: 'ix_event_agenda_items_tenant_event_sort']
  }

  Note: 'Non-session schedule entries (breaks, prayers). Check: CK_EventAgendaItem_DurationPositive.'
}

Table "event_sessions" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "event_id" uuid [not null]
  "event_day_id" uuid
  "start_time" timestamptz [not null]
  "end_time" timestamptz [not null]
  "location_id" uuid
  "room_id" uuid
  "title" varchar(500)
  "tenant_id" uuid [not null]
  "slug" varchar(200)
  "max_audience_attendees" int
  "current_audience_attendees" int
  "registration_mode_id" int
  "price" decimal(19,4)
  "currency_code" varchar(3)
  "description" varchar(500)
  "sort_order" int [not null, default: 0]
  "local_start_date" date [not null]
  "local_end_date" date [not null]
  "local_start_time" time [not null]
  "local_end_time" time [not null]
  "local_start_minute_of_day" int [not null]
  "local_end_minute_of_day" int [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (tenant_id, event_id, local_start_date, local_start_minute_of_day) [name: 'ix_event_sessions_tenant_event_local_start']
    (tenant_id, room_id, start_time, end_time) [name: 'ix_event_sessions_tenant_room_time']
    (tenant_id, event_day_id, sort_order) [name: 'ix_event_sessions_tenant_day_sort']
  }

  Note: 'Check: CK_EventSession_NonNegativePrice, CK_EventSession_DurationPositive.'
}

Table "event_session_islamic_aspects" {
  "event_session_id" uuid [pk, not null, note: 'shared PK with event_sessions']
  "start_time_type" int [not null, default: 1]
  "reference_prayer" int
  "offset_minutes" int
  "requires_wudu" boolean [not null, default: false]
  "ritual_requirements_json" jsonb

  Note: 'Check: CK_EventSessionIslamicAspect_RelativeStartFields.'
}

Table "event_session_agenda_items" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "event_session_id" uuid [not null]
  "start_time" timestamptz [not null]
  "end_time" timestamptz [not null]
  "title" varchar(500) [not null]
  "description" varchar(500)
  "location_id" uuid
  "tenant_id" uuid [not null]
}

Table "event_session_languages" {
  "id" int [pk, not null, note: 'auto-increment']
  "event_session_id" uuid [not null]
  "language_id" int [not null]
  "tenant_id" uuid [not null]

  indexes {
    (event_session_id, language_id) [unique]
  }
}

Table "event_session_speakers" {
  "id" uuid [pk, not null]
  "actor_id" uuid [not null]
  "event_session_id" uuid [not null]
  "tenant_id" uuid [not null]

  indexes {
    (tenant_id, event_session_id, actor_id) [unique, name: 'ix_event_session_speakers_tenant_session_actor']
  }
}

Table "event_categories" {
  "id" uuid [pk, not null]
  "event_id" uuid [not null]
  "category_id" uuid [not null]
  "tenant_id" uuid [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    (tenant_id, event_id, category_id) [unique, name: 'ix_event_categories_tenant_event_category']
  }
}

Table "event_tags" {
  "id" uuid [pk, not null]
  "event_id" uuid [not null]
  "tag_id" uuid [not null]
  "tenant_id" uuid [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    (tenant_id, event_id, tag_id) [unique, name: 'ix_event_tags_tenant_event_tag']
  }
}

Table "event_registrations" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "user_id" uuid [not null]
  "event_session_id" uuid [not null]
  "event_registration_intent_id" uuid
  "approval_status_id" int
  "tenant_id" uuid [not null]
  "atproto_record_id" uuid
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid

  indexes {
    (event_session_id, user_id) [unique, name: 'ix_eventregistrations_session_user']
    user_id [name: 'ix_eventregistrations_user']
    event_registration_intent_id [name: 'ix_eventregistrations_intent']
  }
}

Table "event_registration_intents" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "event_id" uuid [not null]
  "user_id" uuid [not null]
  "registration_scope_id" int [not null]
  "selected_event_day_id" uuid
  "registration_policy_snapshot_id" int
  "approval_status_id" int
  "tenant_id" uuid [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (tenant_id, event_id, user_id, registration_scope_id) [name: 'ix_event_registration_intents_tenant_event_user_scope']
    (tenant_id, event_id, selected_event_day_id) [name: 'ix_event_registration_intents_tenant_event_day']
    (tenant_id, user_id) [name: 'ix_event_registration_intents_tenant_user']
    (tenant_id, event_id, user_id) [unique, name: 'ix_event_registration_intents_unique_event_scope', note: 'filter: scope=1']
    (tenant_id, event_id, user_id, selected_event_day_id) [unique, name: 'ix_event_registration_intents_unique_day_scope', note: 'filter: scope=2']
    (tenant_id, event_id, user_id) [unique, name: 'ix_event_registration_intents_unique_session_selection_scope', note: 'filter: scope=3']
  }
}

Table "event_session_categories" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "event_session_id" uuid [not null]
  "category_id" uuid [not null]
  "tenant_id" uuid [not null]

  indexes {
    (event_session_id, category_id) [unique]
    (tenant_id, event_session_id, category_id) [name: 'ix_event_session_categories_tenant_session_category']
  }
}

Table "event_session_tags" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "event_session_id" uuid [not null]
  "tag_id" uuid [not null]
  "tenant_id" uuid [not null]

  indexes {
    (event_session_id, tag_id) [unique]
    (tenant_id, event_session_id, tag_id) [name: 'ix_event_session_tags_tenant_session_tag']
  }
}

Table "event_contact_share_consents" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "tenant_id" uuid [not null]
  "source_event_id" uuid
  "user_id" uuid [not null]
  "recipient_actor_id" uuid [not null]
  "source_event_registration_intent_id" uuid
  "purpose_code" varchar(100) [not null]
  "status" int [not null]
  "email_snapshot" varchar(320) [not null]
  "email_normalized_snapshot" varchar(320) [not null]
  "consent_text_snapshot" text [not null]
  "consent_ui_version" varchar(100) [not null]
  "granted_at" timestamptz [not null]
  "withdrawn_at" timestamptz
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    (tenant_id, user_id, recipient_actor_id, purpose_code) [unique, name: 'ix_eventcontactshareconsents_scope_unique']
    (tenant_id, recipient_actor_id, status) [name: 'ix_eventcontactshareconsents_recipient_status']
    (tenant_id, user_id, status) [name: 'ix_eventcontactshareconsents_user_status']
  }
}

Table "event_contact_share_export" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "tenant_id" uuid [not null]
  "recipient_actor_id" uuid [not null]
  "event_id" uuid
  "exported_by_user_id" uuid [not null]
  "format" varchar(20) [not null]
  "row_count" int [not null]
  "created_at" timestamptz [not null]

  indexes {
    (tenant_id, recipient_actor_id, created_at) [name: 'ix_eventcontactshareexports_recipient_date']
  }
}

Table "event_contact_share_export_item" {
  "export_id" uuid [pk, not null]
  "consent_id" uuid [pk, not null]
  "email_snapshot" varchar(320) [not null]
}

// ============================================================
// Audit & Notifications
// ============================================================

Table "audit_logs" {
  "id" uuid [pk, not null, note: 'uuidv7 app-side']
  "entity_type" varchar(200) [not null]
  "entity_id" varchar(200) [not null]
  "action" varchar(50) [not null]
  "old_values" jsonb
  "new_values" jsonb
  "affected_columns" jsonb
  "actor_id" uuid
  "timestamp" timestamptz [not null]
  "tenant_id" uuid [not null]

  indexes {
    (entity_type, entity_id) [name: 'IX_audit_logs_entity_type_entity_id']
    tenant_id [name: 'IX_audit_logs_tenant_id']
    (tenant_id, entity_type, entity_id, timestamp) [name: 'ix_audit_logs_desc', note: 'descending: timestamp']
  }
}

Table "notifications" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "tenant_id" uuid [not null]
  "user_id" uuid [not null]
  "notification_type_id" int [not null]
  "notification_reason_id" int [not null]
  "source_entity_type_id" int [not null]
  "source_entity_id" uuid [not null]
  "title" varchar(200) [not null]
  "content" varchar(2000) [not null]
  "action_url" varchar(1000)
  "is_read" boolean [not null]
  "read_at" timestamptz
  "is_archived" boolean [not null]
  "archived_at" timestamptz
  "notification_scope_id" uuid
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid

  indexes {
    (tenant_id, user_id, is_read, created_at) [name: 'ix_notifications_tenant_user_unread', note: 'descending: created_at']
    (tenant_id, user_id, created_at) [name: 'ix_notifications_unread_by_user', note: 'descending: created_at']
    (tenant_id, notification_type_id) [name: 'ix_notifications_tenant_type']
    (user_id, notification_scope_id, is_read) [name: 'ix_notifications_user_scope']
    (user_id, is_archived, created_at) [name: 'ix_notifications_user_archived', note: 'descending: created_at']
  }
}

Table "policy_change_outbox" {
  "id" uuid [pk, not null]
  "scope" int [not null]
  "scope_id" uuid
  "operation" int [not null]
  "status" int [not null]
  "created_at" timestamptz [not null]
  "created_by" varchar(200)
  "processed_at" timestamptz
  "retry_count" int [not null]
  "last_error" varchar(2000)
  "next_retry_at" timestamptz

  indexes {
    (status, next_retry_at) [name: 'ix_policy_change_outbox_status_retry']
  }
}

Table "idempotency_records" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "key" varchar(255) [not null]
  "tenant_id" uuid [not null]
  "expires_at" timestamptz [not null]
  "response_status_code" int
  "response_body" text
  "created_at" timestamptz [not null]

  indexes {
    (key, tenant_id) [unique, name: 'IX_IdempotencyRecords_Key_TenantId']
    expires_at [name: 'IX_IdempotencyRecords_ExpiresAt']
  }
}

// ============================================================
// Views
// ============================================================

Table "event_with_sessions_view" {
  "event_id" uuid [pk]
  "tenant_id" uuid
  "event_title" varchar(200)
  "session_count" int
  "total_views" int
  "first_session_start_utc" timestamptz
  "last_session_end_utc" timestamptz
  "is_deleted" boolean

  Note: 'Materialized or standard view for listing performance.'
}

// ============================================================
// Relationships (Foreign Keys)
// ============================================================

// Tenants & Setup
Ref: "tenants"."tenant_status_id" > "tenant_statuses"."id" [delete: restrict]
Ref: "tenant_settings"."tenant_id" - "tenants"."id" [delete: cascade]
Ref: "tenant_setting_overrides"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "tenant_navigation_links"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "tenant_footer_link_groups"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "tenant_footer_links"."footer_link_group_id" > "tenant_footer_link_groups"."id" [delete: cascade]
Ref: "tenant_onboarding_states"."tenant_id" - "tenants"."id" [delete: cascade]
Ref: "tenant_policy_sets"."tenant_id" - "tenants"."id" [delete: cascade]
Ref: "tenant_capabilities"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "tenant_capabilities"."module_id" > "module_definitions"."id" [delete: restrict]
Ref: "tenant_invitations"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "tenant_members"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "tenant_members"."user_id" > "users"."id" [delete: cascade]

// Users & Roles
Ref: "user_pii"."user_id" - "users"."id" [delete: cascade]
Ref: "platform_user_roles"."user_id" > "users"."id" [delete: restrict]
Ref: "platform_user_roles"."role_id" > "roles"."id" [delete: restrict]
Ref: "role_permissions"."role_id" > "roles"."id" [delete: cascade]
Ref: "role_permissions"."permission_id" > "permissions"."id" [delete: cascade]
Ref: "user_authentication_tokens"."user_id" > "users"."id" [delete: restrict]
Ref: "user_external_logins"."user_id" > "users"."id" [delete: restrict]
Ref: "user_preferences"."user_id" > "users"."id" [delete: restrict]
Ref: "user_notification_preferences"."user_id" > "users"."id" [delete: restrict]
Ref: "user_appearance_profiles"."user_id" > "users"."id" [delete: restrict]
Ref: "user_appearance_preferences"."user_id" > "users"."id" [delete: restrict]
Ref: "user_appearance_preferences"."active_profile_id" > "user_appearance_profiles"."id" [delete: restrict]

// Actors & Identity
Ref: "actors"."actor_type_id" > "actor_types"."id" [delete: restrict]
Ref: "actors"."user_id" - "users"."id" [delete: restrict]
Ref: "actors"."organization_id" - "organizations"."id" [delete: restrict]
Ref: "actors"."group_id" - "groups"."id" [delete: restrict]
Ref: "actor_pii"."actor_id" - "actors"."id" [delete: cascade]
Ref: "actor_key_stores"."actor_id" > "actors"."id" [delete: cascade]

// Organizations & Groups
Ref: "organizations"."approval_status_id" > "approval_statuses"."id" [delete: restrict]
Ref: "organizations"."actor_id" - "actors"."id" [delete: restrict]
Ref: "organization_pii"."organization_id" - "organizations"."id" [delete: cascade]
Ref: "organization_members"."organization_id" > "organizations"."id" [delete: restrict]
Ref: "organization_members"."user_id" > "users"."id" [delete: restrict]
Ref: "organization_members"."organization_position_id" > "organization_positions"."id" [delete: restrict]
Ref: "organization_reviews"."organization_id" > "organizations"."id" [delete: restrict]
Ref: "organization_reviews"."event_id" > "events"."id" [delete: restrict]
Ref: "organization_setting_overrides"."organization_id" > "organizations"."id" [delete: restrict]
Ref: "organization_policy_sets"."organization_id" - "organizations"."id" [delete: cascade]
Ref: "groups"."approval_status_id" > "approval_statuses"."id" [delete: restrict]
Ref: "groups"."actor_id" - "actors"."id" [delete: restrict]
Ref: "group_members"."group_id" > "groups"."id" [delete: restrict]
Ref: "group_members"."user_id" > "users"."id" [delete: restrict]
Ref: "group_members"."group_position_id" > "group_positions"."id" [delete: restrict]
Ref: "group_setting_overrides"."group_id" > "groups"."id" [delete: restrict]

// Taxonomy
Ref: "categories"."parent_id" > "categories"."id" [delete: restrict]
Ref: "category_type_categories"."category_id" > "categories"."id" [delete: restrict]
Ref: "category_type_categories"."category_type_id" > "category_types"."id" [delete: restrict]
Ref: "tag_type_tags"."tag_id" > "tags"."id" [delete: restrict]
Ref: "tag_type_tags"."tag_type_id" > "tag_types"."id" [delete: restrict]

// Storage
Ref: "storage_objects"."file_type_id" > "file_types"."id" [delete: restrict]
Ref: "storage_objects"."actor_id" > "actors"."id" [delete: restrict]

// Events Core
Ref: "events"."event_type_id" > "event_types"."id" [delete: restrict]
Ref: "events"."actor_id" > "actors"."id" [delete: restrict]
Ref: "events"."event_status_id" > "event_statuses"."id" [delete: restrict]
Ref: "events"."event_format_id" > "event_formats"."id" [delete: restrict]
Ref: "events"."visibility_type_id" > "visibility_types"."id" [delete: restrict]
Ref: "events"."registration_policy_id" > "event_registration_policies"."id" [delete: restrict]
Ref: "events"."audience_gender_id" > "audience_genders"."id" [delete: restrict]
Ref: "events"."audience_age_id" > "audience_ages"."id" [delete: restrict]
Ref: "events"."madhab_id" > "madhabs"."id" [delete: restrict]
Ref: "events"."atproto_record_id" > "atproto_records"."id" [delete: set null]
Ref: "events"."event_series_id" > "event_series"."id" [delete: restrict]

// Event Extensions (1:1)
Ref: "event_islamic_aspects"."id" - "events"."id" [delete: cascade]
Ref: "event_tech_aspects"."id" - "events"."id" [delete: cascade]

// Event Structure
Ref: "event_days"."event_id" > "events"."id" [delete: cascade]
Ref: "event_agenda_items"."event_id" > "events"."id" [delete: cascade]
Ref: "event_agenda_items"."event_day_id" > "event_days"."id" [delete: restrict]
Ref: "event_agenda_items"."kind_id" > "schedule_item_kinds"."id" [delete: restrict]
Ref: "event_sessions"."event_id" > "events"."id" [delete: cascade]
Ref: "event_sessions"."event_day_id" > "event_days"."id" [delete: restrict]
Ref: "event_sessions"."registration_mode_id" > "registration_modes"."id" [delete: restrict]
Ref: "event_session_islamic_aspects"."event_session_id" - "event_sessions"."id" [delete: cascade]
Ref: "event_session_agenda_items"."event_session_id" > "event_sessions"."id" [delete: cascade]
Ref: "event_session_languages"."event_session_id" > "event_sessions"."id" [delete: cascade]
Ref: "event_session_languages"."language_id" > "languages"."id" [delete: restrict]
Ref: "event_session_speakers"."event_session_id" > "event_sessions"."id" [delete: cascade]
Ref: "event_session_speakers"."actor_id" > "actors"."id" [delete: restrict]
Ref: "event_categories"."event_id" > "events"."id" [delete: cascade]
Ref: "event_categories"."category_id" > "categories"."id" [delete: restrict]
Ref: "event_tags"."event_id" > "events"."id" [delete: cascade]
Ref: "event_tags"."tag_id" > "tags"."id" [delete: restrict]
Ref: "event_session_categories"."event_session_id" > "event_sessions"."id" [delete: cascade]
Ref: "event_session_categories"."category_id" > "categories"."id" [delete: restrict]
Ref: "event_session_tags"."event_session_id" > "event_sessions"."id" [delete: cascade]
Ref: "event_session_tags"."tag_id" > "tags"."id" [delete: restrict]

// Registration
Ref: "event_registration_intents"."event_id" > "events"."id" [delete: restrict]
Ref: "event_registration_intents"."user_id" > "users"."id" [delete: restrict]
Ref: "event_registration_intents"."registration_scope_id" > "registration_scopes"."id" [delete: restrict]
Ref: "event_registration_intents"."selected_event_day_id" > "event_days"."id" [delete: restrict]
Ref: "event_registration_intents"."registration_policy_snapshot_id" > "event_registration_policies"."id" [delete: restrict]
Ref: "event_registration_intents"."approval_status_id" > "approval_statuses"."id" [delete: restrict]
Ref: "event_registrations"."event_registration_intent_id" > "event_registration_intents"."id" [delete: cascade]
Ref: "event_registrations"."event_session_id" > "event_sessions"."id" [delete: cascade]
Ref: "event_registrations"."user_id" > "users"."id" [delete: restrict]

// Contact Share
Ref: "event_contact_share_consents"."user_id" > "users"."id" [delete: restrict]
Ref: "event_contact_share_consents"."recipient_actor_id" > "actors"."id" [delete: restrict]
Ref: "event_contact_share_consents"."source_event_id" > "events"."id" [delete: restrict]
Ref: "event_contact_share_consents"."source_event_registration_intent_id" > "event_registration_intents"."id" [delete: restrict]
Ref: "event_contact_share_export"."recipient_actor_id" > "actors"."id" [delete: restrict]
Ref: "event_contact_share_export"."event_id" > "events"."id" [delete: restrict]
Ref: "event_contact_share_export"."exported_by_user_id" > "users"."id" [delete: restrict]
Ref: "event_contact_share_export_item"."export_id" > "event_contact_share_export"."id" [delete: cascade]
Ref: "event_contact_share_export_item"."consent_id" > "event_contact_share_consents"."id" [delete: restrict]

// Custom Properties (EAV)
Ref: "custom_property_definitions"."default_option_id" > "custom_property_options"."id" [delete: cascade]
Ref: "custom_property_options"."custom_property_definition_id" > "custom_property_definitions"."id" [delete: cascade]
Ref: "custom_property_options"."parent_option_id" > "custom_property_options"."id" [delete: cascade]
Ref: "custom_property_values"."custom_property_definition_id" > "custom_property_definitions"."id" [delete: restrict]
Ref: "custom_property_values"."option_id" > "custom_property_options"."id" [delete: cascade]

// Event EAV
Ref: "event_template_custom_property_definitions"."event_template_id" > "event_templates"."id" [delete: cascade]
Ref: "event_template_custom_property_definitions"."default_option_id" > "event_template_custom_property_options"."id" [delete: cascade]
Ref: "event_template_custom_property_options"."event_template_custom_property_definition_id" > "event_template_custom_property_definitions"."id" [delete: cascade]
Ref: "event_template_custom_property_options"."parent_option_id" > "event_template_custom_property_options"."id" [delete: cascade]
Ref: "event_custom_property_definitions"."event_id" > "events"."id" [delete: cascade]
Ref: "event_custom_property_definitions"."source_template_id" > "event_templates"."id" [delete: restrict]
Ref: "event_custom_property_definitions"."default_option_id" > "event_custom_property_options"."id" [delete: cascade]
Ref: "event_custom_property_options"."event_custom_property_definition_id" > "event_custom_property_definitions"."id" [delete: cascade]
Ref: "event_custom_property_options"."parent_option_id" > "event_custom_property_options"."id" [delete: cascade]
Ref: "event_custom_property_values"."event_id" > "events"."id" [delete: cascade]
Ref: "event_custom_property_values"."event_custom_property_definition_id" > "event_custom_property_definitions"."id" [delete: restrict]
Ref: "event_custom_property_values"."option_id" > "event_custom_property_options"."id" [delete: cascade]
Ref: "event_custom_property_projections"."event_id" > "events"."id" [delete: cascade]
Ref: "event_custom_property_projections"."event_custom_property_definition_id" > "event_custom_property_definitions"."id" [delete: cascade]
Ref: "event_custom_property_projections"."event_custom_property_value_id" - "event_custom_property_values"."id" [delete: cascade]

// Event Session EAV
Ref: "event_session_templates"."event_template_id" > "event_templates"."id" [delete: cascade]
Ref: "event_session_template_custom_property_definitions"."event_session_template_id" > "event_session_templates"."id" [delete: cascade]
Ref: "event_session_template_custom_property_definitions"."default_option_id" > "event_session_template_custom_property_options"."id" [delete: cascade]
Ref: "event_session_template_custom_property_options"."event_session_template_custom_property_definition_id" > "event_session_template_custom_property_definitions"."id" [delete: cascade]
Ref: "event_session_template_custom_property_options"."parent_option_id" > "event_session_template_custom_property_options"."id" [delete: cascade]
Ref: "event_session_custom_property_definitions"."event_session_id" > "event_sessions"."id" [delete: cascade]
Ref: "event_session_custom_property_definitions"."default_option_id" > "event_session_custom_property_options"."id" [delete: cascade]
Ref: "event_session_custom_property_options"."event_session_custom_property_definition_id" > "event_session_custom_property_definitions"."id" [delete: cascade]
Ref: "event_session_custom_property_options"."parent_option_id" > "event_session_custom_property_options"."id" [delete: cascade]
Ref: "event_session_custom_property_values"."event_session_id" > "event_sessions"."id" [delete: cascade]
Ref: "event_session_custom_property_values"."event_session_custom_property_definition_id" > "event_session_custom_property_definitions"."id" [delete: restrict]
Ref: "event_session_custom_property_values"."option_id" > "event_session_custom_property_options"."id" [delete: cascade]
Ref: "event_session_custom_property_projections"."event_session_id" > "event_sessions"."id" [delete: cascade]
Ref: "event_session_custom_property_projections"."event_session_custom_property_definition_id" > "event_session_custom_property_definitions"."id" [delete: cascade]
Ref: "event_session_custom_property_projections"."event_session_custom_property_value_id" - "event_session_custom_property_values"."id" [delete: cascade]

// Notifications
Ref: "notifications"."notification_type_id" > "notification_types"."id" [delete: restrict]
Ref: "notifications"."notification_reason_id" > "notification_reasons"."id" [delete: restrict]
Ref: "notifications"."source_entity_type_id" > "notification_entity_types"."id" [delete: restrict]
Ref: "notifications"."user_id" > "users"."id" [delete: restrict]

// API Keys
Ref: "external_api_keys"."external_api_key_status_id" > "external_api_key_statuses"."id" [delete: restrict]
Ref: "external_api_keys"."external_api_key_credit_period_id" > "external_api_key_credit_periods"."id" [delete: restrict]
Ref: "external_api_key_quotas"."external_api_key_id" > "external_api_keys"."id" [delete: cascade]
