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

  indexes {
    master_code [unique, name: 'ix_actor_types_master_code']
  }

  Note: 'Lookup: classifies federated actors. Values: User(1), Organization(2), Bot(3), Group(4), System(5). Seeded.'
}

Table "role_scopes" {
  "id" int [pk, not null]
  "master_code" varchar(50) [not null]
  "full_name" varchar(100) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ix_role_scopes_master_code']
  }

  Note: 'Lookup: RBAC role and permission scopes. Values: Platform(0), Tenant(1), Organization(2), Group(3), Event(4). Seeded.'
}

Table "setting_scopes" {
  "id" int [pk, not null]
  "master_code" varchar(50) [not null]
  "full_name" varchar(100) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ix_setting_scopes_master_code']
  }

  Note: 'Lookup: configuration hierarchy scopes. Values: System(0), Instance(1), Tenant(2), Organization(3), Group(4), User(5). Seeded.'
}

Table "setting_value_types" {
  "id" int [pk, not null]
  "master_code" varchar(50) [not null]
  "full_name" varchar(100) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ix_setting_value_types_master_code']
  }

  Note: 'Lookup: system setting value data types. Values: String(0), Integer(1), Boolean(2), Decimal(3), Json(4), DateTime(5). Seeded.'
}

Table "secret_source_types" {
  "id" int [pk, not null]
  "master_code" varchar(50) [not null]
  "full_name" varchar(100) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ix_secret_source_types_master_code']
  }

  Note: 'Lookup: secret value source types. Values: Infisical(0), InlineEncrypted(1), EnvironmentVariable(2). Seeded.'
}

Table "secret_validation_statuses" {
  "id" int [pk, not null]
  "master_code" varchar(50) [not null]
  "full_name" varchar(100) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ix_secret_validation_statuses_master_code']
  }

  Note: 'Lookup: secret validation lifecycle. Values: NotValidated(0), Success(1), Failure(2). Seeded.'
}

Table "analytics_providers" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ix_analytics_providers_master_code']
  }

  Note: 'Lookup: supported analytics engines. Values: PostHog(1), Plausible(2), GoogleAnalytics(3). Seeded.'
}

Table "support_access_audit_event_types" {
  "id" int [pk, not null]
  "master_code" varchar(50) [not null]
  "full_name" varchar(100) [not null]
  "description" varchar(500)
  "is_lifecycle_event" boolean [not null]

  indexes {
    master_code [unique, name: 'ix_support_access_audit_event_types_master_code']
  }

  Note: 'Lookup: support-access audit event categories. Values: Started(1), Stopped(2), Expired(3), Revoked(4), Denied(5), RequestObserved(6), CommandCommitted(7). Seeded.'
}

Table "support_access_end_reasons" {
  "id" int [pk, not null]
  "master_code" varchar(50) [not null]
  "full_name" varchar(100) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ix_support_access_end_reasons_master_code']
  }

  Note: 'Lookup: support-access terminal reasons. Values: UserStopped(1), Expired(2), ForceStopped(3), RevokedByPolicy(4), Replaced(5). Seeded.'
}

Table "support_access_modes" {
  "id" int [pk, not null]
  "master_code" varchar(50) [not null]
  "full_name" varchar(100) [not null]
  "description" varchar(500)
  "allows_writes" boolean [not null]

  indexes {
    master_code [unique, name: 'ix_support_access_modes_master_code']
  }

  Note: 'Lookup: support-access permission modes. Values: ReadOnly(1), Write(2). Write mode is separately governed by instance settings.'
}

Table "support_access_session_statuses" {
  "id" int [pk, not null]
  "master_code" varchar(50) [not null]
  "full_name" varchar(100) [not null]
  "description" varchar(500)
  "is_terminal" boolean [not null]

  indexes {
    master_code [unique, name: 'ix_support_access_session_statuses_master_code']
  }

  Note: 'Lookup: support-access lifecycle states. Values: PendingApproval(1), Active(2), Stopped(3), Expired(4), Revoked(5). Seeded.'
}

Table "approval_statuses" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ix_approval_statuses_master_code']
  }

  Note: 'Lookup: entity approval lifecycle. Values: Pending(1), Approved(2), Rejected(3), Deferred(4). Seeded.'
}

Table "audience_ages" {
  "id" int [pk, not null]
  "master_code" text [not null]
  "full_name" text [not null]
  "description" text
  "min_age" int
  "max_age" int

  indexes {
    master_code [unique, name: 'ix_audience_ages_master_code']
  }

  Note: 'Lookup: age-group targeting. Values: AllAges(1), Kids(2), Teens(3), Adults(4). Seeded.'
}

Table "audience_genders" {
  "id" int [pk, not null]
  "master_code" text [not null]
  "full_name" text [not null]
  "description" text

  indexes {
    master_code [unique, name: 'ix_audience_genders_master_code']
  }

  Note: 'Lookup: gender-based targeting. Values: Mixed(1), MenOnly(2), WomenOnly(3). Seeded.'
}

Table "category_types" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ix_category_types_master_code']
  }

  Note: 'Lookup: classifies categories for different contexts (Events, Organizations, Users). Seeded.'
}

Table "did_custody_types" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ix_did_custody_types_master_code']
  }

  Note: 'Lookup: ATProto DID key management model. Values: SelfCustody(1), PlatformManaged(2). Seeded.'
}

Table "event_formats" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ix_event_formats_master_code']
  }

  Note: 'Lookup: event delivery mode. Values: InPerson(1), Virtual(2), Hybrid(3). Seeded.'
}

Table "event_statuses" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ix_event_statuses_master_code']
  }

  Note: 'Lookup: event lifecycle state. Values: Draft(1), Published(2), Cancelled(3), Completed(4), Archived(5), Moderated(6). Seeded.'
}

Table "event_session_statuses" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  Note: 'Lookup: event-session lifecycle state. Values: Draft(1), Submitted(2), UnderReview(3), Approved(4), Published(5), Rejected(6), Cancelled(7), Archived(8), Completed(9), Moderated(10). Seeded. Draft/internal/completed/moderated statuses may be unscheduled or hidden and are not public.'
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

  indexes {
    master_code [unique, name: 'ix_file_types_master_code']
  }

  Note: 'Lookup: media/document classifications. Values: Image(1), Video(2), Document(3), Archive(4). Seeded.'
}

Table "group_positions" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ix_group_positions_master_code']
  }

  Note: 'Lookup: formal roles within a group. Values: Lead(1), Admin(2), Moderator(3), Member(4). Seeded.'
}

Table "languages" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ix_languages_master_code']
  }

  Note: 'Lookup: ISO language codes for localization and content metadata. Seeded.'
}

Table "madhabs" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ix_madhabs_master_code']
  }

  Note: 'Lookup: Islamic schools of jurisprudence. Values: Hanafi(1), Maliki(2), Shafii(3), Hanbali(4), Other(5). Seeded.'
}

Table "organization_positions" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ix_organization_positions_master_code']
  }

  Note: 'Lookup: formal roles within an organization. Values: CEO(1), Admin(2), Staff(3), Volunteer(4). Seeded.'
}

Table "owner_types" {
  "id" int [pk, not null]
  "master_code" text [not null]
  "full_name" text [not null]
  "description" text

  indexes {
    master_code [unique, name: 'ix_owner_types_master_code']
  }

  Note: 'Lookup: classifies entities that can own resources. Values: User(1), Organization(2), Group(3), Tenant(4), InstanceAdmin(5). Seeded.'
}

Table "registration_modes" {
  "id" int [pk, not null]
  "master_code" varchar(50) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ix_registration_modes_master_code']
  }

  Note: 'Lookup: registration policy. Values: Open(1), ApprovalRequired(2), InviteOnly(3), Closed(4). Seeded.'
}

Table "tag_types" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ix_tag_types_master_code']
  }

  Note: 'Lookup: classifies tags for different contexts. Seeded.'
}

Table "tenant_statuses" {
  "id" int [pk, not null]
  "master_code" varchar(50) [not null]
  "full_name" varchar(100) [not null]
  "description" varchar(500)
  "is_active_state" boolean [not null]

  indexes {
    master_code [unique, name: 'ix_tenant_statuses_master_code']
  }

  Note: 'Lookup: tenant lifecycle. Values: Active(1), Suspended(2), Provisioning(3), Deactivated(4). Seeded.'
}

Table "visibility_types" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ix_visibility_types_master_code']
  }

  Note: 'Lookup: discovery level. Values: Public(1), Private(2), Unlisted(3), MembersOnly(4). Seeded.'
}

Table "schedule_item_kinds" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ix_schedule_item_kinds_master_code']
  }

  Note: 'Lookup: classifies agenda items. Values: Intro(1), Talk(2), QAndA(3), Break(4), Prayer(5), Outro(6), Logistics(7), Custom(8). Seeded.'
}

Table "event_registration_policies" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ix_event_registration_policies_master_code']
  }

  Note: 'Lookup: defines which registration scopes are allowed for an event. Values: WholeEventOnly(1), WholeDayOnly(2), SessionSelectionOnly(3), WholeEventOrDay(4), WholeEventOrSession(5), Flexible(6). Seeded.'
}

Table "registration_scopes" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ix_registration_scopes_master_code']
  }

  Note: 'Lookup: granularity of a registration intent. Values: Event(1), Day(2), SessionSelection(3). Seeded.'
}

Table "external_api_key_statuses" {
  "id" int [pk, not null]
  "master_code" varchar(50) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)
  "is_usable" boolean [not null]

  indexes {
    (master_code) [unique, name: 'ix_external_api_key_statuses_master_code']
  }

  Note: 'Lookup: external API key state. Values: Active(1), Revoked(2), Expired(3). Seeded.'
}


Table "external_api_key_credit_periods" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ix_external_api_key_credit_periods_master_code']
  }

  Note: 'Lookup: credit reset frequency. Values: None(1), Daily(2), Weekly(3), Monthly(4). Seeded.'
}

Table "external_api_key_owner_types" {
  "id" int [pk, not null]
  "master_code" varchar(50) [not null]
  "full_name" varchar(100) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ix_external_api_key_owner_types_master_code']
  }

  Note: 'Lookup: external API key owner classifiers. Values: User(1), Organization(2), Group(3), Tenant(4), InstanceAdmin(5). Seeded.'
}

Table "notification_types" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ix_notification_types_master_code']
  }

  Note: 'Lookup: notification classification. Seeded.'
}

Table "notification_scope_types" {
  "id" int [pk, not null]
  "master_code" varchar(50) [not null]
  "full_name" varchar(100) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ix_notification_scope_types_master_code']
  }

  Note: 'Lookup: notification scope classifiers. Values: User(1), Organization(2), Group(4), System(5). Seeded.'
}

Table "notification_entity_types" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ix_notification_entity_types_master_code']
  }

  Note: 'Lookup: type of entity a notification relates to. Seeded.'
}

Table "notification_reasons" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ix_notification_reasons_master_code']
  }

  Note: 'Lookup: why a notification was triggered. Seeded.'
}

Table "actor_subscription_statuses" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ux_actor_subscription_statuses_master_code']
  }

  Note: 'Lookup: actor subscription lifecycle. Values: Active(1), Unsubscribed(2), Blocked(3). Seeded.'
}

