// ABOUTME: Exposes external Identity design-time creation from the SQL Server migration assembly.
// ABOUTME: Lets EF tooling load generated SQL Server migrations from its startup output.

using Explore.Persistence.Identity;
using Microsoft.EntityFrameworkCore.Design;

namespace Explore.Persistence.Migrations.SqlServer;

public sealed class SqlServerExternalIdentityDbContextFactory
    : IDesignTimeDbContextFactory<ExternalIdentityDbContext>
{
    public ExternalIdentityDbContext CreateDbContext(string[] args) =>
        new ExternalIdentityDbContextFactory().CreateDbContext(args);
}
