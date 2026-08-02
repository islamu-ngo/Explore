// ABOUTME: Provider-neutral TLS policy for structured database composition.
// ABOUTME: Maps onto each native connection-string builder without exposing arbitrary fragments.

namespace Explore.Secrets.Database;

public enum PrimaryDatabaseTlsMode
{
    Prefer = 1,
    Required = 2,
    Disabled = 3,
}
