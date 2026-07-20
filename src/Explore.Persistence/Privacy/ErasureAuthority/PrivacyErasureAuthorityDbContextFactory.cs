// ABOUTME: Creates the narrow authority context with an inert EF design-time target.
// ABOUTME: Requires EF tooling's explicit --connection override for database updates.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Explore.Persistence.Privacy.ErasureAuthority;

public sealed class PrivacyErasureAuthorityDbContextFactory
    : IDesignTimeDbContextFactory<PrivacyErasureAuthorityDbContext>
{
    public PrivacyErasureAuthorityDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PrivacyErasureAuthorityDbContext>()
            .UseNpgsql(
                "Host=127.0.0.1;Port=1;Database=privacy_erasure_authority_design_time;Username=design_time;Password=design_time;Timeout=1",
                npgsql => npgsql.MigrationsAssembly("Explore.Persistence"))
            .UseSnakeCaseNamingConvention()
            .Options;
        return new PrivacyErasureAuthorityDbContext(options);
    }
}
