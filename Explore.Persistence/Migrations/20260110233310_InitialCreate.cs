using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Explore.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "actor_types",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false),
                full_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                master_code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_actor_types", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "approval_statuses",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false),
                master_code = table.Column<string>(type: "text", nullable: false),
                full_name = table.Column<string>(type: "text", nullable: false),
                description = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_approval_statuses", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "atproto_records",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                did = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                collection = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                record_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                cid = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                uri = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                indexed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_atproto_records", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "audience_ages",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false),
                master_code = table.Column<string>(type: "text", nullable: false),
                full_name = table.Column<string>(type: "text", nullable: false),
                description = table.Column<string>(type: "text", nullable: true),
                min_age = table.Column<int>(type: "integer", nullable: true),
                max_age = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_audience_ages", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "audience_genders",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false),
                master_code = table.Column<string>(type: "text", nullable: false),
                full_name = table.Column<string>(type: "text", nullable: false),
                description = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_audience_genders", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "did_custody_types",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false),
                master_code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                full_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_did_custody_types", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "event_formats",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false),
                master_code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                full_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_event_formats", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "event_statuses",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false),
                master_code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                full_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_event_statuses", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "event_types",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                full_name = table.Column<string>(type: "text", nullable: false),
                master_code = table.Column<string>(type: "text", nullable: false),
                description = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_event_types", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "file_types",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false),
                master_code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                full_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_file_types", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "indexed_dids",
            columns: table => new
            {
                did = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                handle = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                pds_host = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                signing_key = table.Column<string>(type: "text", nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                last_indexed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_indexed_dids", x => x.did);
            });

        migrationBuilder.CreateTable(
            name: "languages",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false),
                master_code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                full_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_languages", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "madhabs",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false),
                master_code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                full_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_madhabs", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "organization_positions",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false),
                master_code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                full_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_organization_positions", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "organization_roles",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false),
                full_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                master_code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_organization_roles", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "owner_types",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                master_code = table.Column<string>(type: "text", nullable: false),
                full_name = table.Column<string>(type: "text", nullable: false),
                description = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_owner_types", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "registration_modes",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false),
                master_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_registration_modes", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "sync_states",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                service = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                cursor = table.Column<long>(type: "bigint", nullable: false),
                last_seq_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_sync_states", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "tag_types",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false),
                master_code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                full_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_tag_types", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "tenants",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                full_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_tenants", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "visibility_types",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false),
                master_code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                full_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_visibility_types", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "categories",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                master_code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                full_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_categories", x => x.id);
                table.ForeignKey(
                    name: "fk_categories_categories_parent_id",
                    column: x => x.parent_id,
                    principalTable: "categories",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_categories_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "locations",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                full_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                postcode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                country = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                city = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                latitude = table.Column<double>(type: "double precision", nullable: true),
                longitude = table.Column<double>(type: "double precision", nullable: true),
                timezone = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_locations", x => x.id);
                table.ForeignKey(
                    name: "fk_locations_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "tags",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                master_code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                full_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_tags", x => x.id);
                table.ForeignKey(
                    name: "fk_tags_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "tenant_settings",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_tenant_settings", x => x.id);
                table.ForeignKey(
                    name: "fk_tenant_settings_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "user_roles",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false),
                full_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                master_code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_user_roles", x => x.id);
                table.ForeignKey(
                    name: "fk_user_roles_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "tag_type_tags",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tag_id = table.Column<Guid>(type: "uuid", nullable: false),
                tag_type_id = table.Column<int>(type: "integer", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_tag_type_tags", x => x.id);
                table.ForeignKey(
                    name: "fk_tag_type_tags_tag_types_tag_type_id",
                    column: x => x.tag_type_id,
                    principalTable: "tag_types",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_tag_type_tags_tags_tag_id",
                    column: x => x.tag_id,
                    principalTable: "tags",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_tag_type_tags_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "actor_key_stores",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                key_purpose = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                private_key_encrypted = table.Column<string>(type: "text", nullable: false),
                public_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_actor_key_stores", x => x.id);
                table.ForeignKey(
                    name: "fk_actor_key_stores_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "actors",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                actor_type_id = table.Column<int>(type: "integer", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                display_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                profile_picture_id = table.Column<Guid>(type: "uuid", nullable: true),
                did = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                handle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                did_custody_type_id = table.Column<int>(type: "integer", nullable: true),
                pds_host = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                indexed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                profile_picture_cid = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                profile_picture_uri = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_actors", x => x.id);
                table.ForeignKey(
                    name: "fk_actors_actor_types_actor_type_id",
                    column: x => x.actor_type_id,
                    principalTable: "actor_types",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_actors_did_custody_types_did_custody_type_id",
                    column: x => x.did_custody_type_id,
                    principalTable: "did_custody_types",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_actors_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "organizations",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                full_name = table.Column<string>(type: "text", nullable: false),
                email = table.Column<string>(type: "text", nullable: false),
                country = table.Column<string>(type: "text", nullable: false),
                city = table.Column<string>(type: "text", nullable: false),
                address = table.Column<string>(type: "text", nullable: false),
                postcode = table.Column<string>(type: "text", nullable: false),
                website_url = table.Column<string>(type: "text", nullable: true),
                approval_status_id = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                actor_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_organizations", x => x.id);
                table.ForeignKey(
                    name: "fk_organizations_actors_actor_id",
                    column: x => x.actor_id,
                    principalTable: "actors",
                    principalColumn: "id");
                table.ForeignKey(
                    name: "fk_organizations_approval_statuses_approval_status_id",
                    column: x => x.approval_status_id,
                    principalTable: "approval_statuses",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_organizations_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "storage_objects",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                file_type_id = table.Column<int>(type: "integer", nullable: false),
                uri = table.Column<string>(type: "text", nullable: false),
                full_name = table.Column<string>(type: "text", nullable: false),
                extension = table.Column<string>(type: "text", nullable: false),
                size = table.Column<long>(type: "bigint", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                actor_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_storage_objects", x => x.id);
                table.ForeignKey(
                    name: "fk_storage_objects_actors_actor_id",
                    column: x => x.actor_id,
                    principalTable: "actors",
                    principalColumn: "id");
                table.ForeignKey(
                    name: "fk_storage_objects_file_types_file_type_id",
                    column: x => x.file_type_id,
                    principalTable: "file_types",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_storage_objects_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                email = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                first_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                last_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                auth_provider = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                auth_provider_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                default_actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                email_verified = table.Column<bool>(type: "boolean", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_users", x => x.id);
                table.ForeignKey(
                    name: "fk_users_actors_actor_id",
                    column: x => x.actor_id,
                    principalTable: "actors",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "events",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                event_type_id = table.Column<int>(type: "integer", nullable: false),
                title = table.Column<string>(type: "text", nullable: false),
                description = table.Column<string>(type: "text", nullable: true),
                audience_gender_id = table.Column<int>(type: "integer", nullable: false),
                audience_age_id = table.Column<int>(type: "integer", nullable: false),
                actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                price = table.Column<decimal>(type: "numeric", nullable: true),
                currency_code = table.Column<string>(type: "text", nullable: true),
                featured_image_id = table.Column<Guid>(type: "uuid", nullable: false),
                total_views = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                is_registration_required = table.Column<bool>(type: "boolean", nullable: false),
                event_url = table.Column<string>(type: "text", nullable: true),
                madhab_id = table.Column<int>(type: "integer", nullable: true),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                slug = table.Column<string>(type: "text", nullable: true),
                visibility_type_id = table.Column<int>(type: "integer", nullable: false),
                session_count = table.Column<int>(type: "integer", nullable: true),
                event_status_id = table.Column<int>(type: "integer", nullable: false),
                external_registration_url = table.Column<string>(type: "text", nullable: true),
                first_session_date = table.Column<DateOnly>(type: "date", nullable: true),
                last_session_date = table.Column<DateOnly>(type: "date", nullable: true),
                timezone = table.Column<string>(type: "text", nullable: true),
                event_format_id = table.Column<int>(type: "integer", nullable: false),
                atproto_record_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_events", x => x.id);
                table.ForeignKey(
                    name: "fk_events_actors_actor_id",
                    column: x => x.actor_id,
                    principalTable: "actors",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_events_atproto_records_atproto_record_id",
                    column: x => x.atproto_record_id,
                    principalTable: "atproto_records",
                    principalColumn: "id");
                table.ForeignKey(
                    name: "fk_events_audience_ages_audience_age_id",
                    column: x => x.audience_age_id,
                    principalTable: "audience_ages",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_events_audience_genders_audience_gender_id",
                    column: x => x.audience_gender_id,
                    principalTable: "audience_genders",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_events_event_formats_event_format_id",
                    column: x => x.event_format_id,
                    principalTable: "event_formats",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_events_event_statuses_event_status_id",
                    column: x => x.event_status_id,
                    principalTable: "event_statuses",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_events_event_types_event_type_id",
                    column: x => x.event_type_id,
                    principalTable: "event_types",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_events_madhabs_madhab_id",
                    column: x => x.madhab_id,
                    principalTable: "madhabs",
                    principalColumn: "id");
                table.ForeignKey(
                    name: "fk_events_storage_objects_featured_image_id",
                    column: x => x.featured_image_id,
                    principalTable: "storage_objects",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_events_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_events_visibility_types_visibility_type_id",
                    column: x => x.visibility_type_id,
                    principalTable: "visibility_types",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "organization_members",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                organization_role_id = table.Column<int>(type: "integer", nullable: false),
                organization_position_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_organization_members", x => x.id);
                table.ForeignKey(
                    name: "fk_organization_members_organization_positions_organization_po",
                    column: x => x.organization_position_id,
                    principalTable: "organization_positions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_organization_members_organization_roles_organization_role_id",
                    column: x => x.organization_role_id,
                    principalTable: "organization_roles",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_organization_members_organizations_organization_id",
                    column: x => x.organization_id,
                    principalTable: "organizations",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_organization_members_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "tenant_users",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_role_id = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_tenant_users", x => x.id);
                table.ForeignKey(
                    name: "fk_tenant_users_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_tenant_users_user_roles_user_role_id",
                    column: x => x.user_role_id,
                    principalTable: "user_roles",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_tenant_users_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "user_authentication_tokens",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                provider = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                access_token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                refresh_token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                pds_host = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                dpop_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                id_token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_user_authentication_tokens", x => x.id);
                table.ForeignKey(
                    name: "fk_user_authentication_tokens_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_user_authentication_tokens_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "user_external_logins",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                provider = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                provider_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                provider_display_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_user_external_logins", x => x.id);
                table.ForeignKey(
                    name: "fk_user_external_logins_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_user_external_logins_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "event_categories",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                event_id = table.Column<Guid>(type: "uuid", nullable: false),
                category_id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_event_categories", x => x.id);
                table.ForeignKey(
                    name: "fk_event_categories_categories_category_id",
                    column: x => x.category_id,
                    principalTable: "categories",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_event_categories_events_event_id",
                    column: x => x.event_id,
                    principalTable: "events",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_event_categories_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "event_sessions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                event_id = table.Column<Guid>(type: "uuid", nullable: false),
                start_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                end_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                location_id = table.Column<Guid>(type: "uuid", nullable: true),
                title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                max_audience_attendees = table.Column<int>(type: "integer", nullable: true),
                current_audience_attendees = table.Column<int>(type: "integer", nullable: true),
                registration_mode_id = table.Column<int>(type: "integer", nullable: true),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_event_sessions", x => x.id);
                table.ForeignKey(
                    name: "fk_event_sessions_events_event_id",
                    column: x => x.event_id,
                    principalTable: "events",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_event_sessions_locations_location_id",
                    column: x => x.location_id,
                    principalTable: "locations",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "fk_event_sessions_registration_modes_registration_mode_id",
                    column: x => x.registration_mode_id,
                    principalTable: "registration_modes",
                    principalColumn: "id");
                table.ForeignKey(
                    name: "fk_event_sessions_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "event_tags",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                event_id = table.Column<Guid>(type: "uuid", nullable: false),
                tag_id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_event_tags", x => x.id);
                table.ForeignKey(
                    name: "fk_event_tags_events_event_id",
                    column: x => x.event_id,
                    principalTable: "events",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_event_tags_tags_tag_id",
                    column: x => x.tag_id,
                    principalTable: "tags",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_event_tags_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "organization_reviews",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                program_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                reviewer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                rating = table.Column<int>(type: "integer", nullable: false),
                comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_organization_reviews", x => x.id);
                table.ForeignKey(
                    name: "fk_organization_reviews_events_event_id",
                    column: x => x.program_id,
                    principalTable: "events",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_organization_reviews_organizations_organization_id",
                    column: x => x.organization_id,
                    principalTable: "organizations",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_organization_reviews_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_organization_reviews_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "event_registrations",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                event_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                approval_status_id = table.Column<int>(type: "integer", nullable: true),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                atproto_record_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_event_registrations", x => x.id);
                table.ForeignKey(
                    name: "fk_event_registrations_approval_statuses_approval_status_id",
                    column: x => x.approval_status_id,
                    principalTable: "approval_statuses",
                    principalColumn: "id");
                table.ForeignKey(
                    name: "fk_event_registrations_atproto_records_atproto_record_id",
                    column: x => x.atproto_record_id,
                    principalTable: "atproto_records",
                    principalColumn: "id");
                table.ForeignKey(
                    name: "fk_event_registrations_event_sessions_event_session_id",
                    column: x => x.event_session_id,
                    principalTable: "event_sessions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_event_registrations_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_event_registrations_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "event_session_agenda_items",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                event_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                start_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                end_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                location_id = table.Column<Guid>(type: "uuid", nullable: true),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_event_session_agenda_items", x => x.id);
                table.ForeignKey(
                    name: "fk_event_session_agenda_items_event_sessions_event_session_id",
                    column: x => x.event_session_id,
                    principalTable: "event_sessions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_event_session_agenda_items_locations_location_id",
                    column: x => x.location_id,
                    principalTable: "locations",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "fk_event_session_agenda_items_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "event_session_languages",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                event_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                language_id = table.Column<int>(type: "integer", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_event_session_languages", x => x.id);
                table.ForeignKey(
                    name: "fk_event_session_languages_event_sessions_event_session_id",
                    column: x => x.event_session_id,
                    principalTable: "event_sessions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_event_session_languages_languages_language_id",
                    column: x => x.language_id,
                    principalTable: "languages",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_event_session_languages_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "event_session_speakers",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                event_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_event_session_speakers", x => x.id);
                table.ForeignKey(
                    name: "fk_event_session_speakers_actors_actor_id",
                    column: x => x.actor_id,
                    principalTable: "actors",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_event_session_speakers_event_sessions_event_session_id",
                    column: x => x.event_session_id,
                    principalTable: "event_sessions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_event_session_speakers_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.InsertData(
            table: "actor_types",
            columns: new[] { "id", "description", "full_name", "master_code" },
            values: new object[,]
            {
                { 1, "Individual user actor", "User", "USER" },
                { 2, "Organization actor", "Organization", "ORGANIZATION" },
                { 3, "Automated bot actor", "Bot", "BOT" }
            });

        migrationBuilder.InsertData(
            table: "approval_statuses",
            columns: new[] { "id", "description", "full_name", "master_code" },
            values: new object[,]
            {
                { 1, "Status is pending approval of Admin verifying the Existence of Legal Entity", "Pending", "PENDING" },
                { 2, "Status has been approved by Admin after verifying the Existence of Legal Entity", "Approved", "APPROVED" },
                { 3, "Status has been rejected by Admin after failing to verify the Existence of Legal Entity", "Rejected", "REJECTED" }
            });

        migrationBuilder.InsertData(
            table: "audience_ages",
            columns: new[] { "id", "description", "full_name", "master_code", "max_age", "min_age" },
            values: new object[,]
            {
                { 1, null, "All Ages", "ALL_AGES", null, null },
                { 2, null, "Adults Only (18+)", "ADULTS_18_PLUS", null, 18 },
                { 3, null, "Teens & Adults (16+)", "TEENS_16_PLUS", null, 16 },
                { 4, null, "Preteens & Up (12+)", "PRETEENS_12_PLUS", null, 12 },
                { 5, null, "Young Children (0-6)", "CHILDREN_UNDER_6", 6, null },
                { 6, null, "Children (0-12)", "YOUTH_UNDER_12", 12, null },
                { 7, null, "Children & Young Teens (0-16)", "YOUTH_UNDER_16", 16, null },
                { 8, null, "Youth (0-18)", "YOUTH_UNDER_18", 18, null }
            });

        migrationBuilder.InsertData(
            table: "audience_genders",
            columns: new[] { "id", "description", "full_name", "master_code" },
            values: new object[,]
            {
                { 1, "Only for Man Audience", "Man", "MAN" },
                { 2, "Only for Woman Audience", "Woman", "WOMAN" },
                { 3, "For Both Man and Woman but Segregated so no free mixing", "Both Segregated", "BOTH_SEGREGATED" },
                { 4, "For Both Man and Woman but Free Mixing", "Both Free Mixing", "BOTH_FREE_MIXING" }
            });

        migrationBuilder.InsertData(
            table: "did_custody_types",
            columns: new[] { "id", "description", "full_name", "master_code" },
            values: new object[,]
            {
                { 1, "Platform manages the DID keys", "Custodial", "CUSTODIAL" },
                { 2, "User manages their own DID keys", "Self-Custody", "SELF_CUSTODY" }
            });

        migrationBuilder.InsertData(
            table: "event_formats",
            columns: new[] { "id", "description", "full_name", "master_code" },
            values: new object[,]
            {
                { 1, "Event takes place at a physical location", "Local (In-Person)", "LOCAL" },
                { 2, "Event takes place online", "Digital (Online)", "DIGITAL" },
                { 3, "Event takes place both in-person and online", "Hybrid", "HYBRID" }
            });

        migrationBuilder.InsertData(
            table: "event_statuses",
            columns: new[] { "id", "description", "full_name", "master_code" },
            values: new object[,]
            {
                { 1, "Event is in draft state and not visible to the public", "Draft", "DRAFT" },
                { 2, "Event is published and visible to the public", "Published", "PUBLISHED" },
                { 3, "Event has been cancelled", "Cancelled", "CANCELLED" },
                { 4, "Event has been completed", "Completed", "COMPLETED" },
                { 5, "Event has been archived", "Archived", "ARCHIVED" }
            });

        migrationBuilder.InsertData(
            table: "event_types",
            columns: new[] { "id", "description", "full_name", "master_code" },
            values: new object[,]
            {
                { 1, null, "Conference", "CONFERENCE" },
                { 2, null, "Webinar", "WEBINAR" },
                { 3, null, "Workshop", "WORKSHOP" }
            });

        migrationBuilder.InsertData(
            table: "file_types",
            columns: new[] { "id", "description", "full_name", "master_code" },
            values: new object[,]
            {
                { 1, "Image file (PNG, JPG, GIF, etc.)", "Image", "IMAGE" },
                { 2, "Document file (PDF, DOC, etc.)", "Document", "DOCUMENT" },
                { 3, "Video file (MP4, AVI, etc.)", "Video", "VIDEO" },
                { 4, "Audio file (MP3, WAV, etc.)", "Audio", "AUDIO" },
                { 5, "Other file type", "Other", "OTHER" }
            });

        migrationBuilder.InsertData(
            table: "languages",
            columns: new[] { "id", "description", "full_name", "master_code" },
            values: new object[,]
            {
                { 1, "Arabic language", "Arabic", "AR" },
                { 2, "English language", "English", "EN" },
                { 3, "French language", "French", "FR" },
                { 4, "Turkish language", "Turkish", "TR" },
                { 5, "Urdu language", "Urdu", "UR" },
                { 6, "Indonesian language", "Indonesian", "ID" },
                { 7, "Malay language", "Malay", "MS" },
                { 8, "Bengali language", "Bengali", "BN" },
                { 9, "Persian/Farsi language", "Persian", "FA" },
                { 10, "German language", "German", "DE" },
                { 11, "Dutch language", "Dutch", "NL" },
                { 12, "Spanish language", "Spanish", "ES" }
            });

        migrationBuilder.InsertData(
            table: "madhabs",
            columns: new[] { "id", "description", "full_name", "master_code" },
            values: new object[,]
            {
                { 1, "Hanafi school of Islamic jurisprudence", "Hanafi", "HANAFI" },
                { 2, "Maliki school of Islamic jurisprudence", "Maliki", "MALIKI" },
                { 3, "Shafi'i school of Islamic jurisprudence", "Shafi'i", "SHAFII" },
                { 4, "Hanbali school of Islamic jurisprudence", "Hanbali", "HANBALI" },
                { 5, "Other Islamic jurisprudence approach", "Other", "OTHER" }
            });

        migrationBuilder.InsertData(
            table: "organization_positions",
            columns: new[] { "id", "description", "full_name", "master_code" },
            values: new object[,]
            {
                { 1, "Organization founder", "Founder", "FOUNDER" },
                { 2, "Organization director", "Director", "DIRECTOR" },
                { 3, "Organization manager", "Manager", "MANAGER" },
                { 4, "Teacher or instructor", "Teacher", "TEACHER" },
                { 6, "Organization secretary", "Secretary", "SECRETARY" },
                { 7, "Organization treasurer", "Treasurer", "TREASURER" },
                { 8, "Event or activity coordinator", "Coordinator", "COORDINATOR" },
                { 9, "Organization volunteer", "Volunteer", "VOLUNTEER" },
                { 10, "Organization intern", "Intern", "INTERN" },
                { 11, "Organization advisor", "Advisor", "ADVISOR" },
                { 12, "Organization consultant", "Consultant", "CONSULTANT" },
                { 14, "Supervisor", "Supervisor", "SUPERVISOR" },
                { 15, "Assistant", "Assistant", "ASSISTANT" },
                { 16, "General staff member", "Staff", "STAFF" }
            });

        migrationBuilder.InsertData(
            table: "organization_roles",
            columns: new[] { "id", "description", "full_name", "master_code" },
            values: new object[,]
            {
                { 1, "Organization creator with full ownership", "Creator", "CREATOR" },
                { 2, "Co-owner with near-full access", "Co-Owner", "CO_OWNER" },
                { 3, "Organization Administrator with management access", "Administrator", "ADMIN" },
                { 4, "Organization Moderator with limited access", "Moderator", "MODERATOR" },
                { 5, "Regular organization member", "Member", "MEMBER" },
                { 6, "Read-only access to organization", "Viewer", "VIEWER" }
            });

        migrationBuilder.InsertData(
            table: "registration_modes",
            columns: new[] { "id", "description", "full_name", "master_code" },
            values: new object[,]
            {
                { 1, "Anyone can register", "Open", "OPEN" },
                { 2, "Registration requires approval", "Approval Required", "APPROVAL_REQUIRED" },
                { 3, "Only invited users can register", "Invite Only", "INVITE_ONLY" },
                { 4, "Registration is closed", "Closed", "CLOSED" }
            });

        migrationBuilder.InsertData(
            table: "tag_types",
            columns: new[] { "id", "description", "full_name", "master_code" },
            values: new object[,]
            {
                { 1, "Topic-based tags for content categorization", "Topic", "TOPIC" },
                { 2, "Skill level requirements (beginner, intermediate, advanced)", "Skill Level", "SKILL" },
                { 3, "Language-based tags", "Language", "LANGUAGE" },
                { 4, "Target audience tags", "Audience", "AUDIENCE" }
            });

        migrationBuilder.InsertData(
            table: "tenants",
            columns: new[] { "id", "full_name", "is_active", "slug" },
            values: new object[] { new Guid("018e4e5c-7f00-7000-8000-000000000001"), "ISLAMU Default Tenant", true, "default" });

        migrationBuilder.InsertData(
            table: "visibility_types",
            columns: new[] { "id", "description", "full_name", "master_code" },
            values: new object[,]
            {
                { 1, "Visible to everyone", "Public", "PUBLIC" },
                { 2, "Only visible to invited members", "Private", "PRIVATE" },
                { 3, "Not listed publicly but accessible via direct link", "Unlisted", "UNLISTED" },
                { 4, "Only visible to organization members", "Members Only", "MEMBERS_ONLY" }
            });

        migrationBuilder.InsertData(
            table: "actors",
            columns: new[] { "id", "actor_type_id", "description", "did", "did_custody_type_id", "display_name", "handle", "indexed_at", "pds_host", "profile_picture_cid", "profile_picture_id", "profile_picture_uri", "tenant_id" },
            values: new object[,]
            {
                { new Guid("018e4e5c-7f00-7000-8000-000000000020"), 3, "System actor for automated operations", null, null, "System", "system", null, null, null, null, null, new Guid("018e4e5c-7f00-7000-8000-000000000001") },
                { new Guid("018e4e5c-7f00-7000-8000-000000000021"), 2, "ISLAMU NGO - Islamic Learning and Media Union", null, null, "ISLAMU", "islamu", null, null, null, null, null, new Guid("018e4e5c-7f00-7000-8000-000000000001") }
            });

        migrationBuilder.InsertData(
            table: "categories",
            columns: new[] { "id", "full_name", "master_code", "parent_id", "tenant_id" },
            values: new object[,]
            {
                { new Guid("018e4e5c-7f00-7000-8000-000000000100"), "Islamic Studies", "ISLAMIC_STUDIES", null, new Guid("018e4e5c-7f00-7000-8000-000000000001") },
                { new Guid("018e4e5c-7f00-7000-8000-000000000106"), "Arabic Language", "ARABIC", null, new Guid("018e4e5c-7f00-7000-8000-000000000001") },
                { new Guid("018e4e5c-7f00-7000-8000-000000000107"), "Community Events", "COMMUNITY", null, new Guid("018e4e5c-7f00-7000-8000-000000000001") }
            });

        migrationBuilder.InsertData(
            table: "locations",
            columns: new[] { "id", "address", "city", "country", "full_name", "latitude", "longitude", "postcode", "tenant_id", "timezone" },
            values: new object[] { new Guid("018e4e5c-7f00-7000-8000-000000000300"), "Virtual", "Virtual", "Internet", "Online / Virtual", null, null, "00000", new Guid("018e4e5c-7f00-7000-8000-000000000001"), "UTC" });

        migrationBuilder.InsertData(
            table: "tags",
            columns: new[] { "id", "full_name", "master_code", "tenant_id" },
            values: new object[,]
            {
                { new Guid("018e4e5c-7f00-7000-8000-000000000200"), "Beginner", "BEGINNER", new Guid("018e4e5c-7f00-7000-8000-000000000001") },
                { new Guid("018e4e5c-7f00-7000-8000-000000000201"), "Intermediate", "INTERMEDIATE", new Guid("018e4e5c-7f00-7000-8000-000000000001") },
                { new Guid("018e4e5c-7f00-7000-8000-000000000202"), "Advanced", "ADVANCED", new Guid("018e4e5c-7f00-7000-8000-000000000001") },
                { new Guid("018e4e5c-7f00-7000-8000-000000000203"), "Free", "FREE", new Guid("018e4e5c-7f00-7000-8000-000000000001") },
                { new Guid("018e4e5c-7f00-7000-8000-000000000204"), "Paid", "PAID", new Guid("018e4e5c-7f00-7000-8000-000000000001") },
                { new Guid("018e4e5c-7f00-7000-8000-000000000205"), "Online", "ONLINE", new Guid("018e4e5c-7f00-7000-8000-000000000001") },
                { new Guid("018e4e5c-7f00-7000-8000-000000000206"), "In-Person", "IN_PERSON", new Guid("018e4e5c-7f00-7000-8000-000000000001") }
            });

        migrationBuilder.InsertData(
            table: "user_roles",
            columns: new[] { "id", "description", "full_name", "master_code", "tenant_id" },
            values: new object[,]
            {
                { 1, "Full system access across all tenants", "Super Administrator", "SUPER_ADMIN", new Guid("018e4e5c-7f00-7000-8000-000000000001") },
                { 2, "Tenant administrator with full access within tenant", "Administrator", "ADMIN", new Guid("018e4e5c-7f00-7000-8000-000000000001") },
                { 3, "Content moderation and user management", "Moderator", "MODERATOR", new Guid("018e4e5c-7f00-7000-8000-000000000001") },
                { 4, "Standard user role", "User", "USER", new Guid("018e4e5c-7f00-7000-8000-000000000001") }
            });

        migrationBuilder.InsertData(
            table: "categories",
            columns: new[] { "id", "full_name", "master_code", "parent_id", "tenant_id" },
            values: new object[,]
            {
                { new Guid("018e4e5c-7f00-7000-8000-000000000101"), "Quran & Tafsir", "QURAN", new Guid("018e4e5c-7f00-7000-8000-000000000100"), new Guid("018e4e5c-7f00-7000-8000-000000000001") },
                { new Guid("018e4e5c-7f00-7000-8000-000000000102"), "Hadith Sciences", "HADITH", new Guid("018e4e5c-7f00-7000-8000-000000000100"), new Guid("018e4e5c-7f00-7000-8000-000000000001") },
                { new Guid("018e4e5c-7f00-7000-8000-000000000103"), "Fiqh (Islamic Jurisprudence)", "FIQH", new Guid("018e4e5c-7f00-7000-8000-000000000100"), new Guid("018e4e5c-7f00-7000-8000-000000000001") },
                { new Guid("018e4e5c-7f00-7000-8000-000000000104"), "Aqeedah (Islamic Creed)", "AQEEDAH", new Guid("018e4e5c-7f00-7000-8000-000000000100"), new Guid("018e4e5c-7f00-7000-8000-000000000001") },
                { new Guid("018e4e5c-7f00-7000-8000-000000000105"), "Seerah (Prophetic Biography)", "SEERAH", new Guid("018e4e5c-7f00-7000-8000-000000000100"), new Guid("018e4e5c-7f00-7000-8000-000000000001") }
            });

        migrationBuilder.InsertData(
            table: "organizations",
            columns: new[] { "id", "actor_id", "address", "approval_status_id", "city", "country", "email", "full_name", "postcode", "tenant_id", "website_url" },
            values: new object[] { new Guid("018e4e5c-7f00-7000-8000-000000000040"), new Guid("018e4e5c-7f00-7000-8000-000000000021"), "Parc Du Peterbos", 2, "Brussels", "Belgium", "contact@openislamu.org", "ISLAMU", "1070", new Guid("018e4e5c-7f00-7000-8000-000000000001"), "https://islamu.ngo" });

        migrationBuilder.InsertData(
            table: "users",
            columns: new[] { "id", "actor_id", "auth_provider", "auth_provider_id", "default_actor_id", "email", "email_verified", "first_name", "last_name" },
            values: new object[] { new Guid("018e4e5c-7f00-7000-8000-000000000030"), new Guid("018e4e5c-7f00-7000-8000-000000000020"), "system", "system", null, "system@islamu.org", true, "System", "Account" });

        migrationBuilder.CreateIndex(
            name: "ix_actor_key_stores_actor_id",
            table: "actor_key_stores",
            column: "actor_id");

        migrationBuilder.CreateIndex(
            name: "ix_actor_key_stores_tenant_id",
            table: "actor_key_stores",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_actors_actor_type_id",
            table: "actors",
            column: "actor_type_id");

        migrationBuilder.CreateIndex(
            name: "ix_actors_did_custody_type_id",
            table: "actors",
            column: "did_custody_type_id");

        migrationBuilder.CreateIndex(
            name: "ix_actors_profile_picture_id",
            table: "actors",
            column: "profile_picture_id");

        migrationBuilder.CreateIndex(
            name: "ix_actors_tenant_id",
            table: "actors",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_atproto_records_did_collection_record_key",
            table: "atproto_records",
            columns: new[] { "did", "collection", "record_key" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_categories_parent_id",
            table: "categories",
            column: "parent_id");

        migrationBuilder.CreateIndex(
            name: "ix_categories_tenant_id",
            table: "categories",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_event_categories_category_id",
            table: "event_categories",
            column: "category_id");

        migrationBuilder.CreateIndex(
            name: "ix_event_categories_event_id",
            table: "event_categories",
            column: "event_id");

        migrationBuilder.CreateIndex(
            name: "ix_event_categories_tenant_id",
            table: "event_categories",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_event_registrations_approval_status_id",
            table: "event_registrations",
            column: "approval_status_id");

        migrationBuilder.CreateIndex(
            name: "ix_event_registrations_atproto_record_id",
            table: "event_registrations",
            column: "atproto_record_id");

        migrationBuilder.CreateIndex(
            name: "ix_event_registrations_event_session_id",
            table: "event_registrations",
            column: "event_session_id");

        migrationBuilder.CreateIndex(
            name: "ix_event_registrations_tenant_id",
            table: "event_registrations",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_event_registrations_user_id",
            table: "event_registrations",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_event_session_agenda_items_event_session_id",
            table: "event_session_agenda_items",
            column: "event_session_id");

        migrationBuilder.CreateIndex(
            name: "ix_event_session_agenda_items_location_id",
            table: "event_session_agenda_items",
            column: "location_id");

        migrationBuilder.CreateIndex(
            name: "ix_event_session_agenda_items_tenant_id",
            table: "event_session_agenda_items",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_event_session_languages_event_session_id",
            table: "event_session_languages",
            column: "event_session_id");

        migrationBuilder.CreateIndex(
            name: "ix_event_session_languages_language_id",
            table: "event_session_languages",
            column: "language_id");

        migrationBuilder.CreateIndex(
            name: "ix_event_session_languages_tenant_id",
            table: "event_session_languages",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_event_session_speakers_actor_id",
            table: "event_session_speakers",
            column: "actor_id");

        migrationBuilder.CreateIndex(
            name: "ix_event_session_speakers_event_session_id",
            table: "event_session_speakers",
            column: "event_session_id");

        migrationBuilder.CreateIndex(
            name: "ix_event_session_speakers_tenant_id",
            table: "event_session_speakers",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_event_sessions_event_id",
            table: "event_sessions",
            column: "event_id");

        migrationBuilder.CreateIndex(
            name: "ix_event_sessions_location_id",
            table: "event_sessions",
            column: "location_id");

        migrationBuilder.CreateIndex(
            name: "ix_event_sessions_registration_mode_id",
            table: "event_sessions",
            column: "registration_mode_id");

        migrationBuilder.CreateIndex(
            name: "ix_event_sessions_tenant_id",
            table: "event_sessions",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_event_tags_event_id",
            table: "event_tags",
            column: "event_id");

        migrationBuilder.CreateIndex(
            name: "ix_event_tags_tag_id",
            table: "event_tags",
            column: "tag_id");

        migrationBuilder.CreateIndex(
            name: "ix_event_tags_tenant_id",
            table: "event_tags",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_events_actor_id",
            table: "events",
            column: "actor_id");

        migrationBuilder.CreateIndex(
            name: "ix_events_atproto_record_id",
            table: "events",
            column: "atproto_record_id");

        migrationBuilder.CreateIndex(
            name: "ix_events_audience_age_id",
            table: "events",
            column: "audience_age_id");

        migrationBuilder.CreateIndex(
            name: "ix_events_audience_gender_id",
            table: "events",
            column: "audience_gender_id");

        migrationBuilder.CreateIndex(
            name: "ix_events_event_format_id",
            table: "events",
            column: "event_format_id");

        migrationBuilder.CreateIndex(
            name: "ix_events_event_status_id",
            table: "events",
            column: "event_status_id");

        migrationBuilder.CreateIndex(
            name: "ix_events_event_type_id",
            table: "events",
            column: "event_type_id");

        migrationBuilder.CreateIndex(
            name: "ix_events_featured_image_id",
            table: "events",
            column: "featured_image_id");

        migrationBuilder.CreateIndex(
            name: "ix_events_madhab_id",
            table: "events",
            column: "madhab_id");

        migrationBuilder.CreateIndex(
            name: "ix_events_tenant_id",
            table: "events",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_events_visibility_type_id",
            table: "events",
            column: "visibility_type_id");

        migrationBuilder.CreateIndex(
            name: "ix_locations_tenant_id",
            table: "locations",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_organization_members_organization_id",
            table: "organization_members",
            column: "organization_id");

        migrationBuilder.CreateIndex(
            name: "ix_organization_members_organization_position_id",
            table: "organization_members",
            column: "organization_position_id");

        migrationBuilder.CreateIndex(
            name: "ix_organization_members_organization_role_id",
            table: "organization_members",
            column: "organization_role_id");

        migrationBuilder.CreateIndex(
            name: "ix_organization_members_user_id",
            table: "organization_members",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_organization_reviews_event_id",
            table: "organization_reviews",
            column: "program_id");

        migrationBuilder.CreateIndex(
            name: "ix_organization_reviews_organization_id",
            table: "organization_reviews",
            column: "organization_id");

        migrationBuilder.CreateIndex(
            name: "ix_organization_reviews_tenant_id",
            table: "organization_reviews",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_organization_reviews_user_id",
            table: "organization_reviews",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_organizations_actor_id",
            table: "organizations",
            column: "actor_id");

        migrationBuilder.CreateIndex(
            name: "ix_organizations_approval_status_id",
            table: "organizations",
            column: "approval_status_id");

        migrationBuilder.CreateIndex(
            name: "ix_organizations_tenant_id",
            table: "organizations",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_storage_objects_actor_id",
            table: "storage_objects",
            column: "actor_id");

        migrationBuilder.CreateIndex(
            name: "ix_storage_objects_file_type_id",
            table: "storage_objects",
            column: "file_type_id");

        migrationBuilder.CreateIndex(
            name: "ix_storage_objects_tenant_id",
            table: "storage_objects",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_sync_states_service",
            table: "sync_states",
            column: "service",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_tag_type_tags_tag_id",
            table: "tag_type_tags",
            column: "tag_id");

        migrationBuilder.CreateIndex(
            name: "ix_tag_type_tags_tag_type_id",
            table: "tag_type_tags",
            column: "tag_type_id");

        migrationBuilder.CreateIndex(
            name: "ix_tag_type_tags_tenant_id",
            table: "tag_type_tags",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_tags_tenant_id",
            table: "tags",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_tenant_settings_tenant_id",
            table: "tenant_settings",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_tenant_users_tenant_id",
            table: "tenant_users",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_tenant_users_user_id",
            table: "tenant_users",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_tenant_users_user_role_id",
            table: "tenant_users",
            column: "user_role_id");

        migrationBuilder.CreateIndex(
            name: "ix_tenants_slug",
            table: "tenants",
            column: "slug",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_user_authentication_tokens_tenant_id",
            table: "user_authentication_tokens",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_user_authentication_tokens_user_id",
            table: "user_authentication_tokens",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_user_external_logins_tenant_id",
            table: "user_external_logins",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_user_external_logins_user_id",
            table: "user_external_logins",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_user_roles_tenant_id",
            table: "user_roles",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_users_actor_id",
            table: "users",
            column: "actor_id");

        migrationBuilder.AddForeignKey(
            name: "fk_actor_key_stores_actors_actor_id",
            table: "actor_key_stores",
            column: "actor_id",
            principalTable: "actors",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "fk_actors_storage_objects_profile_picture_id",
            table: "actors",
            column: "profile_picture_id",
            principalTable: "storage_objects",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_storage_objects_actors_actor_id",
            table: "storage_objects");

        migrationBuilder.DropTable(
            name: "actor_key_stores");

        migrationBuilder.DropTable(
            name: "event_categories");

        migrationBuilder.DropTable(
            name: "event_registrations");

        migrationBuilder.DropTable(
            name: "event_session_agenda_items");

        migrationBuilder.DropTable(
            name: "event_session_languages");

        migrationBuilder.DropTable(
            name: "event_session_speakers");

        migrationBuilder.DropTable(
            name: "event_tags");

        migrationBuilder.DropTable(
            name: "indexed_dids");

        migrationBuilder.DropTable(
            name: "organization_members");

        migrationBuilder.DropTable(
            name: "organization_reviews");

        migrationBuilder.DropTable(
            name: "owner_types");

        migrationBuilder.DropTable(
            name: "sync_states");

        migrationBuilder.DropTable(
            name: "tag_type_tags");

        migrationBuilder.DropTable(
            name: "tenant_settings");

        migrationBuilder.DropTable(
            name: "tenant_users");

        migrationBuilder.DropTable(
            name: "user_authentication_tokens");

        migrationBuilder.DropTable(
            name: "user_external_logins");

        migrationBuilder.DropTable(
            name: "categories");

        migrationBuilder.DropTable(
            name: "languages");

        migrationBuilder.DropTable(
            name: "event_sessions");

        migrationBuilder.DropTable(
            name: "organization_positions");

        migrationBuilder.DropTable(
            name: "organization_roles");

        migrationBuilder.DropTable(
            name: "organizations");

        migrationBuilder.DropTable(
            name: "tag_types");

        migrationBuilder.DropTable(
            name: "tags");

        migrationBuilder.DropTable(
            name: "user_roles");

        migrationBuilder.DropTable(
            name: "users");

        migrationBuilder.DropTable(
            name: "events");

        migrationBuilder.DropTable(
            name: "locations");

        migrationBuilder.DropTable(
            name: "registration_modes");

        migrationBuilder.DropTable(
            name: "approval_statuses");

        migrationBuilder.DropTable(
            name: "atproto_records");

        migrationBuilder.DropTable(
            name: "audience_ages");

        migrationBuilder.DropTable(
            name: "audience_genders");

        migrationBuilder.DropTable(
            name: "event_formats");

        migrationBuilder.DropTable(
            name: "event_statuses");

        migrationBuilder.DropTable(
            name: "event_types");

        migrationBuilder.DropTable(
            name: "madhabs");

        migrationBuilder.DropTable(
            name: "visibility_types");

        migrationBuilder.DropTable(
            name: "actors");

        migrationBuilder.DropTable(
            name: "actor_types");

        migrationBuilder.DropTable(
            name: "did_custody_types");

        migrationBuilder.DropTable(
            name: "storage_objects");

        migrationBuilder.DropTable(
            name: "file_types");

        migrationBuilder.DropTable(
            name: "tenants");
    }
}
