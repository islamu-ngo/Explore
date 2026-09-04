// ABOUTME: Exposes external Identity design-time creation from the MySQL migration assembly.
// ABOUTME: Lets EF tooling load generated MySQL migrations from its startup output.

using Explore.Persistence.Identity;
using Microsoft.EntityFrameworkCore.Design;

namespace Explore.Persistence.Migrations.MySql;

public sealed class MySqlExternalIdentityDbContextFactory
    : IDesignTimeDbContextFactory<ExternalIdentityDbContext>
{
    public ExternalIdentityDbContext CreateDbContext(string[] args) =>
        new ExternalIdentityDbContextFactory().CreateDbContext(args);
}
