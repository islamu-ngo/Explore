using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.SqlServer.Migrations.Identity
{
    /// <inheritdoc />
    public partial class ExternalIdentityInit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "islamu_identity");

            migrationBuilder.CreateTable(
                name: "local_identity_roles",
                schema: "islamu_identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_local_identity_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "local_identity_users",
                schema: "islamu_identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    first_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    last_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    user_name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "bit", nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    security_stamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    phone_number = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "bit", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "bit", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "bit", nullable: false),
                    access_failed_count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_local_identity_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "identity_role_claims",
                schema: "islamu_identity",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    role_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    claim_type = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    claim_value = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_identity_role_claims_local_identity_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "islamu_identity",
                        principalTable: "local_identity_roles",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "identity_user_claims",
                schema: "islamu_identity",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    claim_type = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    claim_value = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_identity_user_claims_local_identity_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "islamu_identity",
                        principalTable: "local_identity_users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "identity_user_logins",
                schema: "islamu_identity",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    provider_key = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    provider_display_name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_identity_user_logins_local_identity_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "islamu_identity",
                        principalTable: "local_identity_users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "identity_user_roles",
                schema: "islamu_identity",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    role_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_identity_user_roles_local_identity_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "islamu_identity",
                        principalTable: "local_identity_roles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_identity_user_roles_local_identity_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "islamu_identity",
                        principalTable: "local_identity_users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "identity_user_tokens",
                schema: "islamu_identity",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    login_provider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_identity_user_tokens_local_identity_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "islamu_identity",
                        principalTable: "local_identity_users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_identity_role_claims_role_id",
                schema: "islamu_identity",
                table: "identity_role_claims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_identity_user_claims_user_id",
                schema: "islamu_identity",
                table: "identity_user_claims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_identity_user_logins_user_id",
                schema: "islamu_identity",
                table: "identity_user_logins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_identity_user_roles_role_id",
                schema: "islamu_identity",
                table: "identity_user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_local_identity_roles_normalized_name",
                schema: "islamu_identity",
                table: "local_identity_roles",
                column: "normalized_name",
                unique: true,
                filter: "[normalized_name] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_local_identity_users_normalized_email",
                schema: "islamu_identity",
                table: "local_identity_users",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "ix_local_identity_users_normalized_user_name",
                schema: "islamu_identity",
                table: "local_identity_users",
                column: "normalized_user_name",
                unique: true,
                filter: "[normalized_user_name] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "identity_role_claims",
                schema: "islamu_identity");

            migrationBuilder.DropTable(
                name: "identity_user_claims",
                schema: "islamu_identity");

            migrationBuilder.DropTable(
                name: "identity_user_logins",
                schema: "islamu_identity");

            migrationBuilder.DropTable(
                name: "identity_user_roles",
                schema: "islamu_identity");

            migrationBuilder.DropTable(
                name: "identity_user_tokens",
                schema: "islamu_identity");

            migrationBuilder.DropTable(
                name: "local_identity_roles",
                schema: "islamu_identity");

            migrationBuilder.DropTable(
                name: "local_identity_users",
                schema: "islamu_identity");
        }
    }
}
