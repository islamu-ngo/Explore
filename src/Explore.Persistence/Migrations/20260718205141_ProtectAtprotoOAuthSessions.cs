// ABOUTME: Replaces plaintext OAuth credential columns with an authenticated encrypted session envelope.
// ABOUTME: Refuses legacy-row conversion so deployment cannot silently invent or discard ATProto session state.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProtectAtprotoOAuthSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $migration$
                BEGIN
                    IF EXISTS (SELECT 1 FROM user_authentication_tokens) THEN
                        RAISE EXCEPTION 'ProtectAtprotoOAuthSessions requires user_authentication_tokens to be empty; legacy plaintext sessions must be revoked before migration.';
                    END IF;
                END
                $migration$;
                """);

            migrationBuilder.DropIndex(
                name: "ix_user_authentication_tokens_tenant_id",
                table: "user_authentication_tokens");

            migrationBuilder.DropColumn(
                name: "access_token",
                table: "user_authentication_tokens");

            migrationBuilder.DropColumn(
                name: "dpop_key",
                table: "user_authentication_tokens");

            migrationBuilder.DropColumn(
                name: "id_token",
                table: "user_authentication_tokens");

            migrationBuilder.DropColumn(
                name: "refresh_token",
                table: "user_authentication_tokens");

            migrationBuilder.AlterColumn<string>(
                name: "pds_host",
                table: "user_authentication_tokens",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_stamp",
                table: "user_authentication_tokens",
                type: "uuid",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "encryption_key_id",
                table: "user_authentication_tokens",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "envelope_version",
                table: "user_authentication_tokens",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "o_auth_client_key_id",
                table: "user_authentication_tokens",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "session_ciphertext",
                table: "user_authentication_tokens",
                type: "bytea",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "subject_did",
                table: "user_authentication_tokens",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "ux_user_authentication_tokens_tenant_provider_subject_did",
                table: "user_authentication_tokens",
                columns: new[] { "tenant_id", "provider", "subject_did" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_user_authentication_tokens_ciphertext_not_empty",
                table: "user_authentication_tokens",
                sql: "octet_length(session_ciphertext) >= 29");

            migrationBuilder.AddCheckConstraint(
                name: "ck_user_authentication_tokens_envelope_version",
                table: "user_authentication_tokens",
                sql: "envelope_version = 1");

            migrationBuilder.AddCheckConstraint(
                name: "ck_user_authentication_tokens_required_text",
                table: "user_authentication_tokens",
                sql: "length(btrim(provider)) > 0 AND length(btrim(subject_did)) > 0 AND length(btrim(encryption_key_id)) > 0 AND length(btrim(o_auth_client_key_id)) > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $migration$
                BEGIN
                    IF EXISTS (SELECT 1 FROM user_authentication_tokens) THEN
                        RAISE EXCEPTION 'Cannot downgrade ProtectAtprotoOAuthSessions while encrypted sessions exist; revoke them first because plaintext credentials cannot be reconstructed.';
                    END IF;
                END
                $migration$;
                """);

            migrationBuilder.DropIndex(
                name: "ux_user_authentication_tokens_tenant_provider_subject_did",
                table: "user_authentication_tokens");

            migrationBuilder.DropCheckConstraint(
                name: "ck_user_authentication_tokens_ciphertext_not_empty",
                table: "user_authentication_tokens");

            migrationBuilder.DropCheckConstraint(
                name: "ck_user_authentication_tokens_envelope_version",
                table: "user_authentication_tokens");

            migrationBuilder.DropCheckConstraint(
                name: "ck_user_authentication_tokens_required_text",
                table: "user_authentication_tokens");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "user_authentication_tokens");

            migrationBuilder.DropColumn(
                name: "encryption_key_id",
                table: "user_authentication_tokens");

            migrationBuilder.DropColumn(
                name: "envelope_version",
                table: "user_authentication_tokens");

            migrationBuilder.DropColumn(
                name: "o_auth_client_key_id",
                table: "user_authentication_tokens");

            migrationBuilder.DropColumn(
                name: "session_ciphertext",
                table: "user_authentication_tokens");

            migrationBuilder.DropColumn(
                name: "subject_did",
                table: "user_authentication_tokens");

            migrationBuilder.AlterColumn<string>(
                name: "pds_host",
                table: "user_authentication_tokens",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "access_token",
                table: "user_authentication_tokens",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "dpop_key",
                table: "user_authentication_tokens",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "id_token",
                table: "user_authentication_tokens",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "refresh_token",
                table: "user_authentication_tokens",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_authentication_tokens_tenant_id",
                table: "user_authentication_tokens",
                column: "tenant_id");
        }
    }
}
