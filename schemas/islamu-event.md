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
}

Table "analytics_providers" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)
}

Table "approval_statuses" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)
}

Table "audience_ages" {
  "id" int [pk, not null]
  "master_code" text [not null]
  "full_name" text [not null]
  "description" text
  "min_age" int
  "max_age" int
}

Table "audience_genders" {
  "id" int [pk, not null]
  "master_code" text [not null]
  "full_name" text [not null]
  "description" text
}

Table "category_types" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)
}

Table "did_custody_types" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)
}

Table "event_formats" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)
}

Table "event_statuses" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)
}

Table "event_types" {
  "id" int [pk, not null]
  "full_name" varchar(200) [not null]
  "master_code" varchar(100) [not null]
  "description" varchar(500)
  "tenant_id" uuid
}

Table "file_types" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)
}

Table "group_positions" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)
}

Table "languages" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)
}

Table "madhabs" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)
}

Table "organization_positions" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)
}

Table "owner_types" {
  "id" int [pk, not null]
  "master_code" text [not null]
  "full_name" text [not null]
  "description" text
}

Table "registration_modes" {
  "id" int [pk, not null]
  "master_code" varchar(50) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)
}

Table "tag_types" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)
}

Table "tenant_statuses" {
  "id" int [pk, not null]
  "master_code" varchar(50) [not null]
  "full_name" varchar(100) [not null]
  "description" varchar(500)
  "is_active_state" boolean [not null]
}

Table "visibility_types" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)
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

  Note: 'Check: CK_AppSettings_NoHighValueSecrets — blocks Database:*, Security:MasterKey*, ConnectionStrings:* keys.'
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
    master_code [unique]
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
    master_code [unique]
  }
}

Table "role_permissions" {
  "role_id" int [pk, not null]
  "permission_id" int [pk, not null]
  "granted_at" timestamptz [not null]
  "granted_by" uuid
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
}

Table "sync_states" {
  "id" int [pk, not null]
  "service" varchar(500) [not null]
  "cursor" bigint [not null]
  "last_seq_time" timestamptz
  "updated_at" timestamptz [not null]
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
    (user_id, tenant_id) [unique, name: 'IX_tenant_members_user_id_tenant_id']
  }
}

Table "external_api_keys" {
  "id" uuid [pk, not null]
  "tenant_id" uuid [not null]
  "name" varchar(200) [not null]
  "key_id" varchar(64) [not null]
  "secret_hash" varchar(500) [not null]
  "scopes" varchar(1000) [not null]
  "owner_type" int [not null]
  "owner_id" uuid [not null]
  "status" int [not null]
  "expires_at" timestamptz
  "last_used_at" timestamptz
  "last_used_ip" varchar(64)
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    key_id [unique]
    (tenant_id, owner_type, owner_id)
    (tenant_id, status)
  }
}

// ============================================================
// Taxonomy (Categories, Tags)
// ============================================================

