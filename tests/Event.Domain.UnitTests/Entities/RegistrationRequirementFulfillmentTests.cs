// ABOUTME: Covers durable registration requirement fulfillment and fenced finalization domain invariants.
// ABOUTME: Verifies optional skips remain auditable while mandatory skips and stale effect claims fail closed.

using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.ValueObjects;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Domain.UnitTests.Entities;

public sealed class RegistrationRequirementFulfillmentTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task OptionalRequirementCanBeSkippedButRequiredRequirementCannot()
    {
        (RegistrationOrder order, RegistrationWorkflow workflow) = CreateOrder();
        RegistrationRequirement optional = RegistrationRequirement.Create(
            workflow, 1, RegistrationRequirementCriticalityEnum.Optional, true,
            RegistrationRequirementCompletionEffectEnum.EnrichesRegistration,
            RegistrationAnswerSyncModeEnum.FULL_CANONICAL,
            RegistrationRequirementSubjectTypeEnum.AllOrders, null, UtcNow);
        RegistrationRequirement required = RegistrationRequirement.Create(
            workflow, 2, RegistrationRequirementCriticalityEnum.Required, false,
            RegistrationRequirementCompletionEffectEnum.BlocksRegistration,
            RegistrationAnswerSyncModeEnum.FULL_CANONICAL,
            RegistrationRequirementSubjectTypeEnum.AllOrders, null, UtcNow);

        RegistrationRequirementFulfillment skipped = RegistrationRequirementFulfillment.CreateSkipped(
            order, optional, RegistrationAnswerSubjectTypeEnum.RegistrationOrder, order.Id, UtcNow);

        await Assert.That(skipped.IsSkipped).IsTrue();
        await Assert.That(skipped.SourceRegistrationSubmissionId).IsNull();
        await Assert.That(() => RegistrationRequirementFulfillment.CreateSkipped(
            order, required, RegistrationAnswerSubjectTypeEnum.RegistrationOrder, order.Id, UtcNow))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task EffectClaimUsesMonotonicFenceWhenExpiredLeaseIsRecovered()
    {
        (RegistrationOrder order, _) = CreateOrder();
        RegistrationFinalizationEffect effect = RegistrationFinalizationEffect.Create(order, UtcNow);
        Guid firstLease = Guid.CreateVersion7();
        effect.Claim("worker-a", firstLease, UtcNow.AddSeconds(10), UtcNow);
        effect.RecoverExpiredClaim(UtcNow.AddSeconds(10));
        effect.Claim("worker-b", Guid.CreateVersion7(), UtcNow.AddMinutes(1), UtcNow.AddSeconds(10));

        await Assert.That(effect.AttemptCount).IsEqualTo(2);
        await Assert.That(effect.ProcessingFence).IsEqualTo(2);
        await Assert.That(effect.ProcessingLeaseToken).IsNotEqualTo(firstLease);
    }

    private static (RegistrationOrder Order, RegistrationWorkflow Workflow) CreateOrder()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        RegistrationWorkflow workflow = RegistrationWorkflow.Create(tenantId, eventId, "REGISTRATION", UtcNow);
        RegistrationOrder order = RegistrationOrder.Create(
            tenantId, eventId, Guid.CreateVersion7(), null, BookingPartyTypeEnum.Individual,
            Guid.CreateVersion7(), RegistrationParticipationSnapshot.Create(
                Guid.CreateVersion7(), 4, 3, 2, GuestRecoveryPolicyEnum.VerifiedEmailRequired),
            workflow.Id, null, "EUR", UtcNow, UtcNow.AddMinutes(15));
        return (order, workflow);
    }
}