Table "actor_subscription_notification_levels" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ux_actor_subscription_notification_levels_master_code']
  }

  Note: 'Lookup: per-actor subscription delivery level. Values: None(1), All(2), Personalized(3). Seeded.'
}

Table "event_session_kinds" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)

  indexes {
    master_code [unique, name: 'ix_event_session_kinds_master_code']
  }

  Note: 'Lookup: classifying a program item/session (talk, workshop, panel, activity, etc.). Seeded.'
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
  "setting_value_type_id" int [not null]
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
  "setting_scope_id" int [not null]
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
    (setting_scope_id, scope_id) [name: 'ix_configuration_change_logs_setting_scope_id_scope_id']
  }
}

Table "secret_bindings" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "setting_key" varchar(256) [not null]
  "setting_scope_id" int [not null]
  "scope_id" uuid
  "environment_variable_name" varchar(256)
  "infisical_environment" varchar(64)
  "infisical_key" varchar(256)
  "infisical_path" varchar(512)
  "inline_ciphertext" bytea
  "inline_ciphertext_version" int
  "is_locked" boolean [not null]
  "last_validated_at" timestamptz
  "last_validated_by" uuid
  "last_validation_error" varchar(1000)
  "secret_validation_status_id" int [not null]
  "secret_source_type_id" int [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    setting_key [name: 'ix_secret_bindings_setting_key_instance_unique', note: 'filter: scope_id IS NULL']
    (setting_key, scope_id) [unique, name: 'ix_secret_bindings_setting_key_scope_id_tenant_unique', note: 'filter: scope_id IS NOT NULL']
    secret_source_type_id [name: 'ix_secret_bindings_secret_source_type_id']
    secret_validation_status_id [name: 'ix_secret_bindings_secret_validation_status_id']
    (setting_scope_id, scope_id) [name: 'ix_secret_bindings_setting_scope_id_scope_id']
  }

  Note: 'Maps application settings to external secrets providers (Infisical, env vars, and inline encrypted values).'
}


// ============================================================
// RBAC (Roles & Permissions)
// ============================================================

Table "roles" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)
  "role_scope_id" int [not null]
  "is_system" boolean [not null]

  indexes {
    master_code [unique, name: 'ix_roles_mastercode']
    role_scope_id [name: 'ix_roles_role_scope_id']
    (id, role_scope_id) [unique, name: 'ak_roles_id_role_scope_id']
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
  "role_scope_id" int [not null]
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
    role_scope_id [name: 'ix_permissions_role_scope_id']
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
// Email Dispatch (Basic Dispatch Mode)
// ============================================================

Table "email_dispatch_outbox" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "tenant_id" uuid [not null]
  "publish_event_id" uuid [not null]
  "kind" int [not null]
  "source_type" varchar(100) [not null]
  "source_id" uuid [not null]
  "event_id" uuid
  "registration_intent_id" uuid
  "user_id" uuid
  "recipient_email" varchar(320) [not null]
  "subject" varchar(500) [not null]
  "plain_text_body" text
  "html_body" text
  "reply_to" varchar(320)
  "status" int [not null]
  "attempt_count" int [not null]
  "max_attempts" int [not null, default: 5]
  "next_attempt_at" timestamptz
  "processing_started_at" timestamptz
  "processing_lease_token" uuid
  "sent_at" timestamptz
  "dead_lettered_at" timestamptz
  "parked_at" timestamptz
  "unknown_at" timestamptz
  "last_failure_category" varchar(100)
  "last_error" varchar(2000)
  "last_failure_at" timestamptz
  "provider_message_id" varchar(500)
  "correlation_id" varchar(200)
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid

  indexes {
    event_id [name: 'ix_email_dispatch_outbox_event_id']
    registration_intent_id [name: 'ix_email_dispatch_outbox_registration_intent_id']
    user_id [name: 'ix_email_dispatch_outbox_user_id']
    (tenant_id, publish_event_id) [unique, name: 'ux_email_dispatch_outbox_tenant_publish_event']
    (status, next_attempt_at, created_at) [name: 'ix_email_dispatch_outbox_worker_poll']
    (tenant_id, status, last_failure_at) [name: 'ix_email_dispatch_outbox_tenant_status']
    (tenant_id, source_type, source_id, kind) [unique, name: 'ux_email_dispatch_outbox_tenant_source_kind', note: 'filter: is_deleted = false']
  }

  Note: 'Durable email intent/outbox row for Basic Dispatch Mode retries, dead-lettering, parking, and replay.'
}

Table "email_dispatch_attempts" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "tenant_id" uuid [not null]
  "email_dispatch_outbox_id" uuid [not null]
  "attempt_number" int [not null]
  "transport" varchar(50) [not null]
  "provider" varchar(100)
  "outcome" int [not null]
  "started_at" timestamptz [not null]
  "completed_at" timestamptz
  "failure_category" varchar(100)
  "sanitized_error_message" varchar(2000)
  "provider_message_id" varchar(500)
  "correlation_id" varchar(200)
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    (email_dispatch_outbox_id, attempt_number) [unique, name: 'ux_email_dispatch_attempts_outbox_attempt']
    (tenant_id, started_at) [name: 'ix_email_dispatch_attempts_tenant_started']
  }

  Note: 'Per-attempt immutable delivery ledger for SMTP or future transports.'
}

Table "email_dispatch_receipts" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "tenant_id" uuid [not null]
  "publish_event_id" uuid [not null]
  "email_dispatch_outbox_id" uuid [not null]
  "status" int [not null]
  "consumer_id" varchar(200)
  "first_seen_at" timestamptz [not null]
  "processing_started_at" timestamptz
  "completed_at" timestamptz
  "failed_at" timestamptz
  "failure_code" varchar(100)
  "failure_message" varchar(1000)
  "provider_message_id" varchar(500)
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    (email_dispatch_outbox_id, status) [name: 'ix_email_dispatch_receipts_outbox_status']
    (tenant_id, publish_event_id) [unique, name: 'ux_email_dispatch_receipts_tenant_publish_event']
  }

  Note: 'Idempotency receipt shared by current Basic Mode and future queue-backed dispatch modes.'
}

Table "email_dispatch_tenant_controls" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "tenant_id" uuid [not null]
  "is_paused" boolean [not null]
  "pause_reason" varchar(500)
  "paused_at" timestamptz
  "paused_by" uuid
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    tenant_id [unique, name: 'ux_email_dispatch_tenant_controls_tenant']
    (is_paused, updated_at) [name: 'ix_email_dispatch_tenant_controls_pause_state']
  }

  Note: 'Tenant-scoped operational pause/resume control for email dispatch.'
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
}

// ============================================================
// Support Access
// ============================================================

Table "support_access_sessions" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "actor_user_id" uuid [not null]
  "target_tenant_id" uuid [not null]
  "target_tenant_user_id" uuid
  "status_id" int [not null]
  "mode_id" int [not null]
  "reason_code" varchar(100) [not null]
  "reason_text" varchar(1000) [not null]
  "ticket_reference" varchar(200) [not null]
  "approved_by_user_id" uuid
  "started_at_utc" timestamptz [not null]
  "expires_at_utc" timestamptz [not null]
  "ended_at_utc" timestamptz
  "end_reason_id" int
  "end_reason_text" varchar(200)
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (actor_user_id, status_id, expires_at_utc) [name: 'ix_support_access_sessions_actor_status_expires', note: 'expires_at_utc descending']
    approved_by_user_id [name: 'ix_support_access_sessions_approved_by_user_id']
    end_reason_id [name: 'ix_support_access_sessions_end_reason_id']
    (id, actor_user_id, status_id) [name: 'ix_support_access_sessions_id_actor_status']
    mode_id [name: 'ix_support_access_sessions_mode_id']
    status_id [name: 'ix_support_access_sessions_status_id']
    (target_tenant_id, started_at_utc) [name: 'ix_support_access_sessions_target_tenant_started', note: 'started_at_utc descending']
    target_tenant_user_id [name: 'ix_support_access_sessions_target_tenant_user_id']
    actor_user_id [unique, name: 'ux_support_access_sessions_active_actor', note: 'filter: status_id = 2 AND ended_at_utc IS NULL']
  }

  Note: 'Time-boxed, actor-bound, tenant-bound support-access sessions. Check constraints: expires_at_utc > started_at_utc; ended_at_utc is null or after started_at_utc; end_reason_id presence matches ended_at_utc presence.'
}

Table "support_access_audit_events" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "support_access_session_id" uuid [not null]
  "occurred_at_utc" timestamptz [not null]
  "event_type_id" int [not null]
  "actor_user_id" uuid [not null]
  "target_tenant_id" uuid [not null]
  "target_tenant_user_id" uuid
  "route_name" varchar(200)
  "request_name" varchar(200)
  "resource_kind" varchar(200)
  "resource_id" varchar(200)
  "action" varchar(100)
  "outcome" varchar(100) [not null]
  "http_status_code" int
  "correlation_id" varchar(100)
  "trace_id" varchar(100)
  "sanitized_metadata_json" jsonb

  indexes {
    (actor_user_id, occurred_at_utc) [name: 'ix_support_access_audit_events_actor_occurred', note: 'occurred_at_utc descending']
    event_type_id [name: 'ix_support_access_audit_events_event_type_id']
    (support_access_session_id, occurred_at_utc) [name: 'ix_support_access_audit_events_session_occurred', note: 'occurred_at_utc descending']
    target_tenant_user_id [name: 'ix_support_access_audit_events_target_tenant_user_id']
    (target_tenant_id, occurred_at_utc) [name: 'ix_support_access_audit_events_tenant_occurred', note: 'occurred_at_utc descending']
  }

  Note: 'Append-only support-access audit evidence. Stores bounded request/action metadata only; no raw payloads, cookies, tokens, or provider responses.'
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


Table "tenant_settings_documents" {
  "id" uuid [pk, not null, note: 'uuidv7 app-side']
  "tenant_id" uuid [not null]
  "document_key" varchar(128) [not null]
  "schema_version" int [not null]
  "defaults_version" varchar(64) [not null]
  "payload_json" jsonb [not null]
  "concurrency_stamp" uuid [not null, note: 'optimistic concurrency token, app-managed']
  "created_at" timestamptz [not null, default: `NOW()`]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    document_key [name: 'ix_tenant_settings_documents_document_key']
    (tenant_id, document_key) [unique, name: 'ix_tenant_settings_documents_tenant_id_document_key']
  }

  Note: 'Tenant-owned typed settings documents. Payload is non-secret JSONB; tenant.branding is provisioned for each tenant.'
}