Table "categories" {
  "id" uuid [pk, not null, note: 'uuidv7 app-side']
  "master_code" varchar(500) [not null]
  "full_name" varchar(500) [not null]
  "parent_id" uuid
  "tenant_id" uuid [not null]

  Indexes {
    (tenant_id, master_code) [unique, name: 'ix_categories_tenant_master_code']
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
  "master_code" varchar(500) [not null]
  "full_name" varchar(500) [not null]
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
  "event_type_id" int
  "tenant_id" uuid [not null]
  "name" varchar(100) [not null]
  "display_name" varchar(200) [not null]
  "description" varchar(500)
  "property_type" varchar(50) [not null]
  "is_required" boolean [not null]
  "is_multi" boolean [not null]
  "is_active" boolean [not null]
  "sort_order" int [not null]
  "default_value" varchar(1000)
  "validation_rules" varchar(2000)
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid

  indexes {
    (tenant_id, entity_type_name, is_active) [name: 'ix_cpd_tenant_entity_active']
    (tenant_id, entity_type_name, event_type_id, name) [unique, name: 'ix_cpd_tenant_entity_type_name']
  }
}

Table "custom_property_options" {
  "id" uuid [pk, not null]
  "custom_property_definition_id" uuid [not null]
  "name" varchar(200) [not null]
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

  indexes {
    (custom_property_definition_id, sort_order) [name: 'ix_cpo_definition_sort']
  }
}

Table "custom_property_values" {
  "id" uuid [pk, not null]
  "custom_property_definition_id" uuid [not null]
  "entity_id" uuid [not null]
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

  indexes {
    (custom_property_definition_id, entity_id) [name: 'ix_cpv_definition_entity']
    entity_id [name: 'ix_cpv_entity']
    (tenant_id, custom_property_definition_id) [name: 'ix_cpv_tenant_definition']
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
  "concurrency_stamp" uuid [not null, note: 'optimistic concurrency token, app-managed']

  indexes {
    (tenant_id, is_deleted, approval_status_id) [name: 'ix_organization_tenant_active']
    (tenant_id, actor_id) [name: 'ix_organization_tenant_actor']
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
}

Table "organization_reviews" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "organization_id" uuid [not null]
  "event_id" uuid [not null, note: 'renamed from program_id']
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
  "concurrency_stamp" uuid [not null, note: 'optimistic concurrency token, app-managed']

  indexes {
    (tenant_id, is_deleted, approval_status_id) [name: 'ix_groups_tenant_active']
  }

  Note: 'Community groups. Approval-gated, soft-deletable, concurrency-protected.'
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

  Note: 'File metadata. Actual blobs stored in external object storage.'
}

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

// ============================================================
// Events
// ============================================================

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
  "event_format_id" int [not null]
  "atproto_record_id" uuid
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null, note: 'optimistic concurrency token, app-managed']
  "background_color" varchar(50)
  "background_effect" varchar(50)
  "background_image_id" uuid

  indexes {
    (tenant_id, is_deleted, event_status_id) [name: 'ix_events_tenant_active_status']
    (tenant_id, actor_id, created_at) [name: 'ix_events_tenant_actor_created']
    (tenant_id, first_session_date, last_session_date) [name: 'ix_events_tenant_daterange']
    (tenant_id, event_type_id) [name: 'ix_events_tenant_type']
    (tenant_id, slug) [name: 'ix_events_tenant_slug']
  }

  Note: 'Core aggregate. Check: CK_Event_NonNegativePrice (price IS NULL OR price >= 0). Soft-deletable, tenant-scoped, concurrency-protected.'
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

Table "event_sessions" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "event_id" uuid [not null]
  "start_time" timestamptz [not null]
  "end_time" timestamptz [not null]
  "location_id" uuid
  "title" varchar(500)
  "tenant_id" uuid [not null]
  "slug" varchar(200)
  "max_audience_attendees" int
  "current_audience_attendees" int
  "registration_mode_id" int
  "price" decimal(19,4)
  "currency_code" varchar(3)
  "description" varchar(500)
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null, note: 'optimistic concurrency token, app-managed']

  indexes {
    (event_id) [name: 'ix_event_sessions_event_id']
    (tenant_id) [name: 'ix_event_sessions_tenant_id']
  }

  Note: 'Individual sessions within an event. Check: CK_EventSession_NonNegativePrice. Cascade deletes with parent event.'
}

Table "event_session_islamic_aspects" {
  "event_session_id" uuid [pk, not null, note: 'shared PK with event_sessions']
  "start_time_type" int [not null]
  "reference_prayer" int
  "offset_minutes" int
  "requires_wudu" boolean [not null]
  "ritual_requirements_json" jsonb

  Note: 'Check: CK_EventSessionIslamicAspect_RelativeStartFields — requires prayer fields when start_time_type is relative.'
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
  "id" int [pk, not null]
  "event_session_id" uuid [not null]
  "language_id" int [not null]
  "tenant_id" uuid [not null]

  indexes {
    (event_session_id, language_id) [unique, name: 'IX_event_session_languages_event_session_id_language_id']
  }
}

Table "event_session_speakers" {
  "id" uuid [pk, not null]
  "actor_id" uuid [not null]
  "event_session_id" uuid [not null]
  "tenant_id" uuid [not null]
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
}

Table "event_registrations" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "user_id" uuid [not null]
  "event_session_id" uuid [not null]
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

  Note: 'Session-level RSVP. Soft-deletable, unique per (user, session).'
}

Table "event_contact_share_consents" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "tenant_id" uuid [not null]
  "source_event_id" uuid
  "user_id" uuid [not null]
  "recipient_actor_id" uuid [not null]
  "source_event_registration_id" uuid
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
  "action" varchar(50) [not null, note: 'Created, Updated, Deleted']
  "old_values" jsonb
  "new_values" jsonb
  "affected_columns" jsonb
  "actor_id" uuid
  "timestamp" timestamptz [not null]
  "tenant_id" uuid [not null]

  indexes {
    (entity_type, entity_id) [name: 'IX_audit_logs_entity_type_entity_id']
    tenant_id [name: 'IX_audit_logs_tenant_id']
    timestamp [name: 'IX_audit_logs_timestamp']
  }

  Note: 'Entity-level audit trail. JSONB old/new values for change tracking.'
}

