Table "organization" {
  "id" uuid [pk, not null]
  "full_name" varchar(500) [not null]
  "email" varchar(500) [not null]
  "country" varchar(500) [not null]
  "city" varchar(500) [not null]
  "address" varchar(500) [not null]
  "postcode" varchar(500) [not null]
  "website_url" varchar(500)
  "approval_status_id" int [not null]
  "tenant_id" uuid [not null]
  "actor_id" uuid [ref: < "actor"."id"]
}

Table "madhab" {
  "id" int [pk, not null, ref: < "event"."madhab_id"]
  "master_code" varchar(500) [not null]
  "full_name" varchar(500) [not null]
  "description" varchar(500)
}

Table "audience_age" {
  "id" int [pk, not null, ref: < "event"."audience_age_id"]
  "master_code" varchar(500) [not null]
  "full_name" varchar(500) [not null]
  "description" varchar(500)
  "min_age" int
  "max_age" int
}

Table "event_categories" {
  "id" int [pk, not null]
  "event_id" uuid [not null]
  "category_id" uuid [not null, ref: < "category"."id"]
  "tenant_id" uuid [not null]
}

Table "file_type" {
  "id" int [pk, not null, ref: < "storage_object"."file_type_id"]
  "master_code" varchar(500) [not null]
  "full_name" varchar(500) [not null]
  "description" varchar(500)
}

Table "location" {
  "id" uuid [pk, not null]
  "full_name" varchar(500) [not null]
  "address" varchar(500) [not null]
  "postcode" varchar(500) [not null]
  "country" varchar(500) [not null]
  "city" varchar(500) [not null]
  "tenant_id" uuid [not null]
  "coordinates" point
  "latitude" doubleprecision
  "longitude" doubleprecision
  "timezone" varchar(500)
}

Table "event_session" {
  "id" uuid [pk, not null]
  "event_id" uuid [not null, ref: < "event"."id"]
  "start_time" timestamptz [not null]
  "end_time" timestamptz [not null]
  "location_id" uuid [ref: < "location"."id"]
  "title" varchar(500)
  "tenant_id" uuid [not null]
  "slug" varchar(500)
  "max_audience_attendees" int
  "current_audience_attendees" int
  "registration_mode_id" int
  "description" varchar(500)
}

Table "event" {
  "id" uuid [pk, not null, ref: < "event_categories"."event_id"]
  "event_type_id" int [not null]
  "title" varchar(500) [not null]
  "description" varchar(500)
  "audience_gender_id" int [not null]
  "audience_age_id" int [not null]
  "actor_id" uuid [not null]
  "price" decimal
  "currency_code" varchar(500)
  "featured_image" uuid [not null]
  "total_views" int [not null]
  "is_registration_required" boolean [not null]
  "event_url" varchar(500)
  "madhab_id" int
  "tenant_id" uuid [not null]
  "slug" varchar(500)
  "visibility_type_id" int [not null, ref: < "visibility_type"."id"]
  "session_count" int
  "event_status_id" int [not null, ref: < "event_status"."id"]
  "external_registration_url" varchar(500)
  "first_session_date" date
  "last_session_date" date
  "timezone" varchar(500)
  "event_format_id" int [not null]
  "atproto_record_id" uuid [ref: < "atproto_record"."id"]
}

Table "organization_members" {
  "id" int [pk, not null]
  "organization_id" uuid [not null, ref: < "organization"."id"]
  "user_id" uuid [not null, ref: < "user"."id"]
  "organization_role_id" int [not null, ref: < "organization_role"."id"]
  "organization_position_id" int [ref: < "organization_position"."id"]
}

Table "approval_status" {
  "id" int [pk, not null, ref: < "organization"."approval_status_id", ref: < "event_registration"."approval_status_id"]
  "master_code" varchar(500) [not null]
  "full_name" varchar(500) [not null]
  "description" varchar(500)
}

Table "event_status" {
  "id" int [pk, not null]
  "master_code" varchar(500) [not null]
  "full_name" varchar(500) [not null]
  "description" varchar(500)
}

Table "organization_position" {
  "id" int [pk, not null]
  "master_code" varchar(500) [not null]
  "full_name" varchar(500) [not null]
  "description" varchar(500)
}

Table "event_tags" {
  "id" int [pk, not null]
  "event_id" uuid [not null, ref: < "event"."id"]
  "tag_id" uuid [not null, ref: < "tag"."id"]
  "tenant_id" uuid [not null]
}

Table "user_role" {
  "id" int [pk, not null, ref: < "tenant_user"."user_role_id"]
  "full_name" varchar(500) [not null]
  "master_code" varchar(500) [not null]
  "description" varchar(500)
  "tenant_id" uuid [not null]
}

