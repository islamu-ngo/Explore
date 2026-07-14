// ABOUTME: EF Core repository for authoritative provider-publication aggregates and atomic worker claims.
// ABOUTME: Enforces explicit tenant predicates, entity-returning leases, append-only evidence, and fenced updates.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Explore.Persistence.Repositories;

public sealed class WebhookProviderPublicationRepository : IWebhookProviderPublicationRepository
{
    private const int MaximumBatchSize = 1000;
    private const string UniqueViolationSqlState = "23505";
    private const string AttemptIdentityConstraintPrefix =
        "ux_webhook_provider_publication_attempts_tenant_publication_att";
    private readonly ExploreDbContext _dbContext;

    public WebhookProviderPublicationRepository(ExploreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WebhookProviderPublication> CreateAsync(
        WebhookProviderPublication publication,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(publication);
        await _dbContext.WebhookProviderPublications.AddAsync(publication, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return publication;
    }

    public Task<WebhookProviderPublication?> GetByIdentityAsync(
        Guid tenantId,
        Guid webhookMessageId,
        WebhookProviderKind providerKind,
        Guid providerBindingId,
        CancellationToken cancellationToken) =>
        _dbContext.WebhookProviderPublications
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .Include(publication => publication.Attempts)
            .FirstOrDefaultAsync(
                publication =>
                    publication.TenantId == tenantId &&
                    publication.WebhookMessageId == webhookMessageId &&
                    publication.ProviderKindId == (int)providerKind &&
                    publication.ProviderBindingId == providerBindingId,
                cancellationToken);

    public Task<WebhookProviderPublication?> GetByTenantAndIdAsync(
        Guid tenantId,
        Guid publicationId,
        CancellationToken cancellationToken) =>
        _dbContext.WebhookProviderPublications
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .Include(publication => publication.Attempts)
            .FirstOrDefaultAsync(
                publication => publication.TenantId == tenantId && publication.Id == publicationId,
                cancellationToken);

    public Task<IReadOnlyList<WebhookProviderPublicationClaim>> ClaimDueAsync(
        WebhookProviderPublicationClaimRequest request,
        CancellationToken cancellationToken) =>
        ClaimAsync(request, reconciliation: false, cancellationToken);

    public Task<IReadOnlyList<WebhookProviderPublicationClaim>> ClaimUnknownAsync(
        WebhookProviderPublicationClaimRequest request,
        CancellationToken cancellationToken) =>
        ClaimAsync(request, reconciliation: true, cancellationToken);

    public async Task<IReadOnlyList<WebhookProviderPublication>> GetUnknownRequiringManualAsync(
        DateTime observedAt,
        int batchSize,
        int maxAutomaticReconciliationAttempts,
        CancellationToken cancellationToken)
    {
        if (observedAt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Observation time must use UTC kind.", nameof(observedAt));
        }

        if (batchSize is < 1 or > MaximumBatchSize || maxAutomaticReconciliationAttempts < 1)
        {
            return [];
        }

        return await _dbContext.WebhookProviderPublications
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookWorkerCrossTenantQueue)
            .Include(publication => publication.Attempts)
            .Where(publication =>
                publication.StatusId == (int)WebhookProviderPublicationStatus.PublicationUnknown &&
                (publication.ProcessingLeaseExpiresAt == null ||
                 publication.ProcessingLeaseExpiresAt <= observedAt) &&
                (publication.IdempotencyValidUntil <= observedAt ||
                 publication.AutomaticReconciliationAttemptCount >= maxAutomaticReconciliationAttempts))
            .OrderBy(publication => publication.IdempotencyValidUntil)
            .ThenBy(publication => publication.PreparedAt)
            .ThenBy(publication => publication.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public Task<WebhookProviderPublication?> GetActiveClaimAsync(
        Guid tenantId,
        Guid publicationId,
        Guid leaseToken,
        long publicationFence,
        DateTime observedAt,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty ||
            publicationId == Guid.Empty ||
            leaseToken == Guid.Empty ||
            publicationFence < 1 ||
            observedAt.Kind != DateTimeKind.Utc)
        {
            return Task.FromResult<WebhookProviderPublication?>(null);
        }

        return _dbContext.WebhookProviderPublications
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .Include(publication => publication.Attempts)
            .FirstOrDefaultAsync(
                publication =>
                    publication.TenantId == tenantId &&
                    publication.Id == publicationId &&
                    (publication.StatusId == (int)WebhookProviderPublicationStatus.Publishing ||
                     publication.StatusId == (int)WebhookProviderPublicationStatus.PublicationUnknown) &&
                    publication.ProcessingLeaseToken == leaseToken &&
                    publication.PublicationFence == publicationFence &&
                    publication.ProcessingLeaseExpiresAt > observedAt,
                cancellationToken);
    }

    public async Task<WebhookProviderPublication> UpdateAsync(
        WebhookProviderPublication publication,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(publication);
        if (_dbContext.Entry(publication).State == EntityState.Detached)
        {
            throw new InvalidOperationException(
                "Provider publications must be loaded and updated through the same scoped repository.");
        }

        await TrackAppendedAttemptsAsync(publication, cancellationToken);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: UniqueViolationSqlState,
                ConstraintName: { } constraintName
            } &&
            constraintName.StartsWith(AttemptIdentityConstraintPrefix, StringComparison.Ordinal))
        {
            throw new WebhookProviderPublicationConcurrencyException(
                "The provider publication was completed by another worker.",
                exception);
        }

        return publication;
    }

