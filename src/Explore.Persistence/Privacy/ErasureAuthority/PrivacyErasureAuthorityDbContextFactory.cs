// ABOUTME: Creates the narrow authority context with an inert EF design-time target.
// ABOUTME: Requires EF tooling's explicit --connection override for database updates.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace Explore.Persistence.Privacy.ErasureAuthority;

public sealed class PrivacyErasureAuthorityDbContextFactory
    : IDesignTimeDbContextFactory<PrivacyErasureAuthorityDbContext>
{
    public PrivacyErasureAuthorityDbContext CreateDbContext(string[] args)
    {
        const string connectionArgument = "--connection";
        const string inertConnection =
            "Host=127.0.0.1;Port=1;Database=privacy_erasure_authority_design_time;Username=design_time;Password=design_time;Timeout=1";

        int connectionIndex = Array.IndexOf(args, connectionArgument);
        if (connectionIndex >= 0 && Array.LastIndexOf(args, connectionArgument) != connectionIndex)
        {
            throw new ArgumentException("Specify --connection exactly once.", nameof(args));
        }

        string connectionString = inertConnection;
        if (connectionIndex >= 0)
        {
            if (connectionIndex == args.Length - 1
                || string.IsNullOrWhiteSpace(args[connectionIndex + 1])
                || args[connectionIndex + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException("--connection requires one value.", nameof(args));
            }

            try
            {
                var candidate = new NpgsqlConnectionStringBuilder(args[connectionIndex + 1]);
                if (string.IsNullOrWhiteSpace(candidate.Host)
                    || string.IsNullOrWhiteSpace(candidate.Database)
                    || string.IsNullOrWhiteSpace(candidate.Username))
                {
                    throw new ArgumentException();
                }

                connectionString = candidate.ConnectionString;
            }
            catch (ArgumentException)
            {
                throw new ArgumentException(
                    "The --connection value is not a valid PostgreSQL target.",
                    nameof(args));
            }
        }

        var options = new DbContextOptionsBuilder<PrivacyErasureAuthorityDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly("Explore.Persistence"))
            .UseSnakeCaseNamingConvention()
            .Options;
        return new PrivacyErasureAuthorityDbContext(options);
    }
}
