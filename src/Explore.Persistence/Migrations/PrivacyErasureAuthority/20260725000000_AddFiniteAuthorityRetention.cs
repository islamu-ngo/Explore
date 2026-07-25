// ABOUTME: Adds the finite-retention external authority append function without rewriting the initial migration.
// ABOUTME: Grants the runtime role only the new function while preserving the historical function for rollback.

using Explore.Persistence.Privacy.ErasureAuthority;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.PrivacyErasureAuthority;

[DbContext(typeof(PrivacyErasureAuthorityDbContext))]
[Migration("20260725000000_AddFiniteAuthorityRetention")]
public sealed class AddFiniteAuthorityRetention : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(PrivacyErasureAuthorityDatabaseContract.MigrationSql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(PrivacyErasureAuthorityDatabaseContract.FiniteRetentionRollbackSql);
    }
}