    private async Task<IReadOnlyList<WebhookProviderPublicationClaim>> ClaimAsync(
        WebhookProviderPublicationClaimRequest request,
        bool reconciliation,
        CancellationToken cancellationToken)
    {
        if (request.BatchSize is < 1 or > MaximumBatchSize ||
            request.LeaseDuration <= TimeSpan.Zero ||
            request.MaxAutomaticAttempts < 1)
        {
            return [];
        }

        if (request.ClaimedAt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Claim time must use UTC kind.", nameof(request));
        }

        var leaseOwner = NormalizeLeaseOwner(request.LeaseOwner);
        var leaseExpiresAt = request.ClaimedAt.Add(request.LeaseDuration);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (_dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            await _dbContext.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_xact_lock(hashtext({0}))",
                ["webhook-provider-publication-claim"],
                cancellationToken);
        }

        var query = _dbContext.WebhookProviderPublications
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookWorkerCrossTenantQueue)
            .Include(publication => publication.Attempts)
            .Where(publication =>
                publication.IdempotencyValidUntil > request.ClaimedAt &&
                (publication.NextActionAt == null || publication.NextActionAt <= request.ClaimedAt) &&
                (publication.ProcessingLeaseExpiresAt == null ||
                 publication.ProcessingLeaseExpiresAt <= request.ClaimedAt));

        query = reconciliation
            ? query.Where(publication =>
                publication.StatusId == (int)WebhookProviderPublicationStatus.PublicationUnknown &&
                publication.AutomaticReconciliationAttemptCount < request.MaxAutomaticAttempts)
            : query.Where(publication =>
                (publication.StatusId == (int)WebhookProviderPublicationStatus.Prepared ||
                 publication.StatusId == (int)WebhookProviderPublicationStatus.RetryDue) &&
                publication.AutomaticPublicationAttemptCount < request.MaxAutomaticAttempts);

        var publications = await query
            .OrderBy(publication => publication.NextActionAt ?? publication.PreparedAt)
            .ThenBy(publication => publication.PreparedAt)
            .ThenBy(publication => publication.Id)
            .Take(request.BatchSize)
            .ToListAsync(cancellationToken);

        var claims = new List<WebhookProviderPublicationClaim>(publications.Count);
        foreach (var publication in publications)
        {
            var persistedAttemptIds = publication.Attempts
                .Select(attempt => attempt.Id)
                .ToHashSet();
            var leaseToken = Guid.CreateVersion7();
            if (reconciliation)
            {
                publication.ClaimForAutomaticReconciliation(
                    leaseOwner,
                    leaseToken,
                    leaseExpiresAt,
                    request.ClaimedAt,
                    request.MaxAutomaticAttempts);
            }
            else
            {
                publication.ClaimForPublishing(
                    leaseOwner,
                    leaseToken,
                    leaseExpiresAt,
                    request.ClaimedAt,
                    request.MaxAutomaticAttempts);
            }

            TrackNewAttempts(publication, persistedAttemptIds);
            claims.Add(new WebhookProviderPublicationClaim(
                publication,
                leaseToken,
                publication.PublicationFence,
                request.ClaimedAt,
                leaseExpiresAt));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return claims;
    }

    private async Task TrackAppendedAttemptsAsync(
        WebhookProviderPublication publication,
        CancellationToken cancellationToken)
    {
        var attemptIds = publication.Attempts.Select(attempt => attempt.Id).ToArray();
        if (attemptIds.Length == 0)
        {
            return;
        }

        var persistedAttemptIds = await _dbContext.WebhookProviderPublicationAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .AsNoTracking()
            .Where(attempt =>
                attempt.TenantId == publication.TenantId &&
                attempt.WebhookProviderPublicationId == publication.Id &&
                attemptIds.Contains(attempt.Id))
            .Select(attempt => attempt.Id)
            .ToHashSetAsync(cancellationToken);
        TrackNewAttempts(publication, persistedAttemptIds);
    }

    private void TrackNewAttempts(
        WebhookProviderPublication publication,
        IReadOnlySet<Guid> persistedAttemptIds)
    {
        var automaticChangeDetection = _dbContext.ChangeTracker.AutoDetectChangesEnabled;
        try
        {
            _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
            foreach (var attempt in publication.Attempts.Where(attempt => !persistedAttemptIds.Contains(attempt.Id)))
            {
                _dbContext.Entry(attempt).State = EntityState.Added;
            }
        }
        finally
        {
            _dbContext.ChangeTracker.AutoDetectChangesEnabled = automaticChangeDetection;
        }
    }

    private static string NormalizeLeaseOwner(string leaseOwner)
    {
        if (string.IsNullOrWhiteSpace(leaseOwner))
        {
            throw new ArgumentException("Lease owner is required.", nameof(leaseOwner));
        }

        var normalized = leaseOwner.Trim();
        if (normalized.Length > WebhookProviderPublication.MaxLeaseOwnerLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(leaseOwner),
                $"Lease owner cannot exceed {WebhookProviderPublication.MaxLeaseOwnerLength} characters.");
        }

        return normalized;
    }
}