Table "tenant_setting_overrides"{
  "id" uuid [pk, not null, note: 'uuidv7 app-side']
  "tenant_id" uuid [not null]
  "setting_key" varchar(256) [not null]
  "value" text [not null]
  "is_locked" boolean [not null, default: false]
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
  "title" varchar(100) [not null]
  "order" int [not null]
  "is_active" boolean [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

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
  "url" varchar(1000) [not null]
  "open_in_new_tab" boolean [not null]
  "order" int [not null]
  "is_active" boolean [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

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
  "is_system" boolean [not null]
  "is_editable" boolean [not null]
  "seed_version" int [not null]
  "deprecated_at" timestamptz
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid

  indexes {
    theme_key [unique, name: 'ix_ui_theme_presets_theme_key', note: 'filter: tenant_id IS NULL AND is_deleted = false']
    (tenant_id, theme_key) [unique, name: 'ix_ui_theme_presets_tenant_id_theme_key', note: 'filter: tenant_id IS NOT NULL AND is_deleted = false']
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

Table "tenant_users" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "tenant_id" uuid [not null]
  "user_id" uuid [not null]
  "actor_id" uuid
  "status_id" int [not null, default: 1]
  "joined_at" timestamptz
  "suspended_at" timestamptz
  "suspended_by" uuid
  "ban_expires_at" timestamptz
  "removed_at" timestamptz
  "removed_by" uuid
  "moderation_note" varchar(2000)
  "created_at" timestamptz [not null, default: `NOW()`]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid

  indexes {
    actor_id [name: 'ix_tenant_users_actor_id']
    user_id [name: 'ix_tenant_users_user_id']
    (tenant_id, actor_id) [unique, name: 'ix_tenantusers_tenant_actor', note: 'filter: actor_id IS NOT NULL']
    (tenant_id, user_id) [unique, name: 'ix_tenantusers_tenant_user']
    (tenant_id, id) [unique, name: 'ak_tenant_users_tenant_id_id']
  }

  Note: 'Tenant-local membership/moderation state for a global user. Check: ck_tenant_users_status (1..4).'
}

Table "tenant_user_role_grants" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "tenant_id" uuid [not null]
  "tenant_user_id" uuid [not null]
  "role_id" int [not null]
  "role_scope_id" int [not null, default: 1]
  "granted_at" timestamptz [not null, default: `NOW()`]
  "granted_by" uuid
  "revoked_at" timestamptz
  "revoked_by" uuid
  "revocation_reason" varchar(1000)
  "created_at" timestamptz [not null, default: `NOW()`]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    (tenant_id, tenant_user_id, role_id) [unique, name: 'ix_tenant_user_role_grants_active_tenant_user_role', note: 'filter: revoked_at IS NULL']
    (role_id, role_scope_id) [name: 'ix_tenant_user_role_grants_role_id_role_scope_id']
    (tenant_id, role_id) [name: 'ix_tenant_user_role_grants_tenant_role']
  }

  Note: 'Auditable tenant-role grant rooted in TenantUser lifecycle. Check: ck_tenant_user_role_grants_role_scope (role_scope_id = Tenant/1).'
}

Table "tenant_user_profiles" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "tenant_id" uuid [not null]
  "tenant_user_id" uuid [not null]
  "display_name_override" varchar(256)
  "contact_email_override" varchar(320)
  "locale" varchar(35)
  "time_zone" varchar(128)
  "preferences_json" jsonb
  "consent_json" jsonb
  "admin_note" varchar(2000)
  "created_at" timestamptz [not null, default: `NOW()`]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    tenant_user_id [unique, name: 'ix_tenantuserprofiles_tenant_user']
    (tenant_id, contact_email_override) [name: 'ix_tenantuserprofiles_tenant_contact_email', note: 'filter: contact_email_override IS NOT NULL']
  }

  Note: 'Tenant-admin-managed local profile overrides layered on top of the global user record.'
}

Table "external_api_keys" {
  "id" uuid [pk, not null]
  "tenant_id" uuid
  "name" varchar(200) [not null]
  "description" varchar(1000)
  "key_id" varchar(64) [not null]
  "secret_hash" varchar(500) [not null]
  "scopes" varchar(1000) [not null]
  "external_api_key_owner_type_id" int [not null]
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
    external_api_key_owner_type_id [name: 'ix_external_api_keys_external_api_key_owner_type_id']
    (tenant_id, external_api_key_owner_type_id, owner_id) [name: 'ix_external_api_keys_tenant_id_external_api_key_owner_type_id_']
    (tenant_id, external_api_key_status_id) [name: 'ix_external_api_keys_tenant_id_external_api_key_status_id']
  }
}

Table "external_api_key_quotas" {
  "id" uuid [pk, not null]
  "external_api_key_id" uuid [not null]
  "period_start" date [not null]
  "period_end" date [not null]
  "credit_limit" int [not null]
  "credits_used" int [not null]
  "rollover_credits" int [not null]
  "request_count" bigint [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    (external_api_key_id, period_start) [unique]
  }
}

Table "external_bindings" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "provider_key" varchar(128) [not null]
  "external_system" varchar(128) [not null]
  "external_type" varchar(128) [not null]
  "external_id" varchar(512) [not null]
  "internal_type" varchar(128) [not null]
  "internal_id" uuid [not null]
  "scope_tenant_id" uuid
  "external_binding_status_id" int [not null, default: 1]
  "metadata_json" jsonb
  "last_seen_at" timestamptz
  "created_at" timestamptz [not null, default: `NOW()`]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    scope_tenant_id [name: 'ix_external_bindings_scope_tenant_id']
    (provider_key, external_system, external_type, external_id) [unique, name: 'ix_external_bindings_external_global_unique', note: 'filter: scope_tenant_id IS NULL']
    (provider_key, external_system, internal_type, internal_id) [unique, name: 'ix_external_bindings_internal_global_unique', note: 'filter: scope_tenant_id IS NULL']
    (provider_key, external_system, external_type, external_id, scope_tenant_id) [unique, name: 'ix_external_bindings_external_tenant_unique', note: 'filter: scope_tenant_id IS NOT NULL']
    (provider_key, external_system, internal_type, internal_id, scope_tenant_id) [unique, name: 'ix_external_bindings_internal_tenant_unique', note: 'filter: scope_tenant_id IS NOT NULL']
  }

  Note: 'Provider-neutral external identity binding. Checks: ck_external_bindings_status (1..3), ck_external_bindings_text_not_blank, ck_external_bindings_registered_pair_scope.'
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
    (tenant_id, id) [unique, name: 'ak_categories_tenant_id_id']
    (tenant_id, master_code) [unique]
    (tenant_id, parent_id) [name: 'ix_categories_tenant_id_parent_id']
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
    (tenant_id, id) [unique, name: 'ak_tags_tenant_id_id']
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

  Note: 'Shared custom-property definitions. Check: ck_custom_property_definitions_shared_entity_type allows only registry-approved shared targets.'
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
    (tenant_id, id) [unique, name: 'ak_locations_tenant_id_id']
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
    (tenant_id, id) [unique, name: 'ak_location_rooms_tenant_id_id']
    (tenant_id, location_id, id) [unique, name: 'ak_location_rooms_tenant_id_location_id_id']
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
  "background_image_id" uuid
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
    (actor_type_id) [name: 'ix_actors_actor_type_id']
    (background_image_id) [name: 'ix_actors_background_image_id']
    (banner_picture_id) [name: 'ix_actors_banner_picture_id']
    (did_custody_type_id) [name: 'ix_actors_did_custody_type_id']
    (user_id) [unique, name: 'ix_actors_user_id', note: 'filtered: user_id IS NOT NULL']
    (organization_id) [unique, name: 'ix_actors_organization_id', note: 'filtered: organization_id IS NOT NULL']
    (group_id) [unique, name: 'ix_actors_group_id', note: 'filtered: group_id IS NOT NULL']
    (profile_picture_id) [name: 'ix_actors_profile_picture_id']
    (tenant_id, id) [unique, name: 'ak_actors_tenant_id_id']
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

Table "actor_subscriptions" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "tenant_id" uuid [not null]
  "subscriber_tenant_user_id" uuid [not null]
  "subscriber_user_id" uuid [not null]
  "target_actor_id" uuid [not null]
  "target_actor_type_id" int [not null]
  "status_id" int [not null, default: 1]
  "notification_level_id" int [not null, default: 2]
  "subscribed_at" timestamptz [not null, default: `NOW()`]
  "unsubscribed_at" timestamptz
  "created_at" timestamptz [not null, default: `NOW()`]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (tenant_id, subscriber_tenant_user_id, target_actor_id) [unique, name: 'ux_actor_subscriptions_active_row', note: 'filter: is_deleted = false']
    (tenant_id, target_actor_id, status_id, notification_level_id) [name: 'ix_actor_subscriptions_fanout_scan']
    (tenant_id, subscriber_user_id) [name: 'ix_actor_subscriptions_subscriber_user']
  }

  Note: 'Tenant-local subscription from active tenant user to organization/group actor. Checks: target_actor_type_id IN (2,4); status_id IN (1,2,3); notification_level_id IN (1,2,3); unsubscribed status requires unsubscribed_at.'
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
  "category" varchar(128) [not null]
  "is_enabled" boolean [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    (tenant_id, user_id, category) [unique, name: 'ix_user_notification_preferences_tenant_id_user_id_category']
    (user_id) [name: 'ix_user_notification_preferences_user_id']
  }
}


Table "user_appearance_profiles" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "user_id" uuid [not null]
  "tenant_id" uuid
  "name" varchar(200) [not null]
  "is_default" boolean [not null]
  "is_archived" boolean [not null]
  "is_user_editable" boolean [not null]
  "theme_mode" varchar(10) [not null]
  "source_preset_id" uuid
  "source_preset_key" varchar(128)
  "source_preset_seed_version" int
  "cloned_at" timestamptz
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    (user_id, source_preset_id) [name: 'ix_user_appearance_profiles_user_id_source_preset_id']
    (user_id, tenant_id, is_archived) [name: 'ix_user_appearance_profiles_user_id_tenant_id_is_archived']
    (user_id, tenant_id, is_default) [unique, name: 'ix_user_appearance_profiles_user_id_tenant_id_is_default', note: 'filter: is_default = true']
    (user_id, tenant_id, name) [name: 'ix_user_appearance_profiles_user_id_tenant_id_name']
  }
}


Table "user_appearance_preferences" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "user_id" uuid [not null]
  "tenant_id" uuid
  "active_profile_id" uuid
  "direction" varchar(10) [not null]
  "language" varchar(10) [not null]
  "theme_mode" varchar(10) [not null]

  indexes {
    (user_id, tenant_id) [unique, name: 'ix_user_appearance_preferences_user_id_tenant_id']
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
  "parent_organization_id" uuid
  "parent_group_id" uuid
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    actor_id [name: 'ix_groups_actor_id']
    approval_status_id [name: 'ix_groups_approval_status_id']
    profile_picture_id [name: 'ix_groups_profile_picture_id']
    (tenant_id, full_name) [name: 'ix_groups_tenant_name']
    (tenant_id, parent_group_id) [name: 'ix_groups_tenant_parent_group']
    (tenant_id, parent_organization_id) [name: 'ix_groups_tenant_parent_organization']
    (tenant_id, is_deleted, approval_status_id) [name: 'ix_groups_tenant_active_status']
  }

  Note: 'Community groups. Approval-gated, soft-deletable, concurrency-protected. Checks: ck_groups_no_self_parent, ck_groups_parent_exclusive.'
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
  "object_key" varchar(1024)
  "provider" varchar(50) [not null, note: 'local | s3_compatible | legacy_external']
  "full_name" varchar(500) [not null]
  "safe_display_name" varchar(500) [not null]
  "extension" varchar(50) [not null]
  "content_type" varchar(255)
  "sha256_checksum" varchar(64)
  "size" bigint [not null]
  "visibility" varchar(50) [not null, note: 'public_image | authenticated_tenant | private_owner']
  "purpose" varchar(100) [not null, note: 'legacy_image | profile_image | event_image | attachment | document | system_asset']
  "lifecycle_state" varchar(50) [not null, note: 'pending | active | quarantined | delete_requested | deleted']
  "owning_resource_kind" varchar(100)
  "owning_resource_id" uuid
  "quarantined_at" timestamptz
  "quarantined_by" uuid
  "quarantine_reason" varchar(500)
  "tenant_id" uuid [not null]
  "actor_id" uuid
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" bool [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    actor_id
    (tenant_id, provider, lifecycle_state)
    (tenant_id, visibility, purpose)
    (tenant_id, owning_resource_kind, owning_resource_id) [note: 'Filtered when owning resource fields are present.']
    (provider, object_key) [unique, note: 'Filtered when object_key is present.']
  }

  Note: 'Tenant-scoped file metadata. Provider keys are internal; public access resolves through application metadata and visibility. Storage reconciliation uses lifecycle/quarantine/delete metadata to report drift, quarantine unsafe rows, and soft-delete physically removed objects.'
}

