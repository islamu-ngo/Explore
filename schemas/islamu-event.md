Table "actor_types" {
  "id" int [pk, not null]
  "full_name" varchar(500) [not null]
  "master_code" varchar(500) [not null]
  "description" varchar(500)
}

Table "analytics_providers" {
  "id" int [pk, not null]
  "master_code" varchar(500) [not null]
  "full_name" varchar(500) [not null]
  "description" varchar(500)
}

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
}

Table "approval_statuses" {
  "id" int [pk, not null]
  "master_code" text [not null]
  "full_name" text [not null]
  "description" text
}

Table "atproto_records" {
  "id" uuid [pk, not null]
  "did" varchar(255) [not null]
  "collection" varchar(500) [not null]
  "record_key" varchar(500) [not null]
  "cid" varchar(255)
  "uri" varchar(500)
  "indexed_at" timestamptz
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

Table "configuration_change_logs" {
  "id" uuid [pk, not null]
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
}

Table "did_custody_types" {
  "id" int [pk, not null]
  "master_code" varchar(500) [not null]
  "full_name" varchar(500) [not null]
  "description" varchar(500)
}

Table "event_formats" {
  "id" int [pk, not null]
  "master_code" varchar(500) [not null]
  "full_name" varchar(500) [not null]
  "description" varchar(500)
}

Table "event_statuses" {
  "id" int [pk, not null]
  "master_code" varchar(500) [not null]
  "full_name" varchar(500) [not null]
  "description" varchar(500)
}

Table "event_types" {
  "id" int [pk, not null]
  "full_name" text [not null]
  "master_code" text [not null]
  "description" text
}

Table "file_types" {
  "id" int [pk, not null]
  "master_code" varchar(500) [not null]
  "full_name" varchar(500) [not null]
  "description" varchar(500)
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

Table "InstanceBootstrapStates" {
  "id" uuid [pk, not null]
  "is_completed" boolean [not null]
  "created_at" timestamptz [not null]
  "completed_at" timestamptz
  "completed_by_user_id" uuid
  "selected_deployment_mode" varchar(32)
}

Table "languages" {
  "id" int [pk, not null]
  "master_code" varchar(500) [not null]
  "full_name" varchar(500) [not null]
  "description" varchar(500)
}

Table "madhabs" {
  "id" int [pk, not null]
  "master_code" varchar(500) [not null]
  "full_name" varchar(500) [not null]
  "description" varchar(500)
}

Table "ModuleDefinitions" {
  "id" uuid [pk, not null]
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

Table "organization_positions" {
  "id" int [pk, not null]
  "master_code" varchar(500) [not null]
  "full_name" varchar(500) [not null]
  "description" varchar(500)
}

Table "owner_types" {
  "id" int [pk, not null]
  "master_code" text [not null]
  "full_name" text [not null]
  "description" text
}

Table "pds_sync_outbox" {
  "id" uuid [pk, not null]
  "did" varchar(255) [not null]
  "collection" varchar(500) [not null]
  "record_key" varchar(500) [not null]
  "operation" int [not null]
  "payload" jsonb
  "pds_host" varchar(500)
  "status" int [not null]
  "created_at" timestamptz [not null]
  "processed_at" timestamptz
  "retry_count" int [not null]
  "last_error" varchar(2000)
  "next_retry_at" timestamptz
  "source_entity_type" varchar(100)
  "source_entity_id" uuid
}

Table "permissions" {
  "id" int [pk, not null]
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
}

Table "registration_modes" {
  "id" int [pk, not null]
  "master_code" varchar(50) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)
}

Table "roles" {
  "id" int [pk, not null]
  "master_code" varchar(100) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)
  "scope" int [not null]
  "is_system" boolean [not null]
}

Table "sync_states" {
  "id" int [pk, not null]
  "service" varchar(500) [not null]
  "cursor" bigint [not null]
  "last_seq_time" timestamptz
  "updated_at" timestamptz [not null]
}

Table "system_settings" {
  "id" uuid [pk, not null]
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

Table "tag_types" {
  "id" int [pk, not null]
  "master_code" varchar(500) [not null]
  "full_name" varchar(500) [not null]
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
  "master_code" varchar(500) [not null]
  "full_name" varchar(500) [not null]
  "description" varchar(500)
}

Table "role_permissions" {
  "role_id" int [pk, not null]
  "permission_id" int [pk, not null]
  "granted_at" timestamptz [not null]
  "granted_by" uuid
}

Table "tenants" {
  "id" uuid [pk, not null]
  "full_name" varchar(500) [not null]
  "slug" varchar(500) [not null]
  "description" varchar(500)
  "tenant_status_id" int [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
}

Table "categories" {
  "id" uuid [pk, not null]
  "master_code" varchar(500) [not null]
  "full_name" varchar(500) [not null]
  "parent_id" uuid
  "tenant_id" uuid [not null]
}

Table "locations" {
  "id" uuid [pk, not null]
  "full_name" varchar(500) [not null]
  "address" varchar(500) [not null]
  "postcode" varchar(500) [not null]
  "country" varchar(500) [not null]
  "city" varchar(500) [not null]
  "tenant_id" uuid [not null]
  "latitude" doubleprecision
  "longitude" doubleprecision
  "timezone" varchar(500)
}

Table "tags" {
  "id" uuid [pk, not null]
  "master_code" varchar(500) [not null]
  "full_name" varchar(500) [not null]
  "tenant_id" uuid [not null]
}

Table "tenant_navigation_links" {
  "id" uuid [pk, not null]
  "tenant_id" uuid [not null]
  "label" varchar(50) [not null]
  "url" varchar(500) [not null]
  "icon" varchar(100)
  "order" int [not null]
  "open_in_new_tab" boolean [not null]
  "is_active" boolean [not null]
}

Table "tenant_setting_overrides" {
  "id" uuid [pk, not null]
  "tenant_id" uuid [not null]
  "setting_key" varchar(256) [not null]
  "value" text [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
}

Table "TenantCapabilities" {
  "id" uuid [pk, not null]
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

Table "TenantInvitations" {
  "id" uuid [pk, not null]
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

Table "TenantLifecycleLogs" {
  "id" uuid [pk, not null]
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

Table "TenantOnboardingStates" {
  "id" uuid [pk, not null]
  "tenant_id" uuid [not null]
  "is_completed" boolean [not null]
  "current_step" int [not null]
  "total_steps" int [not null]
  "completed_steps_json" jsonb
  "created_at" timestamptz [not null]
  "completed_at" timestamptz
  "completed_by_user_id" uuid
}

Table "tag_type_tags" {
  "id" uuid [pk, not null]
  "tag_id" uuid [not null]
  "tag_type_id" int [not null]
  "tenant_id" uuid [not null]
}

Table "actor_key_stores" {
  "id" uuid [pk, not null]
  "actor_id" uuid [not null]
  "tenant_id" uuid [not null]
  "key_purpose" varchar(50) [not null]
  "private_key_encrypted" text [not null]
  "public_key" varchar(500) [not null]
  "is_active" boolean
  "created_at" timestamptz
}

Table "actors" {
  "id" uuid [pk, not null]
  "actor_type_id" int [not null]
  "user_id" uuid
  "organization_id" uuid
  "group_id" uuid
  "tenant_id" uuid [not null]
  "display_name" varchar(500) [not null]
  "profile_picture_id" uuid
  "did" varchar(500)
  "handle" varchar(500)
  "did_custody_type_id" int
  "pds_host" varchar(500)
  "description" varchar(500)
  "indexed_at" timestamptz
  "profile_picture_cid" varchar(500)
  "profile_picture_uri" varchar(500)
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
}

Table "organizations" {
  "id" uuid [pk, not null]
  "full_name" varchar(500) [not null]
  "email" varchar(500) [not null]
  "country" varchar(200)
  "city" varchar(200)
  "address" varchar(500)
  "postcode" varchar(50)
  "website_url" varchar(500)
  "metadata_json" text
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
}

Table "storage_objects" {
  "id" uuid [pk, not null]
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
}

Table "users" {
  "id" uuid [pk, not null]
  "email" varchar(500) [not null]
  "first_name" varchar(500) [not null]
  "last_name" varchar(500) [not null]
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
}

Table "events" {
  "id" uuid [pk, not null]
  "event_type_id" int
  "title" varchar(200) [not null]
  "subtitle" varchar(200)
  "description" varchar(5000)
  "audience_gender_id" int
  "audience_age_id" int
  "actor_id" uuid [not null]
  "price" decimal
  "currency_code" varchar(3)
  "featured_image_id" uuid
  "total_views" int [not null]
  "is_registration_required" boolean [not null]
  "is_user_reported" boolean [not null]
  "event_url" varchar(500)
  "madhab_id" int
  "tenant_id" uuid [not null]
  "slug" varchar(500)
  "visibility_type_id" int [not null]
  "session_count" int
  "event_status_id" int [not null]
  "external_registration_url" varchar(500)
  "first_session_date" date
  "last_session_date" date
  "timezone" varchar(100)
  "event_format_id" int [not null]
  "atproto_record_id" uuid
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
  "metadata_json" jsonb
}

Table "groups" {
  "id" uuid [pk, not null]
  "full_name" varchar(500) [not null]
  "description" varchar(5000)
  "profile_picture_id" uuid
  "approval_status_id" int [not null]
  "tenant_id" uuid [not null]
  "actor_id" uuid
  "metadata_json" text
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
}

Table "organization_members" {
  "id" uuid [pk, not null]
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

Table "PlatformUserRoles" {
  "id" uuid [pk, not null]
  "user_id" uuid [not null]
  "role_id" int [not null]
  "granted_at" timestamptz [not null]
  "granted_by" uuid
}

Table "tenant_members" {
  "id" uuid [pk, not null]
  "user_id" uuid [not null]
  "tenant_id" uuid [not null]
  "role_id" int [not null]
  "granted_at" timestamptz [not null]
  "granted_by" uuid
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
}

Table "tenant_users" {
  "id" uuid [pk, not null]
  "user_id" uuid [not null]
  "tenant_id" uuid [not null]
  "role_id" int [not null]
}

Table "user_authentication_tokens" {
  "id" uuid [pk, not null]
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
  "id" uuid [pk, not null]
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

Table "event_islamic_aspects" {
  "id" uuid [pk, not null]
  "madhab_id" int
  "reference_prayer" int
  "prayer_time_offset" int
  "gender_mode" int [not null]
  "includes_quran_recitation" boolean [not null]
  "primary_language_id" int
}

Table "event_sessions" {
  "id" uuid [pk, not null]
  "event_id" uuid [not null]
  "start_time" timestamptz [not null]
  "end_time" timestamptz [not null]
  "location_id" uuid
  "title" varchar(500)
  "tenant_id" uuid [not null]
  "slug" varchar(500)
  "max_audience_attendees" int
  "current_audience_attendees" int
  "registration_mode_id" int
  "description" varchar(500)
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
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

Table "event_tech_aspects" {
  "id" uuid [pk, not null]
  "github_repo_url" varchar(500)
  "hackathon_track" varchar(200)
  "skill_level" int [not null]
  "tech_stack_tags" varchar(1000)
  "requires_laptop" boolean [not null]
  "is_coding_competition" boolean [not null]
  "max_team_size" int
  "prize_pool" decimal
  "prize_currency_code" varchar(3)
}

Table "organization_reviews" {
  "id" uuid [pk, not null]
  "organization_id" uuid [not null]
  "program_id" uuid [not null]
  "user_id" uuid [not null]
  "reviewer_name" varchar(200) [not null]
  "rating" int [not null]
  "comment" varchar(2000)
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "tenant_id" uuid [not null]
}

Table "group_members" {
  "id" uuid [pk, not null]
  "group_id" uuid [not null]
  "user_id" uuid [not null]
  "role_id" int [not null]
  "tenant_id" uuid [not null]
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
  "is_deleted" boolean [not null]
  "deleted_at" timestamptz
  "deleted_by" uuid
}

Table "tenant_settings" {
  "id" uuid [pk, not null]
  "tenant_id" uuid [not null]
  "event_publishing_policy" int [not null]
  "allow_public_organization_registration" boolean [not null]
  "require_organization_verification" boolean [not null]
  "allow_public_group_creation" boolean [not null]
  "require_group_approval" boolean [not null]
  "default_organization_id" uuid
  "default_group_id" uuid
}

Table "event_registrations" {
  "id" uuid [pk, not null]
  "user_id" uuid [not null]
  "event_session_id" uuid [not null]
  "approval_status_id" int
  "tenant_id" uuid [not null]
  "atproto_record_id" uuid
  "created_at" timestamptz [not null]
  "created_by" uuid
  "updated_at" timestamptz
  "updated_by" uuid
}

Table "event_session_agenda_items" {
  "id" uuid [pk, not null]
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
}

Table "event_session_speakers" {
  "id" uuid [pk, not null]
  "actor_id" uuid [not null]
  "event_session_id" uuid [not null]
  "tenant_id" uuid [not null]
}

// Relationships
Ref: "tenants"."tenant_status_id" > "tenant_statuses"."id"
Ref: "categories"."parent_id" > "categories"."id"
Ref: "categories"."tenant_id" > "tenants"."id"
Ref: "locations"."tenant_id" > "tenants"."id"
Ref: "tags"."tenant_id" > "tenants"."id"
Ref: "tenant_navigation_links"."tenant_id" > "tenants"."id"
Ref: "tenant_setting_overrides"."tenant_id" > "tenants"."id"
Ref: "TenantCapabilities"."module_id" > "ModuleDefinitions"."id"
Ref: "TenantCapabilities"."tenant_id" > "tenants"."id"
Ref: "TenantInvitations"."role_id" > "roles"."id"
Ref: "TenantInvitations"."tenant_id" > "tenants"."id"
Ref: "TenantLifecycleLogs"."new_status_id" > "tenant_statuses"."id"
Ref: "TenantLifecycleLogs"."old_status_id" > "tenant_statuses"."id"
Ref: "TenantLifecycleLogs"."tenant_id" > "tenants"."id"
Ref: "TenantOnboardingStates"."tenant_id" > "tenants"."id"
Ref: "tag_type_tags"."tag_type_id" > "tag_types"."id"
Ref: "tag_type_tags"."tag_id" > "tags"."id"
Ref: "tag_type_tags"."tenant_id" > "tenants"."id"
Ref: "actor_key_stores"."tenant_id" > "tenants"."id"
Ref: "actors"."actor_type_id" > "actor_types"."id"
Ref: "actors"."did_custody_type_id" > "did_custody_types"."id"
Ref: "actors"."tenant_id" > "tenants"."id"
Ref: "organizations"."actor_id" > "actors"."id"
Ref: "organizations"."approval_status_id" > "approval_statuses"."id"
Ref: "organizations"."tenant_id" > "tenants"."id"
Ref: "storage_objects"."actor_id" > "actors"."id"
Ref: "storage_objects"."file_type_id" > "file_types"."id"
Ref: "storage_objects"."tenant_id" > "tenants"."id"
Ref: "users"."actor_id" > "actors"."id"
Ref: "events"."actor_id" > "actors"."id"
Ref: "events"."atproto_record_id" > "atproto_records"."id"
Ref: "events"."audience_age_id" > "audience_ages"."id"
Ref: "events"."audience_gender_id" > "audience_genders"."id"
Ref: "events"."event_format_id" > "event_formats"."id"
Ref: "events"."event_status_id" > "event_statuses"."id"
Ref: "events"."event_type_id" > "event_types"."id"
Ref: "events"."madhab_id" > "madhabs"."id"
Ref: "events"."featured_image_id" > "storage_objects"."id"
Ref: "events"."tenant_id" > "tenants"."id"
Ref: "events"."visibility_type_id" > "visibility_types"."id"
Ref: "groups"."actor_id" > "actors"."id"
Ref: "groups"."approval_status_id" > "approval_statuses"."id"
Ref: "groups"."profile_picture_id" > "storage_objects"."id"
Ref: "groups"."tenant_id" > "tenants"."id"
Ref: "organization_members"."organization_position_id" > "organization_positions"."id"
Ref: "organization_members"."organization_id" > "organizations"."id"
Ref: "organization_members"."role_id" > "roles"."id"
Ref: "organization_members"."tenant_id" > "tenants"."id"
Ref: "organization_members"."user_id" > "users"."id"
Ref: "PlatformUserRoles"."role_id" > "roles"."id"
Ref: "PlatformUserRoles"."user_id" > "users"."id"
Ref: "tenant_members"."role_id" > "roles"."id"
Ref: "tenant_members"."tenant_id" > "tenants"."id"
Ref: "tenant_members"."user_id" > "users"."id"
Ref: "tenant_users"."role_id" > "roles"."id"
Ref: "tenant_users"."tenant_id" > "tenants"."id"
Ref: "tenant_users"."user_id" > "users"."id"
Ref: "user_authentication_tokens"."tenant_id" > "tenants"."id"
Ref: "user_authentication_tokens"."user_id" > "users"."id"
Ref: "user_external_logins"."tenant_id" > "tenants"."id"
Ref: "user_external_logins"."user_id" > "users"."id"
Ref: "event_categories"."category_id" > "categories"."id"
Ref: "event_categories"."event_id" > "events"."id"
Ref: "event_categories"."tenant_id" > "tenants"."id"
Ref: "event_islamic_aspects"."id" > "events"."id"
Ref: "event_islamic_aspects"."primary_language_id" > "languages"."id"
Ref: "event_islamic_aspects"."madhab_id" > "madhabs"."id"
Ref: "event_sessions"."event_id" > "events"."id"
Ref: "event_sessions"."location_id" > "locations"."id"
Ref: "event_sessions"."registration_mode_id" > "registration_modes"."id"
Ref: "event_sessions"."tenant_id" > "tenants"."id"
Ref: "event_tags"."event_id" > "events"."id"
Ref: "event_tags"."tag_id" > "tags"."id"
Ref: "event_tags"."tenant_id" > "tenants"."id"
Ref: "event_tech_aspects"."id" > "events"."id"
Ref: "organization_reviews"."program_id" > "events"."id"
Ref: "organization_reviews"."organization_id" > "organizations"."id"
Ref: "organization_reviews"."tenant_id" > "tenants"."id"
Ref: "organization_reviews"."user_id" > "users"."id"
Ref: "group_members"."group_id" > "groups"."id"
Ref: "group_members"."role_id" > "roles"."id"
Ref: "group_members"."tenant_id" > "tenants"."id"
Ref: "group_members"."user_id" > "users"."id"
Ref: "tenant_settings"."default_group_id" > "groups"."id"
Ref: "tenant_settings"."default_organization_id" > "organizations"."id"
Ref: "tenant_settings"."tenant_id" > "tenants"."id"
Ref: "event_registrations"."approval_status_id" > "approval_statuses"."id"
Ref: "event_registrations"."atproto_record_id" > "atproto_records"."id"
Ref: "event_registrations"."event_session_id" > "event_sessions"."id"
Ref: "event_registrations"."tenant_id" > "tenants"."id"
Ref: "event_registrations"."user_id" > "users"."id"
Ref: "event_session_agenda_items"."event_session_id" > "event_sessions"."id"
Ref: "event_session_agenda_items"."location_id" > "locations"."id"
Ref: "event_session_agenda_items"."tenant_id" > "tenants"."id"
Ref: "event_session_languages"."event_session_id" > "event_sessions"."id"
Ref: "event_session_languages"."language_id" > "languages"."id"
Ref: "event_session_languages"."tenant_id" > "tenants"."id"
Ref: "event_session_speakers"."actor_id" > "actors"."id"
Ref: "event_session_speakers"."event_session_id" > "event_sessions"."id"
Ref: "event_session_speakers"."tenant_id" > "tenants"."id"
Ref: "actor_key_stores"."actor_id" > "actors"."id"
Ref: "actors"."group_id1" > "groups"."id"
Ref: "actors"."organization_id" > "organizations"."id"
Ref: "actors"."profile_picture_id" > "storage_objects"."id"
Ref: "actors"."user_id" > "users"."id"
