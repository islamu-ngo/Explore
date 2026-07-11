// ABOUTME: PostgreSQL-backed tests for idempotency replay-cache persistence.
// ABOUTME: Verifies expired-row cleanup is bounded and preserves live replay records.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Domain;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class IdempotencyRepositoryTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task DeleteExpiredAsync_DeletesOnlyExpiredRowsUpToBatchSize()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var repository = new IdempotencyRepository(context);
        var tenantId = Guid.NewGuid();
        var now = new DateTime(2026, 5, 28, 12, 0, 0, DateTimeKind.Utc);

        context.IdempotencyRecords.AddRange(
            NewRecord("expired-1", tenantId, now.AddHours(-4)),
            NewRecord("expired-2", tenantId, now.AddHours(-3)),
            NewRecord("expired-3", tenantId, now.AddHours(-2)),
            NewRecord("live", tenantId, now.AddHours(1)));
        await context.SaveChangesAsync();

        var eligibleBefore = await repository.CountExpiredAsync(now.AddHours(-1), batchSize: 2, CancellationToken.None);
        var deleted = await repository.DeleteExpiredAsync(now.AddHours(-1), batchSize: 2, CancellationToken.None);

        await Assert.That(eligibleBefore).IsEqualTo(2);
        await Assert.That(deleted).IsEqualTo(2);
        await Assert.That(await context.IdempotencyRecords.CountAsync()).IsEqualTo(2);
        await Assert.That(await context.IdempotencyRecords.AnyAsync(record => record.Key == "live")).IsTrue();
        await Assert.That(await context.IdempotencyRecords.AnyAsync(record => record.Key == "expired-3")).IsTrue();
    }

    private static IdempotencyRecord NewRecord(string key, Guid tenantId, DateTime expiresAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            Key = key,
            TenantId = tenantId,
            UserId = "test-user",
            RequestMethod = "POST",
            RequestTarget = "/api/test",
            RequestBodyHash = Guid.NewGuid().ToString("N"),
            PrincipalFingerprint = Guid.NewGuid().ToString("N"),
            StatusCode = 201,
            ContentType = "application/json",
            ResponseBody = "{}",
            CreatedAt = expiresAt.AddHours(-24),
            ExpiresAt = expiresAt
        };
}
