// ABOUTME: Provider-neutral structured database settings for one role.
// ABOUTME: Created separately for runtime and migrator composition without raw connection strings.

namespace Explore.Secrets.Database;

public sealed record PrimaryDatabaseConnectionOptions
{
    public required PrimaryDatabaseRole Role { get; init; }

    public required PrimaryDatabaseProvider Provider { get; init; }

    public string? Host { get; init; }

    public int? Port { get; init; }

    public string? Database { get; init; }

    public string? Username { get; init; }

    public string? Password { get; init; }

    public PrimaryDatabaseTlsMode TlsMode { get; init; } = PrimaryDatabaseTlsMode.Prefer;

    public bool TrustServerCertificate { get; init; }

    public PrimaryDatabaseServerFlavor? ServerFlavor { get; init; }

    public Version? ServerVersion { get; init; }
}