Table "storage_upload_sessions" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "tenant_id" uuid [not null]
  "user_id" uuid
  "provider" varchar(50) [not null]
  "expected_size_bytes" bigint [not null]
  "reserved_bytes" bigint [not null]
  "content_type" varchar(255) [not null]
  "original_file_name" varchar(500)
  "safe_display_name" varchar(500) [not null]
  "extension" varchar(50)
  "purpose" varchar(100) [not null]
  "visibility" varchar(50) [not null]
  "status" varchar(50) [not null, note: 'reserved | uploading | finalized | canceled | failed | expired']
  "object_key" varchar(1024)
  "sha256_checksum" varchar(64)
  "storage_object_id" uuid
  "idempotency_key" varchar(128)
  "failure_code" varchar(100)
  "failure_message" varchar(500)
  "expires_at" timestamptz [not null]
  "upload_started_at" timestamptz
  "finalized_at" timestamptz
  "canceled_at" timestamptz
  "failed_at" timestamptz
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (tenant_id, status, expires_at)
    (tenant_id, idempotency_key) [unique, note: 'Filtered when idempotency_key is present.']
    (provider, object_key) [note: 'Filtered when object_key is present.']
    storage_object_id
    user_id
  }

  Note: 'Tenant/user scoped upload reservation before bytes are accepted.'
}

Table "storage_usage_counters" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "tenant_id" uuid [not null]
  "provider" varchar(50) [not null]
  "used_bytes" bigint [not null]
  "reserved_bytes" bigint [not null]
  "quarantined_bytes" bigint [not null]
  "object_count" bigint [not null]
  "last_recalculated_at" timestamptz
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (tenant_id, provider) [unique]
  }

  Note: 'Tenant/provider storage usage and reservation aggregate for quota checks.'
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

// ============================================================
// AI Assistant
// ============================================================

Table "ai_conversations" {
  "id" uuid [pk, not null, default: `uuidv7()`]
  "tenant_id" uuid [not null]
  "user_id" uuid [not null]
  "actor_id" uuid
  "status" int [not null]
  "title" varchar(200)
  "provider" varchar(100)
  "model_id" varchar(200)
  "blocked_reason" varchar(200)
  "last_message_sequence" bigint [not null]
  "created_at" timestamp [not null]
  "created_by" uuid
  "updated_at" timestamp
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamp
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    actor_id [name: 'ix_ai_conversations_actor_id']
    (tenant_id, actor_id, updated_at) [name: 'ix_ai_conversations_tenant_actor_updated_at']
    (tenant_id, user_id, status, updated_at) [name: 'ix_ai_conversations_tenant_user_status_updated_at']
    user_id [name: 'ix_ai_conversations_user_id']
  }

  Note: 'Tenant-scoped private AI assistant conversation aggregate. Status values: Active(1), Running(2), Blocked(3), Archived(4). last_message_sequence is the long cursor for ordered messages.'
}

Table "ai_messages" {
  "id" uuid [pk, not null, default: `uuidv7()`]
  "tenant_id" uuid [not null]
  "conversation_id" uuid [not null]
  "sequence" bigint [not null]
  "role" int [not null]
  "content" varchar(16000) [not null]
  "created_at" timestamp [not null]
  "created_by" uuid

  indexes {
    conversation_id [name: 'ix_ai_messages_conversation_id']
    (tenant_id, conversation_id, created_at) [name: 'ix_ai_messages_tenant_conversation_created_at']
    (tenant_id, conversation_id, sequence) [unique, name: 'ux_ai_messages_tenant_conversation_sequence']
  }

  Note: 'Ordered private conversation messages. Role values: System(1), User(2), Assistant(3), Tool(4). sequence must be positive and is unique per tenant/conversation.'
}

Table "ai_runs" {
  "id" uuid [pk, not null, default: `uuidv7()`]
  "tenant_id" uuid [not null]
  "conversation_id" uuid [not null]
  "status" int [not null]
  "provider" varchar(100) [not null]
  "model_id" varchar(200) [not null]
  "queued_at" timestamp [not null]
  "started_at" timestamp
  "completed_at" timestamp
  "failure_code" varchar(100)
  "failure_message" varchar(1000)

  indexes {
    conversation_id [name: 'ix_ai_runs_conversation_id']
    (tenant_id, conversation_id, queued_at) [name: 'ix_ai_runs_tenant_conversation_queued_at']
    (tenant_id, status, queued_at) [name: 'ix_ai_runs_tenant_status_queued_at']
  }

  Note: 'AI provider run audit row. Status values: Queued(1), InProgress(2), Succeeded(3), Failed(4), Cancelled(5). Failure metadata is bounded and safe for private history.'
}

Table "ai_conversation_references" {
  "id" uuid [pk, not null, default: `uuidv7()`]
  "tenant_id" uuid [not null]
  "conversation_id" uuid [not null]
  "kind" int [not null]
  "reference_id" uuid [not null]
  "display_name" varchar(500) [not null]
  "summary" varchar(2000)
  "created_at" timestamp [not null]
  "created_by" uuid

  indexes {
    conversation_id [name: 'ix_ai_conversation_references_conversation_id']
    (tenant_id, conversation_id, kind, reference_id) [unique, name: 'ux_ai_conversation_references_identity']
  }

  Note: 'Server-selected references attached to an AI conversation. Kind values: Event(1), EventSession(2), Actor(3), Organization(4).'
}

Table "ai_proposed_actions" {
  "id" uuid [pk, not null, default: `uuidv7()`]
  "tenant_id" uuid [not null]
  "conversation_id" uuid [not null]
  "message_id" uuid
  "kind" int [not null]
  "status" int [not null]
  "payload_json" jsonb [not null]
  "confirmed_by" uuid
  "confirmed_at" timestamp
  "rejected_by" uuid
  "rejected_at" timestamp
  "result_resource_id" uuid
  "failure_code" varchar(100)
  "failure_message" varchar(1000)
  "created_at" timestamp [not null]
  "created_by" uuid

  indexes {
    conversation_id [name: 'ix_ai_proposed_actions_conversation_id']
    message_id [name: 'ix_ai_proposed_actions_message_id']
    (tenant_id, conversation_id, status, created_at) [name: 'ix_ai_proposed_actions_tenant_conversation_status_created_at']
    (tenant_id, status, kind, created_at) [name: 'ix_ai_proposed_actions_tenant_status_kind_created_at']
  }

  Note: 'Typed provider proposal requiring explicit user confirmation before side effects. Kind values currently allow CreateEventDraft(1). Status values: Proposed(1), Confirmed(2), Rejected(3), Executed(4), Failed(5). payload_json must be a JSON object.'
}

Table "ai_tool_executions" {
  "id" uuid [pk, not null, default: `uuidv7()`]
  "tenant_id" uuid [not null]
  "proposed_action_id" uuid [not null]
  "tool_name" varchar(200) [not null]
  "started_at" timestamp [not null]
  "completed_at" timestamp
  "succeeded" boolean [not null]
  "failure_code" varchar(100)
  "failure_message" varchar(1000)

  indexes {
    proposed_action_id [name: 'ix_ai_tool_executions_proposed_action_id']
    (tenant_id, proposed_action_id, started_at) [name: 'ix_ai_tool_executions_tenant_action_started_at']
    (tenant_id, tool_name, started_at) [name: 'ix_ai_tool_executions_tenant_tool_started_at']
  }

  Note: 'Audit rows for future confirmed AI tool execution attempts. No automatic tool execution is enabled by the MVP send flow.'
}

Ref: "ai_conversations"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "ai_conversations"."user_id" > "users"."id" [delete: restrict]
Ref: "ai_conversations"."actor_id" > "actors"."id" [delete: set null]
Ref: "ai_messages"."conversation_id" > "ai_conversations"."id" [delete: cascade]
Ref: "ai_runs"."conversation_id" > "ai_conversations"."id" [delete: cascade]
Ref: "ai_conversation_references"."conversation_id" > "ai_conversations"."id" [delete: cascade]
Ref: "ai_proposed_actions"."conversation_id" > "ai_conversations"."id" [delete: cascade]
Ref: "ai_proposed_actions"."message_id" > "ai_messages"."id" [delete: set null]
Ref: "ai_tool_executions"."proposed_action_id" > "ai_proposed_actions"."id" [delete: cascade]

Table "events" {
  "id" uuid [pk, not null, note: 'uuidv7 app-side']
  "event_type_id" int
  "title" varchar(200) [not null]
  "subtitle" varchar(200)
  "description" varchar(150)
  "content" varchar(5000)
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
  "event_time_zone_id" varchar(100)
  "event_series_id" uuid
  "series_order" int
  "registration_policy_id" int [note: 'FK to event_registration_policies. Null = Flexible.']
  "event_format_id" int [not null]
  "atproto_record_id" uuid
  "instantiated_from_template_at" timestamptz
  "last_synced_from_template_at" timestamptz
  "source_template_id" uuid
  "source_template_key" text
  "source_template_version" int
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
    (tenant_id, id) [unique, name: 'ak_events_tenant_id_id']
    (tenant_id, is_deleted, event_status_id) [name: 'ix_events_tenant_active_status']
    (tenant_id, actor_id, created_at) [name: 'ix_events_tenant_actor_created']
    (tenant_id, first_session_date, last_session_date) [name: 'ix_events_tenant_daterange']
    (tenant_id, event_type_id) [name: 'ix_events_tenant_eventtype']
    (tenant_id, slug) [name: 'ix_events_tenant_slug']
    (tenant_id, visibility_type_id) [name: 'ix_events_tenant_visibility']
    (atproto_record_id) [name: 'ix_events_atproto_record_id']
    (audience_age_id) [name: 'ix_events_audience_age_id']
    (audience_gender_id) [name: 'ix_events_audience_gender_id']
    (background_image_id) [name: 'ix_events_background_image_id']
    (featured_image_id) [name: 'ix_events_featured_image_id']
    (event_format_id) [name: 'ix_events_event_format_id']
    (event_series_id) [name: 'ix_events_event_series_id']
    (event_status_id) [name: 'ix_events_event_status_id']
    (event_type_id) [name: 'ix_events_event_type_id']
    (madhab_id) [name: 'ix_events_madhab_id']
    (registration_policy_id) [name: 'ix_events_registration_policy_id']
    (visibility_type_id) [name: 'ix_events_visibility_type_id']
  }

  Note: 'Core aggregate. Checks: CK_Event_NonNegativePrice (price >= 0), CK_Event_SessionDateRange, CK_Event_SessionStartUtcRange, CK_Event_TimeZoneIdNotBlank. Soft-deletable, tenant-scoped, concurrency-protected. Event graph child rows use (tenant_id, id) as the principal key. UTC session starts are source of truth; local dates are server-owned projections.'
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
    (tenant_id, id) [unique, name: 'ak_event_days_tenant_id_id']
    (tenant_id, event_id, id) [unique, name: 'ak_event_days_tenant_id_event_id_id']
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
    (tenant_id, event_id, event_day_id) [name: 'ix_event_agenda_items_tenant_id_event_id_event_day_id']
    (tenant_id, location_id) [name: 'ix_event_agenda_items_tenant_id_location_id']
    (tenant_id, location_id, room_id) [name: 'ix_event_agenda_items_tenant_id_location_id_room_id']
    (tenant_id, event_id, local_start_date, local_start_minute_of_day) [name: 'ix_event_agenda_items_tenant_event_local_start']
    (tenant_id, event_id, sort_order) [name: 'ix_event_agenda_items_tenant_event_sort']
  }

  Note: 'Non-session schedule entries (breaks, prayers). Checks: CK_EventAgendaItem_EndAfterStart, CK_EventAgendaItem_RoomRequiresLocation, CK_EventAgendaItem_LocalStartMinuteRange, CK_EventAgendaItem_LocalEndMinuteRange, CK_EventAgendaItem_LocalStartMinuteMatchesTime, CK_EventAgendaItem_LocalEndMinuteMatchesTime. UTC times are source of truth; local fields are server-owned projections.'
}