Table "notification_types" {
  "id" int [pk, not null, note: 'ValueGeneratedNever — seeded lookup']
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  Note: 'Lookup: RegistrationConfirmed, ApprovalGranted, ApprovalRejected, WaitlistPromoted, EventCreated, EventUpdated, EventCancelled, MemberInvited, MemberRemoved, General.'
}

Table "notification_entity_types" {
  "id" int [pk, not null, note: 'ValueGeneratedNever — seeded lookup']
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  Note: 'Lookup: Event, Organization, Group, EventRegistration, EventSession, User.'
}

Table "notifications" {
  "id" uuid [pk, not null, note: 'uuidv7 default']
  "user_id" uuid [not null]
  "notification_type_id" int [not null]
  "title" varchar(500) [not null]
  "body" varchar(2000)
  "is_read" boolean [not null]
  "read_at" timestamptz
  "notification_entity_type_id" int [note: 'nullable — links to entity type for deep linking']
  "entity_id" varchar(200) [note: 'nullable — ID of the related entity']
  "notification_scope_id" int [not null]
  "source_actor_id" uuid
  "recipient_context_actor_id" uuid
  "tenant_id" uuid [not null]
  "created_at" timestamptz [not null, default: `NOW()`]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null, default: false]
  "deleted_at" timestamptz
  "deleted_by" uuid

  indexes {
    (tenant_id, user_id, is_read, created_at) [name: 'ix_notifications_tenant_user_unread', note: 'created_at DESC']
    (tenant_id, user_id, created_at) [name: 'ix_notifications_unread_by_user', note: 'partial: is_read=false AND is_deleted=false, created_at DESC']
    (tenant_id, notification_type_id) [name: 'ix_notifications_tenant_type']
    (user_id, notification_scope_id, is_read) [name: 'ix_notifications_user_scope']
  }

  Note: 'User notification inbox. Soft-deletable, tenant-scoped. FKs to notification_types and notification_entity_types for structured categorization.'
}

// ============================================================
// Enums (domain value types stored as integers)
// ============================================================

Enum "pds_sync_operation" {
  "Create" [note: '1']
  "Update" [note: '2']
  "Delete" [note: '3']
}

Enum "pds_sync_status" {
  "Pending" [note: '1']
  "Processing" [note: '2']
  "Completed" [note: '3']
  "Failed" [note: '4']
  "DeadLettered" [note: '5']
}

Enum "actor_type_enum" {
  "User" [note: '1']
  "Organization" [note: '2']
  "Group" [note: '3']
  "Bot" [note: '4']
}

Enum "approval_status_enum" {
  "Pending" [note: '1']
  "Approved" [note: '2']
  "Rejected" [note: '3']
}

Enum "event_status_enum" {
  "Draft" [note: '1']
  "Published" [note: '2']
  "Cancelled" [note: '3']
  "Completed" [note: '4']
}

Enum "event_format_enum" {
  "InPerson" [note: '1']
  "Online" [note: '2']
  "Hybrid" [note: '3']
}

Enum "visibility_type_enum" {
  "Public" [note: '1']
  "Private" [note: '2']
  "Unlisted" [note: '3']
}

Enum "role_scope_enum" {
  "Platform" [note: '1']
  "Tenant" [note: '2']
  "Organization" [note: '3']
  "Group" [note: '4']
}

Enum "did_custody_type_enum" {
  "Custodial" [note: '1 - managed by platform']
  "SelfCustody" [note: '2 - user controls keys']
}

Enum "tenant_status_enum" {
  "Pending" [note: '1']
  "Active" [note: '2']
  "Suspended" [note: '3']
  "Deactivated" [note: '4']
}

Enum "registration_mode_enum" {
  "Open" [note: '1']
  "ApprovalRequired" [note: '2']
  "InviteOnly" [note: '3']
  "Closed" [note: '4']
}

Enum "config_scope_enum" {
  "Instance" [note: '0']
  "Tenant" [note: '1']
  "Organization" [note: '2']
  "Group" [note: '3']
  "User" [note: '4']
}

Enum "skill_level_enum" {
  "AllLevels" [note: '0']
  "Beginner" [note: '1']
  "Intermediate" [note: '2']
  "Advanced" [note: '3']
}

