using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManagedControlPlaneRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "managed_control_plane_registrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    managed_instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    control_plane_endpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    management_api_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    event_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    deployment_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    request_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    event_to_control_plane_key_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    event_to_control_plane_secret_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    control_plane_to_event_key_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    control_plane_to_event_secret_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    credential_secret_binding_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_to_control_plane_credential_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    control_plane_to_event_credential_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    last_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_failure_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    registered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_managed_control_plane_registrations", x => x.id);
                    table.CheckConstraint("ck_managed_control_plane_registration_expiry", "event_to_control_plane_credential_expires_at > created_at AND control_plane_to_event_credential_expires_at > created_at");
                    table.CheckConstraint("ck_managed_control_plane_registration_registered", "(status IN ('Registered', 'Revoked')) = (registered_at IS NOT NULL)");
                    table.CheckConstraint("ck_managed_control_plane_registration_revoked", "(status = 'Revoked') = (revoked_at IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_managed_control_plane_registrations_secret_bindings_credent",
                        column: x => x.credential_secret_binding_id,
                        principalTable: "secret_bindings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_managed_control_plane_registrations_control_plane_to_event_",
                table: "managed_control_plane_registrations",
                column: "control_plane_to_event_key_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_managed_control_plane_registrations_credential_secret_bindi",
                table: "managed_control_plane_registrations",
                column: "credential_secret_binding_id");

            migrationBuilder.CreateIndex(
                name: "ix_managed_control_plane_registrations_event_instance_id",
                table: "managed_control_plane_registrations",
                column: "event_instance_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_managed_control_plane_registrations_event_to_control_plane_",
                table: "managed_control_plane_registrations",
                column: "event_to_control_plane_key_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_managed_control_plane_registrations_managed_instance_id",
                table: "managed_control_plane_registrations",
                column: "managed_instance_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "managed_control_plane_registrations");
        }
    }
}