Table "event_sessions" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "event_id" uuid [not null]
  "event_day_id" uuid
  "event_session_kind_id" int
  "event_session_status_id" int [not null, default: 1, note: 'FK to event_session_statuses. Default/backfill = Draft.']
  "start_time" timestamptz
  "end_time" timestamptz
  "end_time_type" int [not null, default: 0, note: 'Values: Fixed(0), OpenEnded(1), RelativeToPrayer(2)']
  "location_id" uuid
  "room_id" uuid
  "title" varchar(500)
  "featured_image_id" uuid
  "tenant_id" uuid [not null]
  "slug" varchar(200)
  "max_audience_attendees" int
  "current_audience_attendees" int
  "registration_mode_id" int
  "price" decimal(19,4)
  "currency_code" varchar(3)
  "description" varchar(500)
  "sort_order" int [not null, default: 0]
  "local_start_date" date
  "local_end_date" date
  "local_start_time" time
  "local_end_time" time
  "local_start_minute_of_day" int
  "local_end_minute_of_day" int
  "instantiated_from_template_at" timestamptz
  "last_synced_from_template_at" timestamptz
  "source_template_id" uuid
  "source_template_key" text
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
    (tenant_id, id) [unique, name: 'ak_event_sessions_tenant_id_id']
    (tenant_id, event_id, id) [unique, name: 'ak_event_sessions_tenant_id_event_id_id']
    (tenant_id, event_id, event_day_id) [name: 'ix_event_sessions_tenant_id_event_id_event_day_id']
    (tenant_id, location_id) [name: 'ix_event_sessions_tenant_id_location_id']
    (tenant_id, event_id, local_start_date, local_start_minute_of_day) [name: 'ix_event_sessions_tenant_event_local_start']
    (tenant_id, location_id, room_id, start_time, end_time) [name: 'ix_event_sessions_tenant_location_room_time']
    (tenant_id, event_day_id, sort_order) [name: 'ix_event_sessions_tenant_day_sort']
    (event_session_kind_id) [name: 'ix_event_sessions_event_session_kind_id']
    (event_session_status_id) [name: 'ix_event_sessions_event_session_status_id']
    (registration_mode_id) [name: 'ix_event_sessions_registration_mode_id']
    (featured_image_id) [name: 'ix_event_sessions_featured_image_id']
  }

  Note: 'Draft-capable program item. Checks: CK_EventSession_NonNegativePrice, CK_EventSession_EndAfterStart (conditional when scheduled), CK_EventSession_RoomRequiresLocation, CK_EventSession_LocalStartMinuteRange, CK_EventSession_LocalEndMinuteRange, CK_EventSession_LocalStartMinuteMatchesTime, CK_EventSession_LocalEndMinuteMatchesTime. Nullable start/end/local projection fields represent unscheduled draft/internal sessions. Model-owned exclusion: EX_EventSession_RoomNoOverlap prevents overlapping active scheduled sessions in the same tenant/location/room using tstzrange(start_time, end_time, ''[)'') and ignores rows where start_time or end_time is null. UTC times are source of truth when present; local fields are server-owned projections.'
}

Table "event_session_groups" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "event_id" uuid [not null]
  "name" varchar(200) [not null]
  "slug" varchar(200)
  "description" varchar(1000)
  "location_id" uuid
  "room_id" uuid
  "color" varchar(50)
  "sort_order" int [not null, default: 0]
  "is_published" boolean [not null]
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
    (tenant_id, id) [unique, name: 'ak_event_session_groups_tenant_id_id']
    (tenant_id, event_id, id) [unique, name: 'ak_event_session_groups_tenant_id_event_id_id']
    (tenant_id, location_id) [name: 'ix_event_session_groups_tenant_id_location_id']
    (tenant_id, location_id, room_id) [name: 'ix_event_session_groups_tenant_id_location_id_room_id']
    (tenant_id, event_id, sort_order) [name: 'ix_event_session_groups_tenant_event_sort']
  }

  Note: 'Tenant-scoped program section/track/devroom grouping for sessions inside an event. Check: CK_EventSessionGroup_RoomRequiresLocation.'
}

Table "event_session_group_sessions" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "event_session_group_id" uuid [not null]
  "event_session_id" uuid [not null]
  "event_id" uuid [not null]
  "is_primary" boolean [not null]
  "sort_order" int [not null, default: 0]
  "tenant_id" uuid [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid

  indexes {
    (tenant_id, event_id, event_session_group_id, event_session_id) [unique, name: 'ix_event_session_group_sessions_tenant_event_group_session', note: 'filter: is_deleted = false']
    (tenant_id, event_id, event_session_id, is_primary) [unique, name: 'ix_event_session_group_sessions_tenant_event_session_primary', note: 'filter: is_primary = true AND is_deleted = false']
    (tenant_id, event_session_group_id, sort_order) [name: 'ix_event_session_group_sessions_tenant_group_sort']
  }

  Note: 'Explicit join assigning EventSession program items to tracks/devrooms/sections.'
}

Table "event_session_islamic_aspects" {
  "event_session_id" uuid [pk, not null, note: 'shared PK with event_sessions']
  "start_time_type" int [not null]
  "reference_prayer" int
  "offset_minutes" int
  "end_reference_prayer" int
  "end_offset_minutes" int
  "requires_wudu" boolean [not null, default: false]
  "ritual_requirements_json" jsonb

  Note: 'Checks: CK_EventSessionIslamicAspect_StartTimeState, CK_EventSessionIslamicAspect_OffsetRange, CK_EventSessionIslamicAspect_ReferencePrayerRange, CK_EventSessionIslamicAspect_EndOffsetRange, CK_EventSessionIslamicAspect_EndReferencePrayerRange, CK_EventSessionIslamicAspect_EndTimeState. Fixed start requires null prayer fields; prayer-relative start requires reference_prayer and offset_minutes (-180..180). Fixed end requires null end prayer fields; prayer-relative end requires end_reference_prayer and end_offset_minutes (-180..180).'
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

  indexes {
    (tenant_id, event_session_id) [name: 'ix_event_session_agenda_items_tenant_id_event_session_id']
    (tenant_id, location_id) [name: 'ix_event_session_agenda_items_tenant_id_location_id']
  }
}

Table "event_session_languages" {
  "id" int [pk, not null, note: 'auto-increment']
  "event_session_id" uuid [not null]
  "language_id" int [not null]
  "tenant_id" uuid [not null]

  indexes {
    (tenant_id, event_session_id, language_id) [unique, name: 'ix_eventsessionlanguages_session_language']
  }
}

Table "event_session_speakers" {
  "id" uuid [pk, not null]
  "actor_id" uuid [not null]
  "event_session_id" uuid [not null]
  "tenant_id" uuid [not null]

  indexes {
    (tenant_id, event_session_id, actor_id) [unique, name: 'ix_event_session_speakers_tenant_session_actor']
    (tenant_id, actor_id) [name: 'ix_event_session_speakers_tenant_id_actor_id']
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
    (tenant_id, category_id) [name: 'ix_event_categories_tenant_id_category_id']
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
    (tenant_id, tag_id) [name: 'ix_event_tags_tenant_id_tag_id']
  }
}

Table "event_registrations" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "event_id" uuid [not null, note: 'denormalized from EventSession for same-event composite FK enforcement']
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
    (tenant_id, event_id, event_session_id, user_id) [unique, name: 'ix_eventregistrations_session_user', note: 'filter: is_deleted = false']
    user_id [name: 'ix_eventregistrations_user']
    (tenant_id, event_id, event_registration_intent_id) [name: 'ix_eventregistrations_intent']
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
    (tenant_id, id) [unique, name: 'ak_event_registration_intents_tenant_id_id']
    (tenant_id, event_id, id) [unique, name: 'ak_event_registration_intents_tenant_id_event_id_id']
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
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    (tenant_id, category_id) [name: 'ix_event_session_categories_tenant_id_category_id']
    (tenant_id, event_session_id, category_id) [unique, name: 'ix_event_session_categories_tenant_session_category']
  }
}


Table "event_session_tags" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "event_session_id" uuid [not null]
  "tag_id" uuid [not null]
  "tenant_id" uuid [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    (tenant_id, tag_id) [name: 'ix_event_session_tags_tenant_id_tag_id']
    (tenant_id, event_session_id, tag_id) [unique, name: 'ix_event_session_tags_tenant_session_tag']
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
    (tenant_id, source_event_id) [name: 'ix_event_contact_share_consents_tenant_id_source_event_id']
    (tenant_id, source_event_registration_intent_id) [name: 'ix_event_contact_share_consents_tenant_id_source_event_registration_intent_id']
  }
}

Table "event_contact_share_exports" {
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
    (tenant_id, event_id) [name: 'ix_event_contact_share_exports_tenant_id_event_id']
    (exported_by_user_id) [name: 'ix_event_contact_share_exports_exported_by_user_id']
  }
}


Table "event_contact_share_export_items" {
  "export_id" uuid [pk, not null]
  "consent_id" uuid [pk, not null]
  "email_snapshot" varchar(320) [not null]

  indexes {
    (consent_id) [name: 'ix_event_contact_share_export_items_consent_id']
  }
}


// ============================================================
// Event Reporting & Moderation Review
// ============================================================

