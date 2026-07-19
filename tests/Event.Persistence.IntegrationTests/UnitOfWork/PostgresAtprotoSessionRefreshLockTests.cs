// ABOUTME: Verifies PostgreSQL serializes ATProto refresh leases for one exact tenant/user/DID scope.
// ABOUTME: Proves a competing application instance cannot rotate the same provider session concurrently.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Persistence;

namespace Event.Persistence.IntegrationTests.UnitOfWork;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class PostgresAtprotoSessionRefreshLockTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task SameScopeWaitsUntilTheAuthoritativeLeaseIsReleased()
    {
        await using var firstContext = fixture.CreateDbContext();
        await using var secondContext = fixture.CreateDbContext();
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var firstLock = new PostgresAtprotoSessionRefreshLock(firstContext);
        var secondLock = new PostgresAtprotoSessionRefreshLock(secondContext);
        await using var firstLease = await firstLock.AcquireAsync(
            tenantId, userId, "atproto", "did:plc:refresh-lock", CancellationToken.None);

        Task<IAsyncDisposable> competing = secondLock.AcquireAsync(
            tenantId, userId, "atproto", "did:plc:refresh-lock", CancellationToken.None);
        Task early = await Task.WhenAny(competing, Task.Delay(TimeSpan.FromMilliseconds(250)));

        await Assert.That(ReferenceEquals(early, competing)).IsFalse();
        await firstLease.DisposeAsync();
        await using var secondLease = await competing.WaitAsync(TimeSpan.FromSeconds(5));
    }

}

public sealed class PostgresAtprotoSessionRefreshLockKeyTests
{
    [Test]
    public async Task StableKeyIncludesEverySecurityScopeDimension()
    {
        var tenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");
        var userId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000002");
        long baseline = PostgresAtprotoSessionRefreshLock.ComputeStableLockKey(
            tenantId, userId, "atproto", "did:plc:alice");

        await Assert.That(PostgresAtprotoSessionRefreshLock.ComputeStableLockKey(
            tenantId, userId, "atproto", "did:plc:alice")).IsEqualTo(baseline);
        await Assert.That(PostgresAtprotoSessionRefreshLock.ComputeStableLockKey(
            Guid.CreateVersion7(), userId, "atproto", "did:plc:alice")).IsNotEqualTo(baseline);
        await Assert.That(PostgresAtprotoSessionRefreshLock.ComputeStableLockKey(
            tenantId, Guid.CreateVersion7(), "atproto", "did:plc:alice")).IsNotEqualTo(baseline);
        await Assert.That(PostgresAtprotoSessionRefreshLock.ComputeStableLockKey(
            tenantId, userId, "other", "did:plc:alice")).IsNotEqualTo(baseline);
        await Assert.That(PostgresAtprotoSessionRefreshLock.ComputeStableLockKey(
            tenantId, userId, "atproto", "did:plc:bob")).IsNotEqualTo(baseline);
    }
}
