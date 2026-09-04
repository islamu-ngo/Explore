using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.Sqlite.Migrations.Identity
{
    /// <inheritdoc />
    public partial class ExternalIdentityInit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ie_local_identity_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_local_identity_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ie_local_identity_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    first_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    last_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    user_name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    password_hash = table.Column<string>(type: "TEXT", nullable: true),
                    security_stamp = table.Column<string>(type: "TEXT", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "TEXT", nullable: true),
                    phone_number = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    access_failed_count = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_local_identity_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ie_identity_role_claims",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    role_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    claim_type = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    claim_value = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_identity_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_identity_role_claims_local_identity_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "ie_local_identity_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ie_identity_user_claims",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    claim_type = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    claim_value = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_identity_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_identity_user_claims_local_identity_users_user_id",
                        column: x => x.user_id,
                        principalTable: "ie_local_identity_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ie_identity_user_logins",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    provider_key = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    provider_display_name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_identity_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_identity_user_logins_local_identity_users_user_id",
                        column: x => x.user_id,
                        principalTable: "ie_local_identity_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ie_identity_user_roles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    role_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_identity_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_identity_user_roles_local_identity_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "ie_local_identity_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_identity_user_roles_local_identity_users_user_id",
                        column: x => x.user_id,
                        principalTable: "ie_local_identity_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ie_identity_user_tokens",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    login_provider = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_identity_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_identity_user_tokens_local_identity_users_user_id",
                        column: x => x.user_id,
                        principalTable: "ie_local_identity_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_identity_role_claims_role_id",
                table: "ie_identity_role_claims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_identity_user_claims_user_id",
                table: "ie_identity_user_claims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_identity_user_logins_user_id",
                table: "ie_identity_user_logins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_identity_user_roles_role_id",
                table: "ie_identity_user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_local_identity_roles_normalized_name",
                table: "ie_local_identity_roles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_local_identity_users_normalized_email",
                table: "ie_local_identity_users",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "ix_local_identity_users_normalized_user_name",
                table: "ie_local_identity_users",
                column: "normalized_user_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ie_identity_role_claims");

            migrationBuilder.DropTable(
                name: "ie_identity_user_claims");

            migrationBuilder.DropTable(
                name: "ie_identity_user_logins");

            migrationBuilder.DropTable(
                name: "ie_identity_user_roles");

            migrationBuilder.DropTable(
                name: "ie_identity_user_tokens");

            migrationBuilder.DropTable(
                name: "ie_local_identity_roles");

            migrationBuilder.DropTable(
                name: "ie_local_identity_users");
        }
    }
}
