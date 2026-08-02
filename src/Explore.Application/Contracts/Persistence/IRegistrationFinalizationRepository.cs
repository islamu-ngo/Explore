// ABOUTME: Persistence contract for requirement evidence and fenced registration-finalization effects.
// ABOUTME: Keeps tenant-scoped deduplication and worker claims behind the Application boundary.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public sealed record RegistrationFinalizationClaim(
    Guid EffectId,
    Guid TenantId,
    Guid RegistrationOrderId,
    Guid LeaseToken,
    long ProcessingFence);

public interface IRegistrationFinalizationRepository
{
    Task<bool> RecordFulfillmentAsync(
        RegistrationRequirementFulfillment fulfillment,
        DateTime recordedAt,
        CancellationToken cancellationToken);

    Task<bool> TryRecordSkippedFulfillmentsAndConsumeAttemptAsync(
        RegistrationAttempt attempt,
        Guid expectedAttemptConcurrencyStamp,
        IReadOnlyCollection<RegistrationRequirementFulfillment> fulfillments,
        DateTime recordedAt,
        CancellationToken cancellationToken);

    Task<bool> AreMandatoryRequirementsFulfilledAsync(
        Guid tenantId,
        Guid registrationOrderId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RegistrationRequirementFulfillment>> GetFulfillmentsAsync(
        Guid tenantId,
        Guid registrationOrderId,
        Guid registrationRequirementId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RegistrationFinalizationClaim>> ClaimDueAsync(
        string leaseOwner,
        int batchSize,
        DateTime claimedAt,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    Task<bool> CompleteAsync(
        RegistrationFinalizationClaim claim,
        DateTime completedAt,
        CancellationToken cancellationToken);

    Task<bool> RetryAsync(
        RegistrationFinalizationClaim claim,
        DateTime nextAttemptAt,
        DateTime failedAt,
        CancellationToken cancellationToken);
}