Enum "event_publishing_policy_enum" {
  "Open" [note: '1 - anyone can publish']
  "RequiresApproval" [note: '2 - admin must approve']
}

Enum "notification_type_enum" {
  "RegistrationConfirmed" [note: '1']
  "ApprovalGranted" [note: '2']
  "ApprovalRejected" [note: '3']
  "WaitlistPromoted" [note: '4']
  "EventCreated" [note: '5']
  "EventUpdated" [note: '6']
  "EventCancelled" [note: '7']
  "MemberInvited" [note: '8']
  "MemberRemoved" [note: '9']
  "General" [note: '10']
}

Enum "notification_entity_type_enum" {
  "Event" [note: '1']
  "Organization" [note: '2']
  "Group" [note: '3']
  "EventRegistration" [note: '4']
  "EventSession" [note: '5']
  "User" [note: '6']
}

Enum "external_api_key_owner_type_enum" {
  "User" [note: '1']
  "Organization" [note: '2']
}

Enum "external_api_key_status_enum" {
  "Active" [note: '1']
  "Revoked" [note: '2']
}

Enum "consent_status_enum" {
  "Granted" [note: '1']
  "Withdrawn" [note: '2']
}

Enum "policy_change_operation_enum" {
  "Created" [note: '1']
  "Updated" [note: '2']
  "Deleted" [note: '3']
}

Enum "policy_change_status_enum" {
  "Pending" [note: '1']
  "Processing" [note: '2']
  "Completed" [note: '3']
  "Failed" [note: '4']
}

Enum "translation_management_provider_enum" {
  "None" [note: '0 — Offline bundles only']
  "Tolgee" [note: '1']
  "Weblate" [note: '2']
}

// ============================================================
// Relationships
// ============================================================

// Tenant relationships
Ref: "tenants"."tenant_status_id" > "tenant_statuses"."id" [delete: restrict]
Ref: "tenant_settings"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "tenant_settings"."default_group_id" > "groups"."id" [delete: restrict]
Ref: "tenant_settings"."default_organization_id" > "organizations"."id" [delete: restrict]
Ref: "tenant_setting_overrides"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "tenant_navigation_links"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "tenant_onboarding_states"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "tenant_policy_sets"."tenant_id" > "tenants"."id" [delete: cascade]
Ref: "tenant_lifecycle_logs"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "tenant_lifecycle_logs"."old_status_id" > "tenant_statuses"."id" [delete: restrict]
Ref: "tenant_lifecycle_logs"."new_status_id" > "tenant_statuses"."id" [delete: restrict]
Ref: "tenant_capabilities"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "tenant_capabilities"."module_id" > "module_definitions"."id" [delete: restrict]
Ref: "tenant_invitations"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "tenant_invitations"."role_id" > "roles"."id" [delete: restrict]
Ref: "tenant_members"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "tenant_members"."user_id" > "users"."id" [delete: restrict]
Ref: "tenant_members"."role_id" > "roles"."id" [delete: restrict]
Ref: "external_api_keys"."tenant_id" > "tenants"."id" [delete: restrict]

// Taxonomy relationships
Ref: "categories"."parent_id" > "categories"."id" [delete: restrict]
Ref: "categories"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "category_type_categories"."category_id" > "categories"."id" [delete: restrict]
Ref: "category_type_categories"."category_type_id" > "category_types"."id" [delete: restrict]
Ref: "category_type_categories"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "tags"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "tag_type_tags"."tag_id" > "tags"."id" [delete: restrict]
Ref: "tag_type_tags"."tag_type_id" > "tag_types"."id" [delete: restrict]
Ref: "tag_type_tags"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "custom_property_definitions"."event_type_id" > "event_types"."id" [delete: restrict]
Ref: "custom_property_definitions"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "custom_property_options"."custom_property_definition_id" > "custom_property_definitions"."id" [delete: cascade]
Ref: "custom_property_options"."parent_option_id" > "custom_property_options"."id" [delete: set null]
Ref: "custom_property_values"."custom_property_definition_id" > "custom_property_definitions"."id" [delete: cascade]
Ref: "custom_property_values"."option_id" > "custom_property_options"."id" [delete: set null]
Ref: "custom_property_values"."tenant_id" > "tenants"."id" [delete: restrict]

// Location relationships
Ref: "locations"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "location_pii"."location_id" - "locations"."id" [delete: cascade]

