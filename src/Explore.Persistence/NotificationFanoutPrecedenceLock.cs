// ABOUTME: Centralizes the PostgreSQL advisory-lock identity for event fanout precedence decisions.
// ABOUTME: Keeps occurrence coordination and SMTP provider admission on one deadlock-safe lock boundary.

using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence;

internal static class NotificationFanoutPrecedenceLock
{
    public static async Task AcquireAsync(
        ExploreDbContext dbContext,
        Guid tenantId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        EnsureActivePostgresTransaction(dbContext);
        if (tenantId == Guid.Empty || eventId == Guid.Empty)
        {
            throw new ArgumentException("Fanout precedence locking requires non-empty tenant and event identifiers.");
        }

        string key = $"notification-fanout-precedence:{tenantId:N}:{eventId:N}";
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))",
            cancellationToken);
    }

    public static void EnsureActivePostgresTransaction(ExploreDbContext dbContext)
    {
        if (dbContext.Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL"
            || dbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "Fanout precedence operations require an active PostgreSQL unit-of-work transaction.");
        }
    }
}
