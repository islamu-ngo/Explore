using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ClearExpiredPrivacyErasureCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_privacy_erasure_sagas_receipt_hash",
                table: "privacy_erasure_sagas");

            migrationBuilder.AlterColumn<byte[]>(
                name: "receipt_hash",
                table: "privacy_erasure_sagas",
                type: "bytea",
                fixedLength: true,
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldFixedLength: true,
                oldMaxLength: 32);

            migrationBuilder.AddCheckConstraint(
                name: "ck_privacy_erasure_sagas_receipt_hash",
                table: "privacy_erasure_sagas",
                sql: "receipt_hash IS NULL OR octet_length(receipt_hash) = 32");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_privacy_erasure_sagas_receipt_hash",
                table: "privacy_erasure_sagas");

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM privacy_erasure_sagas WHERE receipt_hash IS NULL) THEN
                        RAISE EXCEPTION 'Cannot restore required receipt hashes after credential destruction.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<byte[]>(
                name: "receipt_hash",
                table: "privacy_erasure_sagas",
                type: "bytea",
                fixedLength: true,
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldFixedLength: true,
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_privacy_erasure_sagas_receipt_hash",
                table: "privacy_erasure_sagas",
                sql: "octet_length(receipt_hash) = 32");
        }
    }
}