Table "event_reports" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "tenant_id" uuid [not null]
  "event_id" uuid [not null]
  "reporter_user_id" uuid
  "reporter_actor_id" uuid
  "reporter_kind" int [not null]
  "source_kind" int [not null]
  "reason_code" varchar(100) [not null]
  "subcategory_code" varchar(100)
  "status" int [not null]
  "priority" int [not null]
  "severity_hint" int
  "duplicate_group_id" uuid
  "reporter_contact_consent" boolean [not null]
  "reporter_locale" varchar(10)
  "reporter_ip_hash" varchar(64)
  "reporter_user_agent_hash" varchar(64)
  "closed_at" timestamptz
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null, default: false]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (tenant_id, id) [unique, name: 'ak_event_reports_tenant_id_id']
    (tenant_id, event_id, id) [unique, name: 'ak_event_reports_tenant_id_event_id_id']
    reporter_user_id [name: 'ix_event_reports_reporter_user_id']
    (tenant_id, reporter_actor_id) [name: 'ix_event_reports_tenant_id_reporter_actor_id']
    (tenant_id, duplicate_group_id) [name: 'ix_event_reports_tenant_duplicate_group', note: 'filter: duplicate_group_id IS NOT NULL']
    (tenant_id, event_id, status, created_at) [name: 'ix_event_reports_tenant_event_status_created', note: 'descending: created_at']
    (tenant_id, priority, status, created_at) [name: 'ix_event_reports_tenant_priority_status_created', note: 'descending: created_at']
    (tenant_id, reporter_user_id, event_id, reason_code, created_at) [name: 'ix_event_reports_tenant_reporter_event_reason_created', note: 'filter: reporter_user_id IS NOT NULL; descending: created_at']
  }

  Note: 'Tenant-scoped event-report aggregate. Checks enforce non-blank reason/subcategory/fingerprint values when present, enum ranges for reporter/source/status/priority/severity, and closed_at only for terminal statuses.'
}

Table "event_report_cases" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "tenant_id" uuid [not null]
  "report_id" uuid [not null]
  "queue_code" varchar(50) [not null]
  "status" int [not null]
  "priority" int [not null]
  "assigned_moderator_user_id" uuid
  "sla_due_at" timestamptz
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (tenant_id, id) [unique, name: 'ak_event_report_cases_tenant_id_id']
    (tenant_id, report_id, id) [unique, name: 'ak_event_report_cases_tenant_id_report_id_id']
    assigned_moderator_user_id [name: 'ix_event_report_cases_assigned_moderator_user_id']
    (tenant_id, assigned_moderator_user_id, status, updated_at) [name: 'ix_event_report_cases_tenant_assignee_status_updated', note: 'filter: assigned_moderator_user_id IS NOT NULL; descending: updated_at']
    (tenant_id, queue_code, status, priority, created_at) [name: 'ix_event_report_cases_tenant_queue_status_priority_created', note: 'descending: created_at']
    (tenant_id, sla_due_at) [name: 'ix_event_report_cases_tenant_sla_due_at', note: 'filter: sla_due_at IS NOT NULL']
  }

  Note: 'Local moderation queue case for an event report. Checks enforce non-blank queue code and status/priority ranges; concurrency_stamp is the optimistic workflow guard.'
}

Table "event_report_evidence" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "tenant_id" uuid [not null]
  "report_id" uuid [not null]
  "evidence_kind" int [not null]
  "text_body_encrypted" text
  "storage_object_id" uuid
  "content_hash" varchar(128)
  "classification" int [not null]
  "retention_until" timestamptz
  "created_by_user_id" uuid
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    created_by_user_id [name: 'ix_event_report_evidence_created_by_user_id']
    (tenant_id, storage_object_id) [name: 'ix_event_report_evidence_tenant_id_storage_object_id']
    (tenant_id, content_hash) [name: 'ix_event_report_evidence_tenant_content_hash', note: 'filter: content_hash IS NOT NULL']
    (tenant_id, report_id, evidence_kind, created_at) [name: 'ix_event_report_evidence_tenant_report_kind_created', note: 'descending: created_at']
    (tenant_id, retention_until) [name: 'ix_event_report_evidence_tenant_retention_until', note: 'filter: retention_until IS NOT NULL']
  }

  Note: 'Sensitive report evidence. Reporter-text evidence stores encrypted text only; checks enforce evidence/classification ranges, required encrypted reporter text for text evidence, and non-blank content_hash when present.'
}

Table "event_report_targets" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "tenant_id" uuid [not null]
  "report_id" uuid [not null]
  "target_kind" int [not null]
  "target_id" uuid [not null]
  "field_path" varchar(200)
  "storage_object_id" uuid

  indexes {
    (tenant_id, storage_object_id) [name: 'ix_event_report_targets_tenant_id_storage_object_id']
    (tenant_id, report_id, target_kind, target_id) [name: 'ix_event_report_targets_tenant_report_target']
    (tenant_id, target_kind, target_id) [name: 'ix_event_report_targets_tenant_target']
  }

  Note: 'Report target references for event/session/field/storage-object targets. Checks enforce target_kind range and non-blank field_path when present.'
}

Table "event_report_signals" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "tenant_id" uuid [not null]
  "report_id" uuid
  "event_id" uuid [not null]
  "provider" int [not null]
  "signal_type" varchar(100) [not null]
  "policy_code" varchar(100) [not null]
  "score" numeric(5,4)
  "verdict" int [not null]
  "recommended_action" int
  "safe_summary" varchar(500)
  "external_signal_id" varchar(200)
  "correlation_id" varchar(100) [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    (tenant_id, event_id, report_id) [name: 'ix_event_report_signals_tenant_id_event_id_report_id']
    (tenant_id, event_id, provider, created_at) [name: 'ix_event_report_signals_tenant_event_provider_created', note: 'descending: created_at']
    (tenant_id, report_id, provider, created_at) [name: 'ix_event_report_signals_tenant_report_provider_created', note: 'filter: report_id IS NOT NULL; descending: created_at']
    (tenant_id, provider, correlation_id) [unique, name: 'ux_event_report_signals_tenant_provider_correlation']
    (tenant_id, provider, external_signal_id) [unique, name: 'ux_event_report_signals_tenant_provider_external_signal', note: 'filter: external_signal_id IS NOT NULL']
  }

  Note: 'Bounded moderation provider signal metadata. Checks enforce provider/verdict/recommended_action ranges, score 0..1, non-blank signal/policy/correlation IDs, and no raw provider payload storage.'
}

Table "event_report_decisions" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "tenant_id" uuid [not null]
  "case_id" uuid [not null]
  "report_id" uuid [not null]
  "decision_source" int [not null]
  "decision_kind" int [not null]
  "reason_code" varchar(100) [not null]
  "safe_note" varchar(1000)
  "moderator_user_id" uuid
  "external_decision_id" varchar(200)
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    (tenant_id, report_id, id) [unique, name: 'ak_event_report_decisions_tenant_id_report_id_id']
    moderator_user_id [name: 'ix_event_report_decisions_moderator_user_id']
    (tenant_id, report_id, case_id) [name: 'ix_event_report_decisions_tenant_id_report_id_case_id']
    (tenant_id, case_id, created_at) [name: 'ix_event_report_decisions_tenant_case_created', note: 'descending: created_at']
    (tenant_id, report_id, created_at) [name: 'ix_event_report_decisions_tenant_report_created', note: 'descending: created_at']
    (tenant_id, decision_source, external_decision_id) [unique, name: 'ux_event_report_decisions_tenant_source_external', note: 'filter: external_decision_id IS NOT NULL']
  }

  Note: 'Review decision before enforcement. Checks enforce source/kind ranges, non-blank reason/safe-note/external IDs when present, and local moderator identity for local decisions.'
}

Table "event_report_external_links" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "tenant_id" uuid [not null]
  "report_id" uuid [not null]
  "case_id" uuid
  "provider" int [not null]
  "provider_case_id" varchar(200)
  "provider_signal_id" varchar(200)
  "provider_url" varchar(500)
  "sync_state" int [not null]
  "last_synced_at" timestamptz
  "last_error_category" varchar(100)
  "retry_count" int [not null]
  "correlation_id" varchar(100) [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    (tenant_id, report_id, case_id) [name: 'ix_event_report_external_links_tenant_id_report_id_case_id']
    (tenant_id, provider, sync_state, created_at) [name: 'ix_event_report_external_links_tenant_provider_state_created', note: 'descending: created_at']
    (tenant_id, provider, provider_case_id) [unique, name: 'ux_event_report_external_links_tenant_provider_case', note: 'filter: provider_case_id IS NOT NULL']
    (tenant_id, provider, correlation_id) [unique, name: 'ux_event_report_external_links_tenant_provider_correlation']
    (tenant_id, provider, provider_signal_id) [unique, name: 'ux_event_report_external_links_tenant_provider_signal', note: 'filter: provider_signal_id IS NOT NULL']
  }

  Note: 'External provider sync marker for report/case/signal mirroring. Checks enforce provider/sync_state ranges, retry_count >= 0, non-blank external IDs/URLs/error categories/correlation IDs when present.'
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
  "notification_reason_id" int
  "notification_entity_type_id" int
  "source_actor_id" uuid
  "recipient_context_actor_id" uuid
  "notification_scope_id" int [not null]
  "entity_id" varchar(200)
  "title" varchar(500) [not null]
  "body" varchar(2000)
  "deduplication_key" varchar(500) [not null]
  "snoozed_until" timestamptz
  "is_read" boolean [not null]
  "read_at" timestamptz
  "is_archived" boolean [not null]
  "archived_at" timestamptz
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid

  indexes {
    notification_entity_type_id [name: 'ix_notifications_notification_entity_type_id']
    notification_reason_id [name: 'ix_notifications_notification_reason_id']
    notification_scope_id [name: 'ix_notifications_notification_scope_id']
    notification_type_id [name: 'ix_notifications_notification_type_id']
    recipient_context_actor_id [name: 'ix_notifications_recipient_context_actor_id']
    source_actor_id [name: 'ix_notifications_source_actor_id']
    (tenant_id, user_id, is_read, created_at) [name: 'ix_notifications_tenant_user_unread', note: 'descending: created_at']
    (tenant_id, user_id, created_at) [name: 'ix_notifications_unread_by_user', note: 'descending: created_at', unique]
    (tenant_id, notification_type_id) [name: 'ix_notifications_tenant_type']
    (user_id, notification_scope_id, is_read) [name: 'ix_notifications_user_scope']
    (user_id, is_archived, created_at) [name: 'ix_notifications_user_archived', note: 'descending: created_at']
    (tenant_id, user_id, deduplication_key) [unique, name: 'ux_notifications_tenant_user_deduplication_key']
  }

  Note: 'User notification inbox row. Check: ck_notifications_entity_reference_shape keeps polymorphic entity references null/null or Guid-shaped. Deduplication key is required for retry-safe fanout-created notifications.'
}

Table "notification_fanout_runs" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "tenant_id" uuid [not null]
  "fanout_kind" varchar(100) [not null]
  "notification_entity_type_id" int [not null]
  "entity_id" uuid [not null]
  "source_actor_id" uuid [not null]
  "status" varchar(50) [not null, default: 'pending']
  "cursor_subscriber_tenant_user_id" uuid
  "processed_count" int [not null]
  "created_notification_count" int [not null]
  "started_at" timestamptz
  "completed_at" timestamptz
  "failed_at" timestamptz
  "last_error" varchar(2000)
  "created_at" timestamptz [not null, default: `NOW()`]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "concurrency_stamp" uuid [not null]

  indexes {
    (tenant_id, fanout_kind, notification_entity_type_id, entity_id, source_actor_id) [unique, name: 'ux_notification_fanout_runs_source']
    (status, created_at) [name: 'ix_notification_fanout_runs_worker_poll']
  }

  Note: 'Idempotency guard and progress cursor for asynchronous notification fanout. Checks: processed_count >= 0; created_notification_count >= 0; status IN (pending, processing, completed, failed).'
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
  "key" varchar(128) [not null]
  "tenant_id" uuid [not null]
  "user_id" varchar(256)
  "request_method" varchar(16) [not null]
  "request_target" varchar(512) [not null]
  "request_content_type" varchar(256)
  "request_body_hash" varchar(64) [not null]
  "principal_fingerprint" varchar(64) [not null]
  "expires_at" timestamptz [not null]
  "status_code" int
  "content_type" varchar(256)
  "response_body" text
  "created_at" timestamptz [not null]

  indexes {
    (key, tenant_id) [unique, name: 'IX_IdempotencyRecords_Key_TenantId']
    expires_at [name: 'IX_IdempotencyRecords_ExpiresAt']
  }

  Note: 'Ephemeral write-retry replay cache. Request fingerprint columns reject same-key reuse for a different write request or principal fingerprint.'
}


