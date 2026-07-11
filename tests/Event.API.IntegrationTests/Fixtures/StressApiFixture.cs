// ABOUTME: Stress test fixture backed by real PostgreSQL with rate limiting explicitly enabled.
// Used for timing-sensitive scenarios: rate limiting enforcement, timeout handling, auth conflicts.

namespace Event.Api.IntegrationTests.Fixtures;

/// <summary>
/// Stress test fixture: PostgreSQL-backed with rate limiting enabled.
/// For timing-sensitive scenarios requiring real middleware enforcement.
/// </summary>
public class StressApiFixture : PostgreSqlApiFixtureBase
{
    protected override Dictionary<string, string?> GetAdditionalConfiguration() => new()
    {
        ["Testing:HostProfile"] = TestHostProfile.Stress,
        ["RateLimiting:DisableInTesting"] = "false",
        // Low thresholds to trigger 429 in tests without excessive request volume.
        // Global policy exempts loopback IPs, so only Authenticated and Write are testable.
        ["RateLimiting:Write:PermitLimit"] = "3",
        ["RateLimiting:Write:WindowSeconds"] = "60",
        ["RateLimiting:Authenticated:PermitLimit"] = "5",
        ["RateLimiting:Authenticated:WindowSeconds"] = "60",
        ["RateLimiting:SetupSecret:PermitLimit"] = "2",
        ["RateLimiting:SetupSecret:WindowSeconds"] = "60",
    };
}
