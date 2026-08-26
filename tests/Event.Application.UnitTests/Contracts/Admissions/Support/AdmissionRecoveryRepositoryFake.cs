// ABOUTME: Implements entity-returning recovery repository and identity resolver test ports.
// ABOUTME: Preserves digest-only state while exercising atomic consume and rotation semantics.

using Explore.Application.Contracts.Admissions;
using Explore.Domain;

namespace ApplicationUnitTests.Contracts.Admissions.Support;

internal sealed class RecoveryIdentityResolverFake(AdmissionTestScenario scenario) :
    IAdmissionRecoveryIdentityResolver
{
    internal const string PortName = nameof(IAdmissionRecoveryIdentityResolver);

    public Task<AdmissionRecoveryIdentityResult> FindAsync(
        AdmissionRecoveryRequest request,
        CancellationToken cancellationToken)
    {
        if (request.TenantId != scenario.TenantId)
        {
            throw AdmissionContractRuntime.Missing("matching recovery identity tenant");
        }

        return Task.FromResult(new AdmissionRecoveryIdentityResult(
            scenario.TenantId,
            scenario.RecoveryRequestId,
            scenario.IdentityPresent,
            scenario.TicketsByAssignment.Values
                .Select(AdmissionContractRuntime.EntityId)
                .ToArray()));
    }
}

internal sealed class RecoveryRepositoryFake(AdmissionTestScenario scenario) :
    IAdmissionRecoveryRepository
{
    internal const string PortName = nameof(IAdmissionRecoveryRepository);
    private readonly Dictionary<string, AdmissionRecoveryCapability> entities =
        new(StringComparer.Ordinal);

    public Task<AdmissionRecoveryCapability> AddAsync(
        AdmissionRecoveryCapability capability,
        CancellationToken cancellationToken)
    {
        if (!scenario.IdentityPresent || entities.ContainsKey(capability.LookupDigest))
        {
            throw AdmissionContractRuntime.Missing("new recovery entity after present identity");
        }

        entities.Add(capability.LookupDigest, capability);
        scenario.RecoveryByDigest.Add(capability.LookupDigest, Stored(capability));
        scenario.RecoveryStoreCalls++;
        return Task.FromResult(capability);
    }

    public Task<AdmissionRecoveryCapability?> FindByProofDigestAsync(
        Guid tenantId,
        Guid recoveryRequestId,
        Guid admissionTicketId,
        AdmissionRecoveryPurpose purpose,
        int keyVersion,
        string lookupDigest,
        CancellationToken cancellationToken)
    {
        entities.TryGetValue(lookupDigest, out AdmissionRecoveryCapability? entity);
        return Task.FromResult(entity is not null &&
            entity.TenantId == tenantId &&
            entity.RecoveryRequestId == recoveryRequestId &&
            entity.AdmissionTicketId == admissionTicketId &&
            entity.Purpose == purpose.ToString() &&
            entity.LookupKeyVersion == keyVersion
                ? entity
                : null);
    }

    public Task<AdmissionRecoveryCapability?> FindByLocatorAsync(
        Guid tenantId,
        IReadOnlyList<AdmissionRecoveryLocatorDigest> locators,
        CancellationToken cancellationToken)
    {
        AdmissionRecoveryCapability? entity = entities.Values.SingleOrDefault(candidate =>
            candidate.TenantId == tenantId &&
            locators.Any(locator =>
                locator.KeyVersion == candidate.LookupKeyVersion &&
                locator.LocatorDigest == candidate.LocatorDigest));
        return Task.FromResult(entity);
    }

    public Task<AdmissionRecoveryCapability?> FindLatestByRequestIdAsync(
        Guid tenantId,
        Guid recoveryRequestId,
        AdmissionRecoveryPurpose purpose,
        CancellationToken cancellationToken)
    {
        scenario.RecoveryCurrentReadCalls++;
        return Task.FromResult(entities.Values
            .Where(entity =>
                entity.TenantId == tenantId &&
                entity.RecoveryRequestId == recoveryRequestId &&
                entity.Purpose == purpose.ToString())
            .OrderByDescending(entity => entity.CapabilityVersion)
            .FirstOrDefault());
    }

    public Task<AdmissionRecoveryCapability?> FindLatestByTicketIdAsync(
        Guid tenantId,
        Guid admissionTicketId,
        AdmissionRecoveryPurpose purpose,
        CancellationToken cancellationToken) =>
        Task.FromResult(entities.Values
            .Where(entity =>
                entity.TenantId == tenantId &&
                entity.AdmissionTicketId == admissionTicketId &&
                entity.Purpose == purpose.ToString())
            .OrderByDescending(entity => entity.CapabilityVersion)
            .FirstOrDefault());

    public Task<bool> TryConsumeAsync(
        Guid tenantId,
        Guid capabilityId,
        int keyVersion,
        string lookupDigest,
        Guid expectedConcurrencyStamp,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken)
    {
        if (!entities.TryGetValue(lookupDigest, out AdmissionRecoveryCapability? entity) ||
            entity.TenantId != tenantId ||
            entity.Id != capabilityId ||
            entity.LookupKeyVersion != keyVersion ||
            entity.ConcurrencyStamp != expectedConcurrencyStamp)
        {
            return Task.FromResult(false);
        }

        AdmissionRecoveryTransitionOutcome outcome = entity.TryConsume(occurredAtUtc);
        if (outcome != AdmissionRecoveryTransitionOutcome.Consumed)
        {
            return Task.FromResult(false);
        }

        scenario.RecoveryByDigest[lookupDigest] =
            scenario.RecoveryByDigest[lookupDigest] with { Consumed = true };
        return Task.FromResult(true);
    }

    public Task<bool> TryRotateAsync(
        AdmissionRecoveryCapability current,
        AdmissionRecoveryCapability replacement,
        DateTime rotatedAtUtc,
        CancellationToken cancellationToken)
    {
        if (!entities.TryGetValue(current.LookupDigest, out AdmissionRecoveryCapability? stored) ||
            stored.ConcurrencyStamp != current.ConcurrencyStamp ||
            stored.ConsumedAt.HasValue ||
            stored.RotatedAt.HasValue)
        {
            return Task.FromResult(false);
        }

        if (stored.TryRotate(rotatedAtUtc) != AdmissionRecoveryTransitionOutcome.Rotated)
        {
            return Task.FromResult(false);
        }

        scenario.RecoveryByDigest[current.LookupDigest] =
            scenario.RecoveryByDigest[current.LookupDigest] with { Rotated = true };
        entities.Add(replacement.LookupDigest, replacement);
        scenario.RecoveryByDigest.Add(replacement.LookupDigest, Stored(replacement));
        scenario.RecoveryRotationCalls++;
        return Task.FromResult(true);
    }

    private static StoredRecoveryCapability Stored(AdmissionRecoveryCapability entity) =>
        new(
            entity.TenantId,
            entity.RecoveryRequestId,
            entity.AdmissionTicketId,
            entity.LookupDigest,
            entity.Purpose,
            new DateTimeOffset(entity.ExpiresAt),
            entity.ConsumedAt.HasValue,
            entity.RotatedAt.HasValue);
}