// ============================================================
// Views
// ============================================================

Table "custom_property_projection_dirty_scope" {
  "id" bigint [pk, not null, note: 'bigint identity']
  "projection_name" varchar(100) [not null]
  "projection_version" int [not null]
  "tenant_id" uuid [not null]
  "scope_type" varchar(50) [not null]
  "scope_id" uuid
  "definition_id" uuid
  "reason" varchar(200) [not null]
  "created_at" timestamptz [not null]
  "drained_at" timestamptz

  indexes {
    tenant_id [name: 'ix_custom_property_projection_dirty_scope_tenant_id']
    (projection_name, projection_version, tenant_id) [name: 'ix_dirty_scope_pending', note: 'filter: drained_at IS NULL']
    (projection_name, projection_version, tenant_id, scope_type, scope_id, definition_id) [unique, name: 'ix_dirty_scope_unique']
  }
}

Table "custom_property_projection_status" {
  "projection_name" varchar(100) [not null]
  "projection_version" int [not null]
  "tenant_id" uuid [not null]
  "state" varchar(50) [not null]
  "last_rebuild_started_at" timestamptz
  "last_rebuild_completed_at" timestamptz
  "rows_processed" bigint [not null]
  "rows_failed" bigint [not null]
  "last_checkpoint" varchar(200)
  "last_error_message" varchar(2000)
  "concurrency_stamp" uuid [not null]

  indexes {
    tenant_id [name: 'ix_custom_property_projection_status_tenant_id']
  }

  Note: 'PK is (projection_name, projection_version, tenant_id).'
}

Table "event_role_assignments" {
  "id" uuid [pk, not null, note: 'uuidv7()']
  "tenant_id" uuid [not null]
  "event_id" uuid [not null]
  "user_id" uuid [not null]
  "role_id" int [not null]
  "status" int [not null]
  "starts_at_utc" timestamptz [not null]
  "expires_at_utc" timestamptz
  "revoked_at_utc" timestamptz
  "revoked_by_user_id" uuid
  "version" bigint [not null, note: 'concurrency token']
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid

  indexes {
    event_id [name: 'ix_event_role_assignments_event_id']
    role_id [name: 'ix_event_role_assignments_role_id']
    user_id [name: 'ix_event_role_assignments_user_id']
    (tenant_id, event_id, role_id, status) [name: 'ix_event_role_assignments_tenant_event_role_status']
    (tenant_id, event_id, user_id, role_id) [unique, name: 'ix_event_role_assignments_unique_pending_active', note: 'filter: status IN (1, 2)']
    (tenant_id, event_id, user_id, status) [name: 'ix_event_role_assignments_tenant_event_user_status']
    (tenant_id, user_id, event_id, status) [name: 'ix_event_role_assignments_tenant_user_event_status']
  }

  Note: 'Check: ck_event_role_assignments_validity_window (expires_at_utc IS NULL OR expires_at_utc > starts_at_utc).'
}


// ============================================================
// Relationships (Foreign Keys)
// ============================================================

// Tenants & Setup
Ref: "tenants"."tenant_status_id" > "tenant_statuses"."id" [delete: restrict]
Ref: "tenant_settings_documents"."tenant_id" - "tenants"."id" [delete: cascade]
Ref: "tenant_setting_overrides"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "tenant_navigation_links"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "tenant_footer_link_groups"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "tenant_footer_links"."footer_link_group_id" > "tenant_footer_link_groups"."id" [delete: cascade]
Ref: "tenant_onboarding_states"."tenant_id" - "tenants"."id" [delete: cascade]
Ref: "tenant_policy_sets"."tenant_id" - "tenants"."id" [delete: cascade]
Ref: "tenant_capabilities"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "tenant_capabilities"."module_id" > "module_definitions"."id" [delete: restrict]
Ref: "tenant_invitations"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "tenant_users"."tenant_id" > "tenants"."id" [delete: cascade]
Ref: "tenant_users"."user_id" > "users"."id" [delete: cascade]
Ref: "tenant_users"."actor_id" > "actors"."id" [delete: set null]
Ref: "tenant_user_role_grants"."tenant_id" > "tenants"."id" [delete: cascade]
Ref: "tenant_user_role_grants".("tenant_id", "tenant_user_id") > "tenant_users".("tenant_id", "id") [delete: cascade]
Ref: "tenant_user_role_grants".("role_id", "role_scope_id") > "roles".("id", "role_scope_id") [delete: restrict]
Ref: "tenant_user_profiles"."tenant_id" > "tenants"."id" [delete: cascade]
Ref: "tenant_user_profiles"."tenant_user_id" - "tenant_users"."id" [delete: cascade]

// Users & Roles
Ref: "user_pii"."user_id" - "users"."id" [delete: cascade]
Ref: "platform_user_roles"."user_id" > "users"."id" [delete: restrict]
Ref: "platform_user_roles"."role_id" > "roles"."id" [delete: restrict]
Ref: "roles"."role_scope_id" > "role_scopes"."id" [delete: restrict]
Ref: "permissions"."role_scope_id" > "role_scopes"."id" [delete: restrict]
Ref: "role_permissions"."role_id" > "roles"."id" [delete: cascade]
Ref: "role_permissions"."permission_id" > "permissions"."id" [delete: cascade]
Ref: "user_authentication_tokens"."user_id" > "users"."id" [delete: restrict]
Ref: "user_external_logins"."user_id" > "users"."id" [delete: restrict]
Ref: "user_preferences"."user_id" > "users"."id" [delete: restrict]
Ref: "user_notification_preferences"."user_id" > "users"."id" [delete: restrict]
Ref: "user_appearance_profiles"."user_id" > "users"."id" [delete: restrict]
Ref: "user_appearance_preferences"."user_id" > "users"."id" [delete: restrict]
Ref: "user_appearance_preferences"."active_profile_id" > "user_appearance_profiles"."id" [delete: restrict]
Ref: "external_bindings"."scope_tenant_id" > "tenants"."id" [delete: restrict]

// Support Access
Ref: "support_access_sessions"."actor_user_id" > "users"."id" [delete: restrict]
Ref: "support_access_sessions"."approved_by_user_id" > "users"."id" [delete: restrict]
Ref: "support_access_sessions"."target_tenant_id" > "tenants"."id" [delete: restrict]
Ref: "support_access_sessions"."target_tenant_user_id" > "tenant_users"."id" [delete: restrict]
Ref: "support_access_sessions"."status_id" > "support_access_session_statuses"."id" [delete: restrict]
Ref: "support_access_sessions"."mode_id" > "support_access_modes"."id" [delete: restrict]
Ref: "support_access_sessions"."end_reason_id" > "support_access_end_reasons"."id" [delete: restrict]
Ref: "support_access_audit_events"."support_access_session_id" > "support_access_sessions"."id" [delete: restrict]
Ref: "support_access_audit_events"."event_type_id" > "support_access_audit_event_types"."id" [delete: restrict]
Ref: "support_access_audit_events"."actor_user_id" > "users"."id" [delete: restrict]
Ref: "support_access_audit_events"."target_tenant_id" > "tenants"."id" [delete: restrict]
Ref: "support_access_audit_events"."target_tenant_user_id" > "tenant_users"."id" [delete: restrict]

// Actors & Identity
Ref: "actors"."actor_type_id" > "actor_types"."id" [delete: restrict]
Ref: "actors"."user_id" - "users"."id" [delete: restrict]
Ref: "actors"."organization_id" - "organizations"."id" [delete: restrict]
Ref: "actors"."group_id" - "groups"."id" [delete: restrict]
Ref: "actor_pii"."actor_id" - "actors"."id" [delete: cascade]
Ref: "actor_key_stores"."actor_id" > "actors"."id" [delete: cascade]
Ref: "actor_subscriptions"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "actor_subscriptions".("tenant_id", "subscriber_tenant_user_id") > "tenant_users".("tenant_id", "id") [delete: restrict]
Ref: "actor_subscriptions"."subscriber_user_id" > "users"."id" [delete: restrict]
Ref: "actor_subscriptions".("tenant_id", "target_actor_id") > "actors".("tenant_id", "id") [delete: restrict]
Ref: "actor_subscriptions"."target_actor_type_id" > "actor_types"."id" [delete: restrict]
Ref: "actor_subscriptions"."status_id" > "actor_subscription_statuses"."id" [delete: restrict]
Ref: "actor_subscriptions"."notification_level_id" > "actor_subscription_notification_levels"."id" [delete: restrict]

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
Ref: "groups"."profile_picture_id" > "storage_objects"."id" [delete: set null]
Ref: "groups"."parent_group_id" > "groups"."id" [delete: restrict]
Ref: "groups"."parent_organization_id" > "organizations"."id" [delete: restrict]
Ref: "group_members"."group_id" > "groups"."id" [delete: restrict]
Ref: "group_members"."user_id" > "users"."id" [delete: restrict]
Ref: "group_members"."group_position_id" > "group_positions"."id" [delete: restrict]
Ref: "group_setting_overrides"."group_id" > "groups"."id" [delete: restrict]

// Taxonomy
// Tenant-scoped event-graph FKs below intentionally include tenant_id in the physical database model.
// This prevents a tenant-owned row from linking to a parent aggregate owned by another tenant.
Ref: "categories".("tenant_id", "parent_id") > "categories".("tenant_id", "id") [delete: restrict]
Ref: "category_type_categories"."category_id" > "categories"."id" [delete: restrict]
Ref: "category_type_categories"."category_type_id" > "category_types"."id" [delete: restrict]
Ref: "tag_type_tags"."tag_id" > "tags"."id" [delete: restrict]
Ref: "tag_type_tags"."tag_type_id" > "tag_types"."id" [delete: restrict]

// Storage
Ref: "storage_objects"."file_type_id" > "file_types"."id" [delete: restrict]
Ref: "storage_objects"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "storage_objects"."actor_id" > "actors"."id" [delete: set null]
Ref: "storage_upload_sessions"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "storage_upload_sessions"."user_id" > "users"."id" [delete: restrict]
Ref: "storage_upload_sessions"."storage_object_id" > "storage_objects"."id" [delete: restrict]
Ref: "storage_usage_counters"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "location_rooms".("tenant_id", "location_id") > "locations".("tenant_id", "id") [delete: cascade]

