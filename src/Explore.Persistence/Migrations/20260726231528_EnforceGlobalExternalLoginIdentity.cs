// ABOUTME: Aligns external login keys with bounded DIDs and enforces one global provider identity.
// ABOUTME: Aborts on duplicate upgrade data or overlong downgrade data before changing constraints.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceGlobalExternalLoginIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "provider_key",
                table: "user_external_logins",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM user_external_logins
                        WHERE provider IS NOT NULL
                          AND provider_key IS NOT NULL
                        GROUP BY provider, provider_key
                        HAVING COUNT(*) > 1)
                    THEN
                        RAISE EXCEPTION 'EnforceGlobalExternalLoginIdentity found duplicate external provider identities.';
                    END IF;
                END
                $$;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_user_external_logins_provider_provider_key",
                table: "user_external_logins",
                columns: new[] { "provider", "provider_key" },
                unique: true,
                filter: "provider IS NOT NULL AND provider_key IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_user_external_logins_provider_provider_key",
                table: "user_external_logins");

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM user_external_logins
                        WHERE length(provider_key) > 500)
                    THEN
                        RAISE EXCEPTION 'EnforceGlobalExternalLoginIdentity cannot downgrade provider keys longer than 500 characters.';
                    END IF;
                END
                $$;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "provider_key",
                table: "user_external_logins",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048,
                oldNullable: true);
        }
    }
}