// Actor relationships
Ref: "actors"."actor_type_id" > "actor_types"."id" [delete: restrict]
Ref: "actors"."did_custody_type_id" > "did_custody_types"."id" [delete: restrict]
Ref: "actors"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "actors"."user_id" > "users"."id" [delete: restrict]
Ref: "actors"."organization_id" > "organizations"."id" [delete: restrict]
Ref: "actors"."group_id" > "groups"."id" [delete: restrict]
Ref: "actors"."profile_picture_id" > "storage_objects"."id" [delete: set null]
Ref: "actors"."banner_picture_id" > "storage_objects"."id" [delete: set null]
Ref: "actor_pii"."actor_id" - "actors"."id" [delete: cascade]
Ref: "actor_key_stores"."actor_id" > "actors"."id" [delete: restrict]
Ref: "actor_key_stores"."tenant_id" > "tenants"."id" [delete: restrict]

// User relationships
Ref: "users"."actor_id" > "actors"."id" [delete: restrict]
Ref: "user_pii"."user_id" - "users"."id" [delete: cascade]
Ref: "platform_user_roles"."user_id" > "users"."id" [delete: restrict]
Ref: "platform_user_roles"."role_id" > "roles"."id" [delete: restrict]
Ref: "user_authentication_tokens"."user_id" > "users"."id" [delete: restrict]
Ref: "user_authentication_tokens"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "user_external_logins"."user_id" > "users"."id" [delete: restrict]
Ref: "user_external_logins"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "user_preferences"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "user_preferences"."user_id" > "users"."id" [delete: cascade]

// Organization relationships
Ref: "organizations"."actor_id" > "actors"."id" [delete: restrict]
Ref: "organizations"."approval_status_id" > "approval_statuses"."id" [delete: restrict]
Ref: "organizations"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "organization_pii"."organization_id" - "organizations"."id" [delete: cascade]
Ref: "organization_members"."organization_id" > "organizations"."id" [delete: restrict]
Ref: "organization_members"."user_id" > "users"."id" [delete: restrict]
Ref: "organization_members"."role_id" > "roles"."id" [delete: restrict]
Ref: "organization_members"."organization_position_id" > "organization_positions"."id" [delete: restrict]
Ref: "organization_members"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "organization_reviews"."organization_id" > "organizations"."id" [delete: restrict]
Ref: "organization_reviews"."event_id" > "events"."id" [delete: restrict]
Ref: "organization_reviews"."user_id" > "users"."id" [delete: restrict]
Ref: "organization_reviews"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "organization_setting_overrides"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "organization_setting_overrides"."organization_id" > "organizations"."id" [delete: restrict]
Ref: "organization_policy_sets"."tenant_id" > "tenants"."id" [delete: cascade]
Ref: "organization_policy_sets"."organization_id" > "organizations"."id" [delete: cascade]

// Group relationships
Ref: "groups"."actor_id" > "actors"."id" [delete: restrict]
Ref: "groups"."approval_status_id" > "approval_statuses"."id" [delete: restrict]
Ref: "groups"."profile_picture_id" > "storage_objects"."id" [delete: restrict]
Ref: "groups"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "group_members"."group_id" > "groups"."id" [delete: restrict]
Ref: "group_members"."user_id" > "users"."id" [delete: restrict]
Ref: "group_members"."role_id" > "roles"."id" [delete: restrict]
Ref: "group_members"."group_position_id" > "group_positions"."id" [delete: restrict]
Ref: "group_members"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "group_setting_overrides"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "group_setting_overrides"."group_id" > "groups"."id" [delete: restrict]

// Storage relationships
Ref: "storage_objects"."file_type_id" > "file_types"."id" [delete: restrict]
Ref: "storage_objects"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "storage_objects"."actor_id" > "actors"."id" [delete: restrict]
Ref: "event_series"."actor_id" > "actors"."id" [delete: restrict]
Ref: "event_series"."featured_image_id" > "storage_objects"."id"
Ref: "event_series"."tenant_id" > "tenants"."id" [delete: cascade]
Ref: "event_series"."visibility_type_id" > "visibility_types"."id" [delete: cascade]

