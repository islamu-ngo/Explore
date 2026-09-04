// ABOUTME: Exposes external Identity design-time creation from the SQLite migration assembly.
// ABOUTME: Lets EF tooling load generated SQLite migrations from its startup output.

using Explore.Persistence.Identity;
using Microsoft.EntityFrameworkCore.Design;

namespace Explore.Persistence.Migrations.Sqlite;

public sealed class SqliteExternalIdentityDbContextFactory
    : IDesignTimeDbContextFactory<ExternalIdentityDbContext>
{
    public ExternalIdentityDbContext CreateDbContext(string[] args) =>
        new ExternalIdentityDbContextFactory().CreateDbContext(args);
}
