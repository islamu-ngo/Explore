// ABOUTME: Covers registration-order PII separation, immutable participation snapshots, and totals snapshots.
// ABOUTME: Proves free, approval, and paid-boundary paths preserve the explicit order state machine.

using Explore.Domain.Enums;
using Explore.Domain.Services.Registration;
using Explore.Domain.ValueObjects;

namespace Event.Domain.UnitTests.Entities;

public sealed class RegistrationOrderTests
{
    [Test]
    public async Task Create_KeepsPurchaserPiiOutsideTheOrderAndPinsParticipation()
    {
        RegistrationParticipationSnapshot participationSnapshot = RegistrationParticipationSnapshot.Create(
            Guid.CreateVersion7(),
            4,
            3,
            2,
            GuestRecoveryPolicyEnum.VerifiedEmailRequired);
        RegistrationOrder order = CreateOrder(participationSnapshot);
        RegistrationOrderPii pii = RegistrationOrderPii.Create(
            order.Id,
            order.TenantId,
            "Jane Doe",
            "Jane.Doe@example.test",
            "+32 470 00 00 00",
            "ISLAMU");

        order.SetPii(pii);

        await Assert.That(order.ParticipationConfigurationVersionSnapshot)
            .IsEqualTo(participationSnapshot.ConfigurationVersion);
        await Assert.That(order.ParticipationSnapshot.IdentityAccessModeId).IsEqualTo(2);
        await Assert.That(order.Pii).IsEqualTo(pii);
        await Assert.That(pii.NormalizedEmail).IsEqualTo("JANE.DOE@EXAMPLE.TEST");
        await Assert.That(typeof(RegistrationOrder).GetProperties()
                .Select(property => property.Name)
                .Intersect(["ContactName", "Email", "NormalizedEmail", "Phone", "OrganizationName"]))
            .IsEmpty();
    }

