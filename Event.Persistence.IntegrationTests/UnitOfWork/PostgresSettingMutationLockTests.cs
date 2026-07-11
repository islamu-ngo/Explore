// ABOUTME: Unit-level contract tests for deterministic PostgreSQL setting advisory-lock keys.
// ABOUTME: Verifies canonical normalization, deduplication, and ordering without a container runtime.

using Explore.Persistence;

namespace Event.Persistence.IntegrationTests.UnitOfWork;

public sealed class PostgresSettingMutationLockTests
{
    [Test]
    public async Task NormalizeCanonicalKeys_ReturnsDistinctOrdinalOrder()
    {
        string[] normalized = PostgresSettingMutationLock.NormalizeCanonicalKeys(
            [" Zebra ", "beta", "ALPHA", "alpha"]);

        await Assert.That(normalized.SequenceEqual(
            ["alpha", "beta", "zebra"],
            StringComparer.Ordinal)).IsTrue();
    }
}
