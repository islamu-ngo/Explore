// ABOUTME: File-backed SQLite regressions for atomic idempotency-key claims.
// ABOUTME: Verifies one contention winner and expired-record takeover through the production EF model.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Persistence;
using Explore.Persistence.Database;
using Explore.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

[NotInParallel("SqliteIdempotency")]
public sealed class IdempotencyRepositorySqliteTests
{
    [Test]
    public async Task TryClaimAsync_WhenFileBackedSqliteContends_AllowsExactlyOneOwner()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"idempotency-{Guid.CreateVersion7():N}.db");

        try
        {
            await CreateDatabaseAsync(databasePath);
            var tenantId = Guid.CreateVersion7();
            var createdAt = new DateTime(2026, 8, 2, 18, 0, 0, DateTimeKind.Utc);
            ExploreDbContext[] contexts = Enumerable.Range(0, 4)
                .Select(_ => CreateContext(databasePath))
                .ToArray();

            try
            {
                var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                Task<IdempotencyClaim>[] attempts = contexts.Select(async context =>
                {
                    await start.Task;
                    return await new IdempotencyRepository(context)
                        .TryClaimAsync(NewClaim("contended-key", tenantId, createdAt));
                }).ToArray();

                start.SetResult();
                IdempotencyClaim[] claims = await Task.WhenAll(attempts).WaitAsync(TimeSpan.FromSeconds(10));

                await Assert.That(claims.Count(claim => claim.IsOwner)).IsEqualTo(1);
                await Assert.That(claims.Select(claim => claim.Record.Id).Distinct().Count()).IsEqualTo(1);
            }
            finally
            {
                foreach (ExploreDbContext context in contexts)
                {
                    await context.DisposeAsync();
                }
            }
        }
        finally
        {
            File.Delete(databasePath);
            File.Delete($"{databasePath}-shm");
            File.Delete($"{databasePath}-wal");
        }
    }

    [Test]
    public async Task TryClaimAsync_WhenExistingSqliteClaimExpired_TransfersOwnership()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"idempotency-{Guid.CreateVersion7():N}.db");

        try
        {
            await CreateDatabaseAsync(databasePath);
            var tenantId = Guid.CreateVersion7();
            var now = new DateTime(2026, 8, 2, 18, 0, 0, DateTimeKind.Utc);

            await using (var firstContext = CreateContext(databasePath))
            {
                var first = await new IdempotencyRepository(firstContext)
                    .TryClaimAsync(NewClaim("expired-key", tenantId, now.AddHours(-2), now.AddHours(-1)));
                await Assert.That(first.IsOwner).IsTrue();
            }

            await using (var takeoverContext = CreateContext(databasePath))
            {
                var replacement = NewClaim("expired-key", tenantId, now);
                var takeover = await new IdempotencyRepository(takeoverContext).TryClaimAsync(replacement);

                await Assert.That(takeover.IsOwner).IsTrue();
                await Assert.That(takeover.Record.Id).IsEqualTo(replacement.Id);
            }
        }
        finally
        {
            File.Delete(databasePath);
            File.Delete($"{databasePath}-shm");
            File.Delete($"{databasePath}-wal");
        }
    }

    private static async Task CreateDatabaseAsync(string databasePath)
    {
        await using var context = CreateContext(databasePath);
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE ie_idempotency_records (
                id TEXT NOT NULL PRIMARY KEY,
                key TEXT NOT NULL,
                tenant_id TEXT NOT NULL,
                user_id TEXT NULL,
                request_method TEXT NOT NULL,
                request_target TEXT NOT NULL,
                request_content_type TEXT NULL,
                request_body_hash TEXT NOT NULL,
                principal_fingerprint TEXT NOT NULL,
                status_code INTEGER NOT NULL,
                response_body TEXT NULL,
                content_type TEXT NULL,
                created_at TEXT NOT NULL,
                expires_at TEXT NOT NULL
            );
            CREATE UNIQUE INDEX ix_idempotency_records_key_tenant_id
                ON ie_idempotency_records (key, tenant_id);
            """);
        await SqliteDatabaseInitializer.InitializeAsync(context, CancellationToken.None);
    }

    private static ExploreDbContext CreateContext(string databasePath)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            DefaultTimeout = 5,
        }.ToString();
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseSqlite(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        var context = new ExploreDbContext(options);
        context.EnableTenantFilterBypass("File-backed SQLite idempotency repository regression.");
        return context;
    }

    private static IdempotencyRecord NewClaim(
        string key,
        Guid tenantId,
        DateTime createdAt,
        DateTime? expiresAt = null) => new()
        {
            Id = Guid.CreateVersion7(),
            Key = key,
            TenantId = tenantId,
            UserId = "sqlite-test-user",
            RequestMethod = "POST",
            RequestTarget = "/api/sqlite-test",
            RequestBodyHash = Guid.NewGuid().ToString("N"),
            PrincipalFingerprint = Guid.NewGuid().ToString("N"),
            StatusCode = IdempotencyRecord.InProgressStatusCode,
            CreatedAt = createdAt,
            ExpiresAt = expiresAt ?? createdAt.AddHours(1),
        };
}
