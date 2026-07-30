// ABOUTME: PostgreSQL-backed tests for idempotency replay-cache persistence.
// ABOUTME: Verifies expired-row cleanup is bounded and preserves live replay records.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Persistence;
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

    [Test]
    public async Task TryClaimAsync_WhenTwoContextsUseTheSameKey_AllowsExactlyOneOwner()
    {
        await fixture.ResetAsync();
        var tenantId = Guid.CreateVersion7();
        var now = new DateTime(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);

        await using var winnerContext = fixture.CreateDbContext();
        await using var contenderContext = fixture.CreateDbContext();
        var winnerRepository = new IdempotencyRepository(winnerContext);
        var contenderRepository = new IdempotencyRepository(contenderContext);
        await using var winnerTransaction = await winnerContext.Database.BeginTransactionAsync();

        var winnerRecord = NewClaim("contended-key", tenantId, now);
        var contenderRecord = NewClaim("contended-key", tenantId, now);
        var winner = await winnerRepository.TryClaimAsync(winnerRecord);
        await Assert.That(winner.IsOwner).IsTrue();

        Task<IdempotencyClaim> contenderTask = contenderRepository.TryClaimAsync(contenderRecord);
        await Task.Delay(TimeSpan.FromMilliseconds(100));
        await Assert.That(contenderTask.IsCompleted).IsFalse();

        await winnerTransaction.CommitAsync();
        IdempotencyClaim contender = await contenderTask;

        await Assert.That(contender.IsOwner).IsFalse();
        await Assert.That(contender.Record.Id).IsEqualTo(winnerRecord.Id);

        await using var verificationContext = fixture.CreateDbContext();
        await Assert.That(await verificationContext.IdempotencyRecords
            .CountAsync(record => record.Key == "contended-key" && record.TenantId == tenantId)).IsEqualTo(1);
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

    private static IdempotencyRecord NewClaim(string key, Guid tenantId, DateTime now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            Key = key,
            TenantId = tenantId,
            UserId = "test-user",
            RequestMethod = "POST",
            RequestTarget = "/api/test",
            RequestBodyHash = Guid.NewGuid().ToString("N"),
            PrincipalFingerprint = Guid.NewGuid().ToString("N"),
            StatusCode = IdempotencyRecord.InProgressStatusCode,
            CreatedAt = now,
            ExpiresAt = now.AddHours(24)
        };
}