// Event relationships
Ref: "events"."event_type_id" > "event_types"."id" [delete: restrict]
Ref: "events"."audience_gender_id" > "audience_genders"."id" [delete: restrict]
Ref: "events"."audience_age_id" > "audience_ages"."id" [delete: restrict]
Ref: "events"."actor_id" > "actors"."id" [delete: restrict]
Ref: "events"."featured_image_id" > "storage_objects"."id" [delete: restrict]
Ref: "events"."background_image_id" > "storage_objects"."id" [delete: set null]
Ref: "events"."madhab_id" > "madhabs"."id" [delete: restrict]
Ref: "events"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "events"."visibility_type_id" > "visibility_types"."id" [delete: restrict]
Ref: "events"."event_status_id" > "event_statuses"."id" [delete: restrict]
Ref: "events"."event_format_id" > "event_formats"."id" [delete: restrict]
Ref: "events"."atproto_record_id" > "atproto_records"."id" [delete: set null]
Ref: "events"."event_series_id" > "event_series"."id"
Ref: "event_types"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "event_islamic_aspects"."id" - "events"."id" [delete: cascade]
Ref: "event_islamic_aspects"."madhab_id" > "madhabs"."id" [delete: set null]
Ref: "event_islamic_aspects"."primary_language_id" > "languages"."id" [delete: set null]
Ref: "event_tech_aspects"."id" - "events"."id" [delete: cascade]
Ref: "event_sessions"."event_id" > "events"."id" [delete: cascade]
Ref: "event_sessions"."location_id" > "locations"."id" [delete: set null]
Ref: "event_sessions"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "event_sessions"."registration_mode_id" > "registration_modes"."id" [delete: restrict]
Ref: "event_session_islamic_aspects"."event_session_id" - "event_sessions"."id" [delete: cascade]
Ref: "event_session_agenda_items"."event_session_id" > "event_sessions"."id" [delete: cascade]
Ref: "event_session_agenda_items"."location_id" > "locations"."id" [delete: set null]
Ref: "event_session_agenda_items"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "event_session_languages"."event_session_id" > "event_sessions"."id" [delete: cascade]
Ref: "event_session_languages"."language_id" > "languages"."id" [delete: restrict]
Ref: "event_session_languages"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "event_session_speakers"."actor_id" > "actors"."id" [delete: cascade]
Ref: "event_session_speakers"."event_session_id" > "event_sessions"."id" [delete: cascade]
Ref: "event_session_speakers"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "event_categories"."event_id" > "events"."id" [delete: cascade]
Ref: "event_categories"."category_id" > "categories"."id" [delete: cascade]
Ref: "event_categories"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "event_tags"."event_id" > "events"."id" [delete: cascade]
Ref: "event_tags"."tag_id" > "tags"."id" [delete: cascade]
Ref: "event_tags"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "event_registrations"."user_id" > "users"."id" [delete: cascade]
Ref: "event_registrations"."event_session_id" > "event_sessions"."id" [delete: cascade]
Ref: "event_registrations"."approval_status_id" > "approval_statuses"."id"
Ref: "event_registrations"."tenant_id" > "tenants"."id" [delete: cascade]
Ref: "event_registrations"."atproto_record_id" > "atproto_records"."id"
Ref: "event_contact_share_consents"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "event_contact_share_consents"."source_event_id" > "events"."id" [delete: restrict]
Ref: "event_contact_share_consents"."user_id" > "users"."id" [delete: restrict]
Ref: "event_contact_share_consents"."recipient_actor_id" > "actors"."id" [delete: restrict]
Ref: "event_contact_share_consents"."source_event_registration_id" > "event_registrations"."id" [delete: set null]
Ref: "event_contact_share_export"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "event_contact_share_export"."recipient_actor_id" > "actors"."id" [delete: restrict]
Ref: "event_contact_share_export"."event_id" > "events"."id" [delete: restrict]
Ref: "event_contact_share_export"."exported_by_user_id" > "users"."id" [delete: restrict]
Ref: "event_contact_share_export_item"."export_id" > "event_contact_share_export"."id" [delete: cascade]
Ref: "event_contact_share_export_item"."consent_id" > "event_contact_share_consents"."id" [delete: restrict]

// RBAC relationships
Ref: "role_permissions"."role_id" > "roles"."id" [delete: restrict]
Ref: "role_permissions"."permission_id" > "permissions"."id" [delete: restrict]

// Audit & Notification relationships
Ref: "audit_logs"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "notifications"."user_id" > "users"."id" [delete: cascade]
Ref: "notifications"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "notifications"."notification_type_id" > "notification_types"."id" [delete: restrict]
Ref: "notifications"."notification_entity_type_id" > "notification_entity_types"."id" [delete: restrict]
Ref: "notifications"."notification_scope_id" > "actor_types"."id" [delete: restrict]
Ref: "notifications"."source_actor_id" > "actors"."id" [delete: set null]
Ref: "notifications"."recipient_context_actor_id" > "actors"."id" [delete: set null]