// Events Core
Ref: "events"."event_type_id" > "event_types"."id" [delete: restrict]
Ref: "events".("tenant_id", "actor_id") > "actors".("tenant_id", "id") [delete: restrict]
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
Ref: "event_days".("tenant_id", "event_id") > "events".("tenant_id", "id") [delete: cascade]
Ref: "event_agenda_items".("tenant_id", "event_id") > "events".("tenant_id", "id") [delete: cascade]
Ref: "event_agenda_items".("tenant_id", "event_id", "event_day_id") > "event_days".("tenant_id", "event_id", "id") [delete: restrict]
Ref: "event_agenda_items".("tenant_id", "location_id") > "locations".("tenant_id", "id") [delete: restrict]
Ref: "event_agenda_items".("tenant_id", "location_id", "room_id") > "location_rooms".("tenant_id", "location_id", "id") [delete: restrict]
Ref: "event_agenda_items"."kind_id" > "schedule_item_kinds"."id" [delete: restrict]
Ref: "event_sessions".("tenant_id", "event_id") > "events".("tenant_id", "id") [delete: cascade]
Ref: "event_sessions".("tenant_id", "event_id", "event_day_id") > "event_days".("tenant_id", "event_id", "id") [delete: restrict]
Ref: "event_sessions".("tenant_id", "location_id") > "locations".("tenant_id", "id") [delete: restrict]
Ref: "event_sessions".("tenant_id", "location_id", "room_id") > "location_rooms".("tenant_id", "location_id", "id") [delete: restrict]
Ref: "event_sessions"."event_session_status_id" > "event_session_statuses"."id" [delete: restrict]
Ref: "event_sessions"."registration_mode_id" > "registration_modes"."id" [delete: restrict]
Ref: "event_session_groups".("tenant_id", "event_id") > "events".("tenant_id", "id") [delete: cascade]
Ref: "event_session_groups".("tenant_id", "location_id") > "locations".("tenant_id", "id") [delete: restrict]
Ref: "event_session_groups".("tenant_id", "location_id", "room_id") > "location_rooms".("tenant_id", "location_id", "id") [delete: restrict]
Ref: "event_session_group_sessions".("tenant_id", "event_id") > "events".("tenant_id", "id") [delete: cascade]
Ref: "event_session_group_sessions".("tenant_id", "event_id", "event_session_group_id") > "event_session_groups".("tenant_id", "event_id", "id") [delete: cascade]
Ref: "event_session_group_sessions".("tenant_id", "event_id", "event_session_id") > "event_sessions".("tenant_id", "event_id", "id") [delete: cascade]
Ref: "event_session_islamic_aspects"."event_session_id" - "event_sessions"."id" [delete: cascade]
Ref: "event_session_agenda_items".("tenant_id", "event_session_id") > "event_sessions".("tenant_id", "id") [delete: cascade]
Ref: "event_session_agenda_items".("tenant_id", "location_id") > "locations".("tenant_id", "id") [delete: restrict]
Ref: "event_session_languages".("tenant_id", "event_session_id") > "event_sessions".("tenant_id", "id") [delete: cascade]
Ref: "event_session_languages"."language_id" > "languages"."id" [delete: restrict]
Ref: "event_session_speakers".("tenant_id", "event_session_id") > "event_sessions".("tenant_id", "id") [delete: cascade]
Ref: "event_session_speakers".("tenant_id", "actor_id") > "actors".("tenant_id", "id") [delete: cascade]
Ref: "event_categories".("tenant_id", "event_id") > "events".("tenant_id", "id") [delete: cascade]
Ref: "event_categories".("tenant_id", "category_id") > "categories".("tenant_id", "id") [delete: cascade]
Ref: "event_tags".("tenant_id", "event_id") > "events".("tenant_id", "id") [delete: cascade]
Ref: "event_tags".("tenant_id", "tag_id") > "tags".("tenant_id", "id") [delete: cascade]
Ref: "event_session_categories".("tenant_id", "event_session_id") > "event_sessions".("tenant_id", "id") [delete: cascade]
Ref: "event_session_categories".("tenant_id", "category_id") > "categories".("tenant_id", "id") [delete: cascade]
Ref: "event_session_tags".("tenant_id", "event_session_id") > "event_sessions".("tenant_id", "id") [delete: cascade]
Ref: "event_session_tags".("tenant_id", "tag_id") > "tags".("tenant_id", "id") [delete: cascade]

// Registration
Ref: "event_registration_intents".("tenant_id", "event_id") > "events".("tenant_id", "id") [delete: cascade]
Ref: "event_registration_intents"."user_id" > "users"."id" [delete: restrict]
Ref: "event_registration_intents"."registration_scope_id" > "registration_scopes"."id" [delete: restrict]
Ref: "event_registration_intents".("tenant_id", "event_id", "selected_event_day_id") > "event_days".("tenant_id", "event_id", "id") [delete: restrict]
Ref: "event_registration_intents"."registration_policy_snapshot_id" > "event_registration_policies"."id" [delete: restrict]
Ref: "event_registration_intents"."approval_status_id" > "approval_statuses"."id" [delete: restrict]
Ref: "event_registrations".("tenant_id", "event_id") > "events".("tenant_id", "id") [delete: cascade]
Ref: "event_registrations".("tenant_id", "event_id", "event_registration_intent_id") > "event_registration_intents".("tenant_id", "event_id", "id") [delete: cascade]
Ref: "event_registrations".("tenant_id", "event_id", "event_session_id") > "event_sessions".("tenant_id", "event_id", "id") [delete: cascade]
Ref: "event_registrations"."user_id" > "users"."id" [delete: restrict]

// Email Dispatch
Ref: "email_dispatch_outbox"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "email_dispatch_outbox"."event_id" > "events"."id" [delete: restrict]
Ref: "email_dispatch_outbox"."registration_intent_id" > "event_registration_intents"."id" [delete: restrict]
Ref: "email_dispatch_outbox"."user_id" > "users"."id" [delete: restrict]
Ref: "email_dispatch_attempts"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "email_dispatch_attempts"."email_dispatch_outbox_id" > "email_dispatch_outbox"."id" [delete: cascade]
Ref: "email_dispatch_receipts"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "email_dispatch_receipts"."email_dispatch_outbox_id" > "email_dispatch_outbox"."id" [delete: cascade]
Ref: "email_dispatch_tenant_controls"."tenant_id" > "tenants"."id" [delete: restrict]

// Contact Share
Ref: "event_contact_share_consents"."user_id" > "users"."id" [delete: restrict]
Ref: "event_contact_share_consents".("tenant_id", "recipient_actor_id") > "actors".("tenant_id", "id") [delete: restrict]
Ref: "event_contact_share_consents".("tenant_id", "source_event_id") > "events".("tenant_id", "id") [delete: restrict]
Ref: "event_contact_share_consents".("tenant_id", "source_event_registration_intent_id") > "event_registration_intents".("tenant_id", "id") [delete: restrict]
Ref: "event_contact_share_exports".("tenant_id", "recipient_actor_id") > "actors".("tenant_id", "id") [delete: restrict]
Ref: "event_contact_share_exports".("tenant_id", "event_id") > "events".("tenant_id", "id") [delete: restrict]
Ref: "event_contact_share_exports"."exported_by_user_id" > "users"."id" [delete: restrict]
Ref: "event_contact_share_export_items"."export_id" > "event_contact_share_exports"."id" [delete: cascade]
Ref: "event_contact_share_export_items"."consent_id" > "event_contact_share_consents"."id" [delete: restrict]

// Event Reporting & Moderation Review
Ref: "event_reports"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "event_reports".("tenant_id", "event_id") > "events".("tenant_id", "id") [delete: restrict]
Ref: "event_reports"."reporter_user_id" > "users"."id" [delete: restrict]
Ref: "event_reports".("tenant_id", "reporter_actor_id") > "actors".("tenant_id", "id") [delete: restrict]
Ref: "event_report_cases"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "event_report_cases".("tenant_id", "report_id") > "event_reports".("tenant_id", "id") [delete: restrict]
Ref: "event_report_cases"."assigned_moderator_user_id" > "users"."id" [delete: restrict]
Ref: "event_report_evidence"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "event_report_evidence".("tenant_id", "report_id") > "event_reports".("tenant_id", "id") [delete: restrict]
Ref: "event_report_evidence".("tenant_id", "storage_object_id") > "storage_objects".("tenant_id", "id") [delete: restrict]
Ref: "event_report_evidence"."created_by_user_id" > "users"."id" [delete: restrict]
Ref: "event_report_targets"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "event_report_targets".("tenant_id", "report_id") > "event_reports".("tenant_id", "id") [delete: restrict]
Ref: "event_report_targets".("tenant_id", "storage_object_id") > "storage_objects".("tenant_id", "id") [delete: restrict]
Ref: "event_report_signals"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "event_report_signals".("tenant_id", "event_id") > "events".("tenant_id", "id") [delete: restrict]
Ref: "event_report_signals".("tenant_id", "event_id", "report_id") > "event_reports".("tenant_id", "event_id", "id") [delete: restrict]
Ref: "event_report_decisions"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "event_report_decisions".("tenant_id", "report_id") > "event_reports".("tenant_id", "id") [delete: restrict]
Ref: "event_report_decisions".("tenant_id", "report_id", "case_id") > "event_report_cases".("tenant_id", "report_id", "id") [delete: restrict]
Ref: "event_report_decisions"."moderator_user_id" > "users"."id" [delete: restrict]
Ref: "event_report_external_links"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "event_report_external_links".("tenant_id", "report_id") > "event_reports".("tenant_id", "id") [delete: restrict]
Ref: "event_report_external_links".("tenant_id", "report_id", "case_id") > "event_report_cases".("tenant_id", "report_id", "id") [delete: restrict]

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
Ref: "notifications"."notification_entity_type_id" > "notification_entity_types"."id" [delete: restrict]
Ref: "notifications"."notification_scope_id" > "notification_scope_types"."id" [delete: restrict]
Ref: "notifications"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "notifications"."user_id" > "users"."id" [delete: cascade]
Ref: "notifications"."source_actor_id" > "actors"."id" [delete: set null]
Ref: "notifications"."recipient_context_actor_id" > "actors"."id" [delete: set null]
Ref: "notification_fanout_runs"."tenant_id" > "tenants"."id" [delete: restrict]
Ref: "notification_fanout_runs"."notification_entity_type_id" > "notification_entity_types"."id" [delete: restrict]
Ref: "notification_fanout_runs".("tenant_id", "source_actor_id") > "actors".("tenant_id", "id") [delete: restrict]

// API Keys
Ref: "external_api_keys"."external_api_key_owner_type_id" > "external_api_key_owner_types"."id" [delete: restrict]
Ref: "external_api_keys"."external_api_key_status_id" > "external_api_key_statuses"."id" [delete: restrict]
Ref: "external_api_keys"."external_api_key_credit_period_id" > "external_api_key_credit_periods"."id" [delete: restrict]
Ref: "external_api_key_quotas"."external_api_key_id" > "external_api_keys"."id" [delete: cascade]

// Configuration & Secrets
Ref: "system_settings"."setting_value_type_id" > "setting_value_types"."id" [delete: restrict]
Ref: "configuration_change_logs"."setting_scope_id" > "setting_scopes"."id" [delete: restrict]
Ref: "secret_bindings"."setting_scope_id" > "setting_scopes"."id" [delete: restrict]
Ref: "secret_bindings"."secret_source_type_id" > "secret_source_types"."id" [delete: restrict]
Ref: "secret_bindings"."secret_validation_status_id" > "secret_validation_statuses"."id" [delete: restrict]