Table "tag" {
  "id" uuid [pk, not null, ref: < "tag_type_tags"."tag_id"]
  "master_code" varchar(500) [not null]
  "full_name" varchar(500) [not null]
  "tenant_id" uuid [not null]
}

Table "user_authentication_token" {
  "id" uuid [pk, not null]
  "user_id" uuid [not null, ref: < "user"."id"]
  "tenant_id" uuid [not null]
  "provider" varchar(500) [not null]
  "access_token" varchar(500)
  "refresh_token" varchar(500)
  "pds_host" varchar(500)
  "dpop_key" varchar(500)
  "id_token" varchar(500)
  "expires_at" timestamp
}

Table "event_type" {
  "id" int [pk, not null, ref: < "event"."event_type_id"]
  "full_name" varchar(500) [not null]
  "master_code" varchar(500) [not null]
  "description" varchar(500)
}

Table "user_external_login" {
  "id" uuid [pk, not null]
  "user_id" uuid [not null]
  "tenant_id" uuid [not null]
  "provider" varchar(255) [note: '''User\'s own DID (if using ATProto OAuth)''']
  "provider_key" varchar(500) [note: 'Encrypted private key for signing']
  "provider_display_name" varchar(500) [note: 'Encrypted private key for rotation']
}

Table "category" {
  "id" uuid [pk, not null]
  "master_code" varchar(500) [not null]
  "full_name" varchar(500) [not null]
  "parent_id" uuid [ref: < "category"."id"]
  "tenant_id" uuid [not null]
}

Table "user" {
  "id" uuid [pk, not null, ref: < "tenant_user"."user_id", ref: < "user_external_login"."user_id"]
  "email" varchar(500) [not null]
  "first_name" varchar(500) [not null]
  "last_name" varchar(500) [not null]
  "actor_id" uuid [ref: < "actor"."id"]
  "auth_provider" varchar(500)
  "auth_provider_id" varchar(500)
  "default_actor_id" uuid [note: 'The primary AT Proto Actor this user controls']
  "email_verified" boolean
}

Table "storage_object" {
  "id" uuid [pk, not null, ref: < "actor"."profile_picture", ref: < "event"."featured_image"]
  "file_type_id" int [not null]
  "uri" varchar(500) [not null]
  "full_name" varchar(500) [not null]
  "extension" varchar(500) [not null]
  "size" bigint [not null]
  "tenant_id" uuid [not null]
  "actor_id" uuid [note: 'Owner id! we can know owner type with actor!']
}

Table "language" {
  "id" int [pk, not null]
  "master_code" varchar(500) [not null]
  "full_name" varchar(500) [not null]
  "description" varchar(500)
}

Table "audience_gender" {
  "id" int [pk, not null, ref: < "event"."audience_gender_id"]
  "master_code" varchar(500) [not null]
  "full_name" varchar(500) [not null]
  "description" varchar(500)
}

Table "event_session_languages" {
  "id" int [pk, not null]
  "event_session_id" uuid [not null, ref: < "event_session"."id"]
  "language_id" int [not null, ref: < "language"."id"]
  "tenant_id" uuid [not null]
}

Table "event_registration" {
  "id" uuid [pk, not null]
  "user_id" uuid [not null, ref: < "user"."id"]
  "event_session_id" uuid [not null, ref: < "event_session"."id"]
  "approval_status_id" int
  "tenant_id" uuid [not null]
  "atproto_record_id" uuid [ref: < "atproto_record"."id"]
}

Table "registration_mode" {
  "id" int [pk, not null, ref: < "event_session"."registration_mode_id"]
  "master_code" varchar(50) [not null]
  "full_name" varchar(200) [not null]
  "description" varchar(500)
}

Table "organization_role" {
  "id" int [pk, not null]
  "full_name" varchar(500) [not null]
  "master_code" varchar(500) [not null]
  "description" varchar(500)
}

Table "tenant" {
  "id" uuid [pk, not null, 
    ref: < "actor"."tenant_id", 
    ref: < "category"."tenant_id", 
    ref: < "event_registration"."tenant_id", 
    ref: < "event_session"."tenant_id", 
    ref: < "event"."tenant_id", 
    ref: < "location"."tenant_id", 
    ref: < "organization"."tenant_id", 
    ref: < "storage_object"."tenant_id", 
    ref: < "tag"."tenant_id", 
    ref: < "tenant_user"."tenant_id", 
    ref: < "user_role"."tenant_id", 
    ref: < "event_tags"."tenant_id",
    ref: < "actor_key_store"."tenant_id",
    ref: < "user_authentication_token"."tenant_id",
    ref: < "user_external_login"."tenant_id",
    ref: < "event_categories"."tenant_id",
    ref: < "event_session_languages"."tenant_id",
    ref: < "event_session_speakers"."tenant_id",
    ref: < "event_session_agenda_items"."tenant_id",
    ref: < "tag_type_tags"."tenant_id",
    ref: < "tenant_settings"."tenant_id"
  ]
  "full_name" varchar(500) [not null]
  "slug" varchar(500) [not null]
  "is_active" boolean [not null]
}

