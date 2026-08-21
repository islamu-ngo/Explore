// ABOUTME: Covers the shared native/provider requirement-fulfillment Application command boundary.
// ABOUTME: Verifies optional skips and reconciled payment success share one fenced finalization drain.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.RegistrationOrders;
using Explore.Application.Features.RegistrationSubmissions.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.ValueObjects;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.RegistrationSubmissions;

public sealed class RegistrationRequirementFulfillmentCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task OptionalSkipUsesSharedFinalizationRepositoryWithoutSubmissionLookup()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        RegistrationWorkflow workflow = RegistrationWorkflow.Create(tenantId, eventId, "REGISTRATION", UtcNow);
        RegistrationRequirement requirement = RegistrationRequirement.Create(
            workflow, 1, RegistrationRequirementCriticalityEnum.Optional, true,
            RegistrationRequirementCompletionEffectEnum.EnrichesRegistration,
            RegistrationAnswerSyncModeEnum.FULL_CANONICAL,
            RegistrationRequirementSubjectTypeEnum.AllOrders, null, UtcNow);
        RegistrationOrder order = RegistrationOrder.Create(
            tenantId, eventId, Guid.CreateVersion7(), null, BookingPartyTypeEnum.Individual,
            Guid.CreateVersion7(), RegistrationParticipationSnapshot.Create(
                Guid.CreateVersion7(), 4, 3, 2, GuestRecoveryPolicyEnum.VerifiedEmailRequired),
            workflow.Id, null, "EUR", UtcNow, UtcNow.AddMinutes(15));
        IRegistrationInventoryRepository inventory = Substitute.For<IRegistrationInventoryRepository>();
        IRegistrationSubmissionRepository submissions = Substitute.For<IRegistrationSubmissionRepository>();
        IRegistrationFinalizationRepository finalization = Substitute.For<IRegistrationFinalizationRepository>();
        inventory.GetOrderByIdAsync(order.Id, tenantId, Arg.Any<CancellationToken>()).Returns(order);
        submissions.GetRequirementAsync(tenantId, requirement.Id, Arg.Any<CancellationToken>()).Returns(requirement);
        finalization.RecordFulfillmentAsync(
                Arg.Any<RegistrationRequirementFulfillment>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);

        bool ready = await new RecordRegistrationRequirementFulfillmentCommandHandler(
            inventory, submissions, finalization, new FixedTimeProvider(UtcNow)).Handle(
            new(tenantId, order.Id, requirement.Id, null,
                RegistrationAnswerSubjectTypeEnum.RegistrationOrder, order.Id, true),
            CancellationToken.None);

        await Assert.That(ready).IsTrue();
        await submissions.DidNotReceive().GetSubmissionAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await finalization.Received(1).RecordFulfillmentAsync(
            Arg.Is<RegistrationRequirementFulfillment>(value => value.IsSkipped && value.RegistrationOrderId == order.Id),
            UtcNow, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DrainBindsClaimTenantAndSettlesOnlyAfterCheckoutTransition()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid orderId = Guid.CreateVersion7();
        RegistrationFinalizationClaim claim = new(
            Guid.CreateVersion7(), tenantId, orderId, Guid.CreateVersion7(), 3);
        IRegistrationFinalizationRepository finalization = Substitute.For<IRegistrationFinalizationRepository>();
        IRegistrationOrderLifecycleService lifecycle = Substitute.For<IRegistrationOrderLifecycleService>();
        ITenantContextAccessor tenantAccessor = Substitute.For<ITenantContextAccessor>();
        finalization.ClaimDueAsync(
                "worker", 100, UtcNow, TimeSpan.FromSeconds(60), CancellationToken.None)
            .Returns([claim]);
        lifecycle.ReadyForCheckoutAsync(orderId, tenantId, CancellationToken.None)
            .Returns(new RegistrationOrderLifecycleResponseDto
            {
                Success = true,
                Order = new RegistrationOrderDto
                {
                    Id = orderId,
                    TenantId = tenantId,
                    StatusId = (int)RegistrationOrderStatusEnum.ReadyForCheckout
                }
            });
        finalization.CompleteAsync(claim, UtcNow, CancellationToken.None).Returns(true);
        var handler = new DrainRegistrationFinalizationEffectsCommandHandler(
            finalization, lifecycle, tenantAccessor, new FixedTimeProvider(UtcNow));

        int completed = await handler.Handle(new("worker"), CancellationToken.None);

        await Assert.That(completed).IsEqualTo(1);
        tenantAccessor.Received(1).SetTenant(tenantId);
        tenantAccessor.Received(1).Clear();
        await finalization.Received(1).CompleteAsync(claim, UtcNow, CancellationToken.None);
        await finalization.DidNotReceive().RetryAsync(
            Arg.Any<RegistrationFinalizationClaim>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Drain_WhenCheckoutNotConverged_RetriesAndClearsTenantForInterruptionRecovery()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid orderId = Guid.CreateVersion7();
        RegistrationFinalizationClaim claim = new(
            Guid.CreateVersion7(), tenantId, orderId, Guid.CreateVersion7(), 7);
        IRegistrationFinalizationRepository finalization = Substitute.For<IRegistrationFinalizationRepository>();
        IRegistrationOrderLifecycleService lifecycle = Substitute.For<IRegistrationOrderLifecycleService>();
        ITenantContextAccessor tenantAccessor = Substitute.For<ITenantContextAccessor>();
        finalization.ClaimDueAsync(
                "worker", 100, UtcNow, TimeSpan.FromSeconds(60), CancellationToken.None)
            .Returns([claim]);
        lifecycle.ReadyForCheckoutAsync(orderId, tenantId, CancellationToken.None)
            .Returns(new RegistrationOrderLifecycleResponseDto
            {
                Success = true,
                Order = new RegistrationOrderDto
                {
                    Id = orderId,
                    TenantId = tenantId,
                    StatusId = (int)RegistrationOrderStatusEnum.AwaitingRequirements
                }
            });
        var handler = new DrainRegistrationFinalizationEffectsCommandHandler(
            finalization, lifecycle, tenantAccessor, new FixedTimeProvider(UtcNow));

        int completed = await handler.Handle(new("worker"), CancellationToken.None);

        await Assert.That(completed).IsEqualTo(0);
        tenantAccessor.Received(1).SetTenant(tenantId);
        tenantAccessor.Received(1).Clear();
        await finalization.Received(1).RetryAsync(
            claim, UtcNow.AddMinutes(1), UtcNow, CancellationToken.None);
        await finalization.DidNotReceive().CompleteAsync(
            Arg.Any<RegistrationFinalizationClaim>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DrainWhenCheckoutRoutesToPaymentFinalizesPaidBeforeSettlingClaim()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid orderId = Guid.CreateVersion7();
        RegistrationFinalizationClaim claim = new(
            Guid.CreateVersion7(), tenantId, orderId, Guid.CreateVersion7(), 11);
        IRegistrationFinalizationRepository finalization = Substitute.For<IRegistrationFinalizationRepository>();
        IRegistrationOrderLifecycleService lifecycle = Substitute.For<IRegistrationOrderLifecycleService>();
        ITenantContextAccessor tenantAccessor = Substitute.For<ITenantContextAccessor>();
        finalization.ClaimDueAsync("worker", 100, UtcNow, TimeSpan.FromSeconds(60), CancellationToken.None)
            .Returns([claim]);
        lifecycle.ReadyForCheckoutAsync(orderId, tenantId, CancellationToken.None)
            .Returns(Response(orderId, tenantId, RegistrationOrderStatusEnum.AwaitingPayment));
        lifecycle.FinalizePaidAsync(orderId, tenantId, CancellationToken.None)
            .Returns(Response(orderId, tenantId, RegistrationOrderStatusEnum.Confirmed));
        finalization.CompleteAsync(claim, UtcNow, CancellationToken.None).Returns(true);
        var handler = new DrainRegistrationFinalizationEffectsCommandHandler(
            finalization, lifecycle, tenantAccessor, new FixedTimeProvider(UtcNow));

        int completed = await handler.Handle(new("worker"), CancellationToken.None);

        await Assert.That(completed).IsEqualTo(1);
        await lifecycle.Received(1).FinalizePaidAsync(orderId, tenantId, CancellationToken.None);
        await finalization.Received(1).CompleteAsync(claim, UtcNow, CancellationToken.None);
    }

    [Test]
    public async Task DrainWhenPaidOrderIsParkedRetriesPaidFinalizationBeforeSettling()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid orderId = Guid.CreateVersion7();
        RegistrationFinalizationClaim claim = new(
            Guid.CreateVersion7(), tenantId, orderId, Guid.CreateVersion7(), 12);
        IRegistrationFinalizationRepository finalization = Substitute.For<IRegistrationFinalizationRepository>();
        IRegistrationOrderLifecycleService lifecycle = Substitute.For<IRegistrationOrderLifecycleService>();
        ITenantContextAccessor tenantAccessor = Substitute.For<ITenantContextAccessor>();
        finalization.ClaimDueAsync("worker", 100, UtcNow, TimeSpan.FromSeconds(60), CancellationToken.None)
            .Returns([claim]);
        lifecycle.ReadyForCheckoutAsync(orderId, tenantId, CancellationToken.None)
            .Returns(Response(orderId, tenantId, RegistrationOrderStatusEnum.NeedsReconciliation));
        lifecycle.FinalizePaidAsync(orderId, tenantId, CancellationToken.None)
            .Returns(Response(orderId, tenantId, RegistrationOrderStatusEnum.Confirmed));
        finalization.CompleteAsync(claim, UtcNow, CancellationToken.None).Returns(true);
        var handler = new DrainRegistrationFinalizationEffectsCommandHandler(
            finalization, lifecycle, tenantAccessor, new FixedTimeProvider(UtcNow));

        int completed = await handler.Handle(new("worker"), CancellationToken.None);

        await Assert.That(completed).IsEqualTo(1);
        await lifecycle.Received(1).FinalizePaidAsync(orderId, tenantId, CancellationToken.None);
        await finalization.Received(1).CompleteAsync(claim, UtcNow, CancellationToken.None);
    }

    [Test]
    public async Task DrainDuplicatePaidOrderParksAndContinuesToConfirmNextValidClaim()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid duplicateOrderId = Guid.CreateVersion7();
        Guid validOrderId = Guid.CreateVersion7();
        RegistrationFinalizationClaim duplicateClaim = new(
            Guid.CreateVersion7(), tenantId, duplicateOrderId, Guid.CreateVersion7(), 20);
        RegistrationFinalizationClaim validClaim = new(
            Guid.CreateVersion7(), tenantId, validOrderId, Guid.CreateVersion7(), 21);
        IRegistrationFinalizationRepository finalization = Substitute.For<IRegistrationFinalizationRepository>();
        IRegistrationOrderLifecycleService lifecycle = Substitute.For<IRegistrationOrderLifecycleService>();
        ITenantContextAccessor tenantAccessor = Substitute.For<ITenantContextAccessor>();
        finalization.ClaimDueAsync("worker", 100, UtcNow, TimeSpan.FromSeconds(60), CancellationToken.None)
            .Returns([duplicateClaim, validClaim]);
        lifecycle.ReadyForCheckoutAsync(duplicateOrderId, tenantId, CancellationToken.None)
            .Returns(Response(duplicateOrderId, tenantId, RegistrationOrderStatusEnum.NeedsReconciliation));
        lifecycle.FinalizePaidAsync(duplicateOrderId, tenantId, CancellationToken.None)
            .Returns(new RegistrationOrderLifecycleResponseDto
            {
                Success = true,
                Message = "payment_duplicate_succeeded_observations",
                Order = new RegistrationOrderDto
                {
                    Id = duplicateOrderId,
                    TenantId = tenantId,
                    StatusId = (int)RegistrationOrderStatusEnum.NeedsReconciliation
                }
            });
        lifecycle.ReadyForCheckoutAsync(validOrderId, tenantId, CancellationToken.None)
            .Returns(Response(validOrderId, tenantId, RegistrationOrderStatusEnum.AwaitingPayment));
        lifecycle.FinalizePaidAsync(validOrderId, tenantId, CancellationToken.None)
            .Returns(Response(validOrderId, tenantId, RegistrationOrderStatusEnum.Confirmed));
        finalization.CompleteAsync(Arg.Any<RegistrationFinalizationClaim>(), UtcNow, CancellationToken.None)
            .Returns(true);
        var handler = new DrainRegistrationFinalizationEffectsCommandHandler(
            finalization, lifecycle, tenantAccessor, new FixedTimeProvider(UtcNow));

        int completed = await handler.Handle(new("worker"), CancellationToken.None);

        await Assert.That(completed).IsEqualTo(2);
        await lifecycle.Received(1).FinalizePaidAsync(duplicateOrderId, tenantId, CancellationToken.None);
        await lifecycle.Received(1).FinalizePaidAsync(validOrderId, tenantId, CancellationToken.None);
        await finalization.Received(1).CompleteAsync(duplicateClaim, UtcNow, CancellationToken.None);
        await finalization.Received(1).CompleteAsync(validClaim, UtcNow, CancellationToken.None);
        tenantAccessor.Received(2).Clear();
    }

    private static RegistrationOrderLifecycleResponseDto Response(
        Guid orderId,
        Guid tenantId,
        RegistrationOrderStatusEnum status) => new()
        {
            Success = true,
            Order = new RegistrationOrderDto
            {
                Id = orderId,
                TenantId = tenantId,
                StatusId = (int)status
            }
        };

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
