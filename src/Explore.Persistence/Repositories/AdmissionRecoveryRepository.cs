// ABOUTME: Persists tenant-scoped admission recovery capabilities and atomic one-time mutations.
// ABOUTME: Resolves verified identities without storing or returning plaintext capability material.

using Explore.Application.Contracts.Admissions;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed class AdmissionRecoveryRepository(ExploreDbContext dbContext) :
    IAdmissionRecoveryRepository
{
    public async Task<AdmissionRecoveryIdentityResult> FindIdentityAsync(
        AdmissionRecoveryRequest request,
        CancellationToken cancellationToken)
    {
        string normalizedIdentity = request.NormalizedIdentity.Trim().ToUpperInvariant();
        int activeStatus = (int)AdmissionTicketStatusEnum.Active;
        int suspendedStatus = (int)AdmissionTicketStatusEnum.Suspended;
        Guid[] ticketIds = await (
                from pii in dbContext.RegistrationOrderPii.AsNoTracking()
                join ticket in dbContext.AdmissionTickets.AsNoTracking()
                    on new { pii.TenantId, pii.RegistrationOrderId }
                    equals new { ticket.TenantId, ticket.RegistrationOrderId }
                where pii.TenantId == request.TenantId &&
                    pii.IsEmailVerified &&
                    pii.NormalizedEmail == normalizedIdentity &&
                    (ticket.AdmissionTicketStatusId == activeStatus ||
                        ticket.AdmissionTicketStatusId == suspendedStatus)
                orderby ticket.CreatedAt descending, ticket.Id
                select ticket.Id)
            .Distinct()
            .Take(1)
            .ToArrayAsync(cancellationToken);
        return new AdmissionRecoveryIdentityResult(
            request.TenantId,
            Guid.CreateVersion7(),
            ticketIds.Length > 0,
            ticketIds);
    }

    public async Task<AdmissionRecoveryMutationResult> StoreAsync(
        AdmissionRecoveryCapabilityRecord request,
        CancellationToken cancellationToken)
    {
        AdmissionRecoveryCapability entity = AdmissionRecoveryCapability.Create(
            request.CapabilityId,
            request.TenantId,
            request.RecoveryRequestId,
            request.AdmissionTicketId,
            request.Purpose.ToString(),
            request.CapabilityVersion,
            request.KeyVersion,
            request.LookupDigest,
            request.ExpiresAtUtc.UtcDateTime,
            request.CreatedAtUtc.UtcDateTime,
            request.LocatorDigest);
        await dbContext.AdmissionRecoveryCapabilities.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new AdmissionRecoveryMutationResult(AdmissionRecoveryMutationOutcome.Stored);
    }

    public async Task<AdmissionRecoveryCapabilityState> GetByLocatorAsync(
        Guid tenantId,
        IReadOnlyList<AdmissionRecoveryLocatorDigest> locators,
        CancellationToken cancellationToken)
    {
        foreach (AdmissionRecoveryLocatorDigest locator in locators)
        {
            AdmissionRecoveryCapability? entity = await dbContext.AdmissionRecoveryCapabilities
                .AsNoTracking()
                .SingleOrDefaultAsync(value =>
                    value.TenantId == tenantId &&
                    value.LookupKeyVersion == locator.KeyVersion &&
                    value.LocatorDigest == locator.LocatorDigest,
                    cancellationToken);
            if (entity is not null)
            {
                return MapState(entity);
            }
        }

        return new AdmissionRecoveryCapabilityState(
            false,
            tenantId,
            Guid.Empty,
            Guid.Empty,
            string.Empty,
            AdmissionRecoveryPurpose.TicketRecovery,
            default,
            false,
            false);
    }

    public async Task<AdmissionRecoveryCapabilityState> GetByDigestAsync(
        AdmissionRecoveryCapabilityLookup request,
        CancellationToken cancellationToken)
    {
        IQueryable<AdmissionRecoveryCapability> query = dbContext.AdmissionRecoveryCapabilities
            .AsNoTracking()
            .Where(value =>
                value.TenantId == request.TenantId &&
                value.RecoveryRequestId == request.RecoveryRequestId &&
                value.AdmissionTicketId == request.AdmissionTicketId &&
                value.Purpose == request.Purpose.ToString() &&
                value.LookupDigest == request.LookupDigest);
        if (request.KeyVersion > 0)
        {
            query = query.Where(value => value.LookupKeyVersion == request.KeyVersion);
        }

        AdmissionRecoveryCapability? entity = await query.SingleOrDefaultAsync(cancellationToken);
        return MapState(entity, request);
    }

    public async Task<AdmissionRecoveryCapabilityState> GetCurrentByRequestIdAsync(
        Guid tenantId,
        Guid recoveryRequestId,
        AdmissionRecoveryPurpose purpose,
        CancellationToken cancellationToken)
    {
        AdmissionRecoveryCapability? entity = await dbContext.AdmissionRecoveryCapabilities
            .AsNoTracking()
            .Where(value =>
                value.TenantId == tenantId &&
                value.RecoveryRequestId == recoveryRequestId &&
                value.Purpose == purpose.ToString())
            .OrderByDescending(value => value.CapabilityVersion)
            .FirstOrDefaultAsync(cancellationToken);
        return entity is null
            ? new AdmissionRecoveryCapabilityState(
                false,
                tenantId,
                recoveryRequestId,
                Guid.Empty,
                string.Empty,
                purpose,
                default,
                false,
                false)
            : MapState(entity);
    }

    public async Task<AdmissionRecoveryMutationResult> ConsumeAsync(
        AdmissionRecoveryCapabilityMutation request,
        CancellationToken cancellationToken)
    {
        DateTime occurredAt = request.OccurredAtUtc.UtcDateTime;
        Guid nextStamp = Guid.CreateVersion7();
        IQueryable<AdmissionRecoveryCapability> candidate = dbContext.AdmissionRecoveryCapabilities
            .Where(value =>
                value.TenantId == request.TenantId &&
                value.RecoveryRequestId == request.RecoveryRequestId &&
                value.AdmissionTicketId == request.AdmissionTicketId &&
                value.Purpose == request.Purpose.ToString() &&
                value.LookupDigest == request.LookupDigest &&
                value.ConsumedAt == null &&
                value.RotatedAt == null &&
                value.ExpiresAt > occurredAt);
        if (request.KeyVersion > 0)
        {
            candidate = candidate.Where(value => value.LookupKeyVersion == request.KeyVersion);
        }

        if (request.CapabilityId != Guid.Empty)
        {
            candidate = candidate.Where(value => value.Id == request.CapabilityId);
        }

        if (request.ExpectedConcurrencyStamp != Guid.Empty)
        {
            candidate = candidate.Where(value =>
                value.ConcurrencyStamp == request.ExpectedConcurrencyStamp);
        }

        int changed = await candidate.ExecuteUpdateAsync(
            setters => setters
                .SetProperty(value => value.ConsumedAt, occurredAt)
                .SetProperty(value => value.ActiveUniquenessSlot, value => value.CapabilityVersion)
                .SetProperty(value => value.UpdatedAt, occurredAt)
                .SetProperty(value => value.ConcurrencyStamp, nextStamp),
            cancellationToken);
        return new AdmissionRecoveryMutationResult(
            changed == 1
                ? AdmissionRecoveryMutationOutcome.Consumed
                : AdmissionRecoveryMutationOutcome.Rejected);
    }

    public async Task<AdmissionRecoveryMutationResult> RotateAsync(
        AdmissionRecoveryRotationRequest request,
        CancellationToken cancellationToken)
    {
        DateTime rotatedAt = request.RotatedAtUtc.UtcDateTime;
        IQueryable<AdmissionRecoveryCapability> candidate = dbContext.AdmissionRecoveryCapabilities
            .Where(value =>
                value.TenantId == request.TenantId &&
                value.RecoveryRequestId == request.RecoveryRequestId &&
                value.AdmissionTicketId == request.AdmissionTicketId &&
                value.Purpose == request.Purpose.ToString() &&
                value.LookupDigest == request.OldLookupDigest &&
                value.ConsumedAt == null &&
                value.RotatedAt == null);
        if (request.OldKeyVersion > 0)
        {
            candidate = candidate.Where(value => value.LookupKeyVersion == request.OldKeyVersion);
        }

        if (request.OldCapabilityId != Guid.Empty)
        {
            candidate = candidate.Where(value => value.Id == request.OldCapabilityId);
        }

        if (request.ExpectedConcurrencyStamp != Guid.Empty)
        {
            candidate = candidate.Where(value =>
                value.ConcurrencyStamp == request.ExpectedConcurrencyStamp);
        }

        int changed = await candidate.ExecuteUpdateAsync(
            setters => setters
                .SetProperty(value => value.RotatedAt, rotatedAt)
                .SetProperty(value => value.ActiveUniquenessSlot, value => value.CapabilityVersion)
                .SetProperty(value => value.UpdatedAt, rotatedAt)
                .SetProperty(value => value.ConcurrencyStamp, Guid.CreateVersion7()),
            cancellationToken);
        if (changed != 1)
        {
            return new AdmissionRecoveryMutationResult(AdmissionRecoveryMutationOutcome.Rejected);
        }

        AdmissionRecoveryCapability replacement = AdmissionRecoveryCapability.Create(
            request.ReplacementCapabilityId,
            request.TenantId,
            request.RecoveryRequestId,
            request.AdmissionTicketId,
            request.Purpose.ToString(),
            request.ReplacementCapabilityVersion,
            request.ReplacementKeyVersion,
            request.ReplacementLookupDigest,
            request.ReplacementExpiresAtUtc.UtcDateTime,
            rotatedAt,
            request.ReplacementLocatorDigest);
        await dbContext.AdmissionRecoveryCapabilities.AddAsync(replacement, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new AdmissionRecoveryMutationResult(AdmissionRecoveryMutationOutcome.Rotated);
    }

    private static AdmissionRecoveryCapabilityState MapState(
        AdmissionRecoveryCapability? entity,
        AdmissionRecoveryCapabilityLookup request) =>
        entity is null
            ? new AdmissionRecoveryCapabilityState(
                false,
                request.TenantId,
                request.RecoveryRequestId,
                request.AdmissionTicketId,
                request.LookupDigest,
                request.Purpose,
                default,
                false,
                false,
                request.KeyVersion)
            : MapState(entity);

    private static AdmissionRecoveryCapabilityState MapState(AdmissionRecoveryCapability entity) =>
        new(
            true,
            entity.TenantId,
            entity.RecoveryRequestId,
            entity.AdmissionTicketId,
            entity.LookupDigest,
            Enum.Parse<AdmissionRecoveryPurpose>(entity.Purpose),
            new DateTimeOffset(entity.ExpiresAt),
            entity.ConsumedAt.HasValue,
            entity.RotatedAt.HasValue,
            entity.LookupKeyVersion,
            entity.Id,
            entity.CapabilityVersion,
            entity.ConcurrencyStamp);
}
