using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProtectPrivacyErasureProviderLocators : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM privacy_erasure_provider_work) THEN
                        RAISE EXCEPTION 'Provider locator migration requires an empty pre-materialization work table.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AddColumn<DateTime>(
                name: "locator_expires_at_utc",
                table: "privacy_erasure_provider_work",
                type: "timestamp with time zone",
                nullable: false);

            migrationBuilder.AddColumn<short>(
                name: "locator_kind",
                table: "privacy_erasure_provider_work",
                type: "smallint",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "locator_protection_version",
                table: "privacy_erasure_provider_work",
                type: "integer",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "protected_locator",
                table: "privacy_erasure_provider_work",
                type: "character varying(8192)",
                maxLength: 8192,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_privacy_erasure_provider_work_locator_expiry",
                table: "privacy_erasure_provider_work",
                sql: "locator_expires_at_utc > created_at_utc");

            migrationBuilder.AddCheckConstraint(
                name: "ck_privacy_erasure_provider_work_locator_kind",
                table: "privacy_erasure_provider_work",
                sql: "locator_kind BETWEEN 1 AND 7");

            migrationBuilder.AddCheckConstraint(
                name: "ck_privacy_erasure_provider_work_locator_lifecycle",
                table: "privacy_erasure_provider_work",
                sql: "(status = 5 AND protected_locator IS NULL) OR (status <> 5 AND protected_locator IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_privacy_erasure_provider_work_locator_version",
                table: "privacy_erasure_provider_work",
                sql: "locator_protection_version >= 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_privacy_erasure_provider_work_locator_expiry",
                table: "privacy_erasure_provider_work");

            migrationBuilder.DropCheckConstraint(
                name: "ck_privacy_erasure_provider_work_locator_kind",
                table: "privacy_erasure_provider_work");

            migrationBuilder.DropCheckConstraint(
                name: "ck_privacy_erasure_provider_work_locator_lifecycle",
                table: "privacy_erasure_provider_work");

            migrationBuilder.DropCheckConstraint(
                name: "ck_privacy_erasure_provider_work_locator_version",
                table: "privacy_erasure_provider_work");

            migrationBuilder.DropColumn(
                name: "locator_expires_at_utc",
                table: "privacy_erasure_provider_work");

            migrationBuilder.DropColumn(
                name: "locator_kind",
                table: "privacy_erasure_provider_work");

            migrationBuilder.DropColumn(
                name: "locator_protection_version",
                table: "privacy_erasure_provider_work");

            migrationBuilder.DropColumn(
                name: "protected_locator",
                table: "privacy_erasure_provider_work");
        }
    }
}