Table "tenant_user" {
  "id" int [pk, not null]
  "user_id" uuid [not null]
  "tenant_id" uuid [not null]
  "user_role_id" int [not null]
}

Table "tenant_settings" {
  "id" int [pk, not null]
  "tenant_id" uuid [not null]
}

Table "actor_type" {
  "id" int [pk, not null, ref: < "actor"."actor_type_id"]
  "full_name" varchar(500) [not null]
  "master_code" varchar(500) [not null]
  "description" varchar(500)
}

Table "actor" {
  "id" uuid [pk, not null, ref: < "event"."actor_id"]
  "actor_type_id" int [not null]
  "tenant_id" uuid [not null]
  "display_name" varchar(500) [not null]
  "profile_picture" uuid
  "did" varchar(500)
  "handle" varchar(500)
  "did_custody_type_id" int [ref: < "did_custody_type"."id"]
  "pds_host" varchar(500)
  "description" varchar(500)
  "indexed_at" timestamp
  "profile_picture_cid" varchar(500)
  "profile_picture_uri" varchar(500)
}

Table "indexed_did" {
  "did" varchar(255) [pk, not null, note: 'did:plc:xxx or did:web:xxx']
  "handle" varchar(255) [note: 'Current handle (e.g., alice.bsky.social)']
  "pds_host" varchar(500) [not null, note: 'PDS hosting this DID']
  "signing_key" text [note: 'Current signing public key']
  "is_active" boolean [not null]
  "last_indexed_at" timestamp [not null]
  "last_seen_at" timestamp
}

Table "sync_state" {
  "id" int [pk, not null]
  "service" varchar(500) [unique, not null, note: 'Relay URL']
  "cursor" bigint [not null, note: 'Last processed sequence number']
  "last_seq_time" timestamp
  "updated_at" timestamp [not null]
}

Table "did_custody_type" {
  "id" int [pk, not null]
  "master_code" varchar(500) [not null]
  "full_name" varchar(500) [not null]
  "description" varchar(500)
}

Table "visibility_type" {
  "id" int [pk, not null]
  "master_code" varchar(500) [not null]
  "full_name" varchar(500) [not null]
  "description" varchar(500)
}

Table "event_session_speakers" {
  "id" int [pk, not null]
  "actor_id" uuid [not null, note: 'align with decentralization-first identity model', ref: < "actor"."id"]
  "event_session_id" uuid [not null, ref: < "event_session"."id"]
  "tenant_id" uuid [not null]
}

Table "event_session_agenda_items" {
  "id" uuid [pk, not null]
  "event_session_id" uuid [not null, ref: < "event_session"."id"]
  "start_time" timestamptz [not null]
  "end_time" timestamptz [not null]
  "title" varchar(500) [not null]
  "description" varchar(500)
  "location_id" uuid [ref: < "location"."id"]
  "tenant_id" uuid [not null]
}

Table "actor_key_store" {
  "id" uuid [pk, not null]
  "actor_id" uuid [not null, ref: < "actor"."id"]
  "tenant_id" uuid [not null]
  "key_purpose" varchar(50) [not null, note: 'signing, rotation, encryption']
  "private_key_encrypted" text [not null, note: 'Use vault transit encryption']
  "public_key" varchar(500) [not null]
  "is_active" boolean
  "created_at" timestamptz
}

Table "tag_type" {
  "id" int [pk, not null, ref: < "tag_type_tags"."tag_type_id"]
  "master_code" varchar(500)
  "full_name" varchar(500)
}

Table "tag_type_tags" {
  "id" int [pk, not null]
  "tag_id" uuid [not null]
  "tag_type_id" int [not null]
  "tenant_id" uuid [not null]
}

Table "event_format" {
  "id" int [pk, not null, ref: < "event"."event_format_id"]
  "master_code" varchar(500) [not null]
  "full_name" varchar(500) [not null, note: 'local (in person), digital (online), hybrid (both)']
  "description" varchar(500)
}

Table "atproto_record" {
  "id" uuid [pk, not null]
  "did" varchar(255) [not null]
  "collection" varchar(500) [not null]
  "record_key" varchar(500) [not null]
  "cid" varchar(255)
  "uri" varchar(500)
  "indexed_at" timestamp
}