    [Test]
    public async Task TransitionTo_ImplementsFreePaidAndApprovalPaths()
    {
        DateTime timestamp = new(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);
        RegistrationOrder freeOrder = CreateOrder();
        RegistrationOrder paidOrder = CreatePricedOrder();
        RegistrationOrder approvalOrder = CreateOrder();

        freeOrder.ApplyTotals(RegistrationOrderTotalsSnapshot.Create("USD", 0, 0, 0, 0));

        freeOrder.TransitionTo(RegistrationOrderStatusEnum.AwaitingRequirements, timestamp);
        freeOrder.TransitionTo(RegistrationOrderStatusEnum.ReadyForCheckout, timestamp);
        freeOrder.TransitionTo(RegistrationOrderStatusEnum.Confirmed, timestamp);

        paidOrder.TransitionTo(RegistrationOrderStatusEnum.AwaitingRequirements, timestamp);
        paidOrder.TransitionTo(RegistrationOrderStatusEnum.ReadyForCheckout, timestamp);
        paidOrder.TransitionTo(RegistrationOrderStatusEnum.AwaitingPayment, timestamp);

        approvalOrder.TransitionTo(RegistrationOrderStatusEnum.AwaitingRequirements, timestamp);
        approvalOrder.TransitionTo(RegistrationOrderStatusEnum.AwaitingApproval, timestamp);
        approvalOrder.TransitionTo(RegistrationOrderStatusEnum.Rejected, timestamp);

        await Assert.That(freeOrder.RegistrationOrderStatusId).IsEqualTo((int)RegistrationOrderStatusEnum.Confirmed);
        await Assert.That(freeOrder.ConfirmedAt).IsEqualTo(timestamp);
        await Assert.That(paidOrder.RegistrationOrderStatusId).IsEqualTo((int)RegistrationOrderStatusEnum.AwaitingPayment);
        await Assert.That(RegistrationOrderRules.IsTerminalForCurrentWorkstream((RegistrationOrderStatusEnum)paidOrder.RegistrationOrderStatusId)).IsTrue();
        await Assert.That(approvalOrder.RejectedAt).IsEqualTo(timestamp);
        await Assert.That(() => approvalOrder.TransitionTo(RegistrationOrderStatusEnum.Confirmed, timestamp))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task TransitionTo_WhenTerminalStatusIsReplayed_PreservesOriginalTimestamp()
    {
        DateTime originalTimestamp = new(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);
        DateTime replayTimestamp = originalTimestamp.AddMinutes(1);
        RegistrationOrder order = CreateOrder();
        order.ApplyTotals(RegistrationOrderTotalsSnapshot.Create("USD", 0, 0, 0, 0));
        order.TransitionTo(RegistrationOrderStatusEnum.AwaitingRequirements, originalTimestamp);
        order.TransitionTo(RegistrationOrderStatusEnum.ReadyForCheckout, originalTimestamp);
        order.TransitionTo(RegistrationOrderStatusEnum.Confirmed, originalTimestamp);

        order.TransitionTo(RegistrationOrderStatusEnum.Confirmed, replayTimestamp);

        await Assert.That(order.ConfirmedAt).IsEqualTo(originalTimestamp);
    }

    [Test]
    public async Task TransitionTo_WhenPositiveTotalIsReady_RejectsFreeConfirmation()
    {
        DateTime timestamp = new(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);
        RegistrationOrder order = CreatePricedOrder();
        order.TransitionTo(RegistrationOrderStatusEnum.AwaitingRequirements, timestamp);
        order.TransitionTo(RegistrationOrderStatusEnum.ReadyForCheckout, timestamp);

        await Assert.That(() => order.TransitionTo(RegistrationOrderStatusEnum.Confirmed, timestamp))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task TransitionTo_WhenZeroTotalIsReady_RejectsPaymentBoundary()
    {
        DateTime timestamp = new(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);
        RegistrationOrder order = CreateOrder();
        order.ApplyTotals(RegistrationOrderTotalsSnapshot.Create("USD", 0, 0, 0, 0));
        order.TransitionTo(RegistrationOrderStatusEnum.AwaitingRequirements, timestamp);
        order.TransitionTo(RegistrationOrderStatusEnum.ReadyForCheckout, timestamp);

        await Assert.That(() => order.TransitionTo(RegistrationOrderStatusEnum.AwaitingPayment, timestamp))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task TransitionTo_WhenPricedLinesHaveNoTotalsSnapshot_RejectsCheckout()
    {
        DateTime timestamp = new(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);
        EventTicketCatalogVersion catalog = CreatePublishedCatalog();
        RegistrationOrder order = CreateOrder(
            ticketCatalogVersionId: catalog.Id,
            tenantId: catalog.TenantId,
            eventId: catalog.EventId);
        order.AddLine(RegistrationOrderLine.Create(
            catalog,
            catalog.TicketTypes.Single(),
            order.Id,
            1,
            null,
            null));
        order.TransitionTo(RegistrationOrderStatusEnum.AwaitingRequirements, timestamp);
        order.TransitionTo(RegistrationOrderStatusEnum.ReadyForCheckout, timestamp);

        await Assert.That(() => order.TransitionTo(RegistrationOrderStatusEnum.Confirmed, timestamp))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ApplyTotals_PersistsSeparateOrganizerAndPlatformContributionAmounts()
    {
        EventTicketCatalogVersion catalog = CreatePublishedCatalog();
        RegistrationOrder order = CreateOrder(
            ticketCatalogVersionId: catalog.Id,
            tenantId: catalog.TenantId,
            eventId: catalog.EventId);
        RegistrationOrderLine line = RegistrationOrderLine.Create(
            catalog,
            catalog.TicketTypes.Single(),
            order.Id,
            1,
            null,
            null);
        PlatformContributionSetting contributionSetting = PlatformContributionSetting.CreateInitial(
            true,
            "Support ISLAMU",
            "Optional contribution",
            [PlatformContributionOption.Create(0, 0, true), PlatformContributionOption.Create(1_000, 1, false)]);
        RegistrationOrderPlatformContribution? contribution = RegistrationOrderPlatformContribution.CreateOrNull(
            order.Id,
            order.TenantId,
            contributionSetting,
            1_000,
            line.LineSubtotalSnapshot,
            "USD");

        order.AddLine(line);
        order.SetPlatformContribution(contribution);
        order.ApplyTotals(RegistrationOrderTotalsSnapshot.Create(
            "USD",
            1_000,
            25,
            975,
            contribution?.AmountMinor ?? 0));

        await Assert.That(order.OrganizerDirectedTotalMinorSnapshot).IsEqualTo(1_000);
        await Assert.That(order.PlatformFeeTotalMinorSnapshot).IsEqualTo(25);
        await Assert.That(order.OrganizerEarningsTotalMinorSnapshot).IsEqualTo(975);
        await Assert.That(order.PlatformContributionTotalMinorSnapshot).IsEqualTo(100);
        await Assert.That(order.TotalDueMinorSnapshot).IsEqualTo(1_100);
        await Assert.That(order.PlatformContribution).IsEqualTo(contribution);
    }

    [Test]
    public async Task PlatformContribution_ZeroSelectionStoresNoRowAndDisabledSettingRejectsCreation()
    {
        PlatformContributionOption[] options =
        [
            PlatformContributionOption.Create(0, 0, true),
            PlatformContributionOption.Create(500, 1, false)
        ];
        PlatformContributionSetting enabled = PlatformContributionSetting.CreateInitial(true, "Support", "Optional", options);
        PlatformContributionSetting disabled = PlatformContributionSetting.CreateInitial(false, string.Empty, string.Empty, options);

        RegistrationOrderPlatformContribution? zeroSelection = RegistrationOrderPlatformContribution.CreateOrNull(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            enabled,
            0,
            1_000,
            "USD");

        await Assert.That(zeroSelection).IsNull();
        await Assert.That(() => RegistrationOrderPlatformContribution.CreateOrNull(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                disabled,
                500,
                1_000,
                "USD"))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task HoldRecovery_WhenExpiryRacesFinalization_RequiresExplicitReReserveOrWaitlistResolution()
    {
        DateTime timestamp = new(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);
        RegistrationOrder reReservedOrder = CreateOrder();
        RegistrationOrder waitlistedOrder = CreateOrder();
        reReservedOrder.TransitionTo(RegistrationOrderStatusEnum.AwaitingParticipantDetails, timestamp);
        reReservedOrder.TransitionTo(RegistrationOrderStatusEnum.AwaitingRequirements, timestamp);
        reReservedOrder.TransitionTo(RegistrationOrderStatusEnum.ReadyForCheckout, timestamp);
        waitlistedOrder.TransitionTo(RegistrationOrderStatusEnum.AwaitingParticipantDetails, timestamp);
        waitlistedOrder.TransitionTo(RegistrationOrderStatusEnum.AwaitingRequirements, timestamp);
        waitlistedOrder.TransitionTo(RegistrationOrderStatusEnum.ReadyForCheckout, timestamp);

        bool markedForReconciliation = reReservedOrder.TryBeginHoldExpiryRecovery(timestamp);
        bool reReserved = reReservedOrder.TryResolveHoldExpiryRecovery(capacityReReserved: true, timestamp.AddSeconds(1));
        waitlistedOrder.TryBeginHoldExpiryRecovery(timestamp);
        bool waitlisted = waitlistedOrder.TryResolveHoldExpiryRecovery(capacityReReserved: false, timestamp.AddSeconds(1));

        await Assert.That(markedForReconciliation).IsTrue();
        await Assert.That(reReserved).IsTrue();
        await Assert.That(waitlisted).IsTrue();
        await Assert.That(reReservedOrder.RegistrationOrderStatusId).IsEqualTo((int)RegistrationOrderStatusEnum.ReadyForCheckout);
        await Assert.That(waitlistedOrder.RegistrationOrderStatusId).IsEqualTo((int)RegistrationOrderStatusEnum.Waitlisted);
    }

    private static RegistrationOrder CreateOrder(
        RegistrationParticipationSnapshot? participationSnapshot = null,
        Guid? ticketCatalogVersionId = null,
        Guid? tenantId = null,
        Guid? eventId = null) => RegistrationOrder.Create(
        tenantId ?? Guid.CreateVersion7(),
        eventId ?? Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        BookingPartyTypeEnum.Individual,
        ticketCatalogVersionId ?? Guid.CreateVersion7(),
        participationSnapshot ?? RegistrationParticipationSnapshot.Create(Guid.CreateVersion7(), 4, 3, 2, GuestRecoveryPolicyEnum.VerifiedEmailRequired),
        null,
        CapabilityTokenHash.Create(Convert.ToBase64String(new byte[32])),
        "USD",
        new DateTime(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc),
        new DateTime(2026, 7, 30, 12, 15, 0, DateTimeKind.Utc));

    private static RegistrationOrder CreatePricedOrder()
    {
        EventTicketCatalogVersion catalog = CreatePublishedCatalog();
        RegistrationOrder order = CreateOrder(
            ticketCatalogVersionId: catalog.Id,
            tenantId: catalog.TenantId,
            eventId: catalog.EventId);
        RegistrationOrderLine line = RegistrationOrderLine.Create(
            catalog,
            catalog.TicketTypes.Single(),
            order.Id,
            1,
            null,
            null);
        order.AddLine(line);
        order.ApplyTotals(RegistrationOrderTotalsSnapshot.Create("USD", 1_000, 0, 1_000, 0));
        return order;
    }

    private static EventTicketCatalogVersion CreatePublishedCatalog()
    {
        EventTicketCatalogVersion catalog = EventTicketCatalogVersion.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), "USD", 1);
        EventTicketType ticketType = EventTicketType.Create(
            Guid.CreateVersion7(),
            catalog.TenantId,
            catalog.Id,
            "General admission",
            "USD",
            TicketPricingModeEnum.Fixed,
            1_000,
            null,
            null,
            ParticipantDataCollectionModeEnum.None,
            null,
            null,
            null,
            false,
            false,
            null,
            null,
            null,
            null);

        catalog.AddTicketType(ticketType, null);
        catalog.AddEntitlement(ticketType, TicketTypeEntitlement.CreateForEvent(ticketType.Id, catalog.TenantId, catalog.EventId, 1));
        catalog.Publish();
        return catalog;
    }
}
