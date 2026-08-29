// ABOUTME: Verifies exact server-authored schedule, operator, provider, typed line, and acceptance revisions.
// ABOUTME: Ensures incomplete startup governance or fabricated schedule evidence fails closed.

using System.Text.Json;
using Explore.Application.Contracts.Payments;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.RegistrationOrders;
using Explore.Application.Services.Registration;
using Explore.Domain;
using Explore.Domain.Settings.Documents.Payloads;
using Explore.Domain.ValueObjects;
using Explore.Domain.Enums;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;
using DomainEvent = Explore.Domain.Event;

namespace Event.Application.UnitTests.Services.Registration;

public sealed class PaidOrderAcceptanceServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _eventId = Guid.CreateVersion7();
    private readonly Guid _organizerActorId = Guid.CreateVersion7();
    private readonly IEventTicketCatalogRepository _catalogs = Substitute.For<IEventTicketCatalogRepository>();
    private readonly IEventRepository _events = Substitute.For<IEventRepository>();
    private readonly IPaidEventPolicyRepository _policies = Substitute.For<IPaidEventPolicyRepository>();
    private readonly IPaymentProviderDescriptor _provider = Substitute.For<IPaymentProviderDescriptor>();
    private readonly IPaidCheckoutActivationService _activation = Substitute.For<IPaidCheckoutActivationService>();
    private readonly IOrganizerPaymentProviderConnectionRepository _connections =
        Substitute.For<IOrganizerPaymentProviderConnectionRepository>();
    private readonly IOrganizerPaymentCommerceConfiguration _commerce =
        Substitute.For<IOrganizerPaymentCommerceConfiguration>();
    private readonly ITenantDirectoryOperatorReadinessEvaluator _directoryReadiness =
        Substitute.For<ITenantDirectoryOperatorReadinessEvaluator>();

    public PaidOrderAcceptanceServiceTests()
    {
        _commerce.ProviderCode.Returns("stripe");
        _commerce.ConnectPlatformId.Returns("platform-live-eu");
        _directoryReadiness.EvaluateAsync(
                Arg.Any<Guid>(),
                TenantDirectoryOperatorIdentityCapability.PaidCommerce,
                Arg.Any<CancellationToken>())
            .Returns(DirectoryIdentityReadiness());
    }

    [Test]
    public async Task DescribeAndAcceptUseExactScheduleOperatorProviderAndTypedLineFacts()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog) = OrderAndCatalog();
        ConfigureCurrentFacts(order, catalog);
        var service = Service(Governance());

        PaidOrderAcceptanceResult described = await service.DescribeAsync(order, CancellationToken.None);
        PaidOrderAcceptanceResult accepted = await service.AcceptAsync(order, new PaidOrderAcceptanceAcknowledgementDto
        {
            Acknowledged = true,
            DisclosureRevision = described.Disclosure!.DisclosureRevision
        }, UtcNow, CancellationToken.None);

        string disclosureJson = JsonSerializer.Serialize(
            described.Disclosure!,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await Assert.That(disclosureJson).Contains("\"organizerMerchant\"");
        await Assert.That(disclosureJson).Contains("\"tenantDirectoryOperator\"");
        await Assert.That(disclosureJson).Contains("\"instanceOperator\"");
        await Assert.That(disclosureJson).DoesNotContain("\"paidEventDirectoryDisclaimer\"");
        await Assert.That(described.Disclosure.DeliveryStartsAtUtc).IsEqualTo(DateTimeOffset.Parse("2026-09-10T17:00:00Z"));
        await Assert.That(described.Disclosure.DeliveryEndsAtUtc).IsEqualTo(DateTimeOffset.Parse("2026-09-10T20:00:00Z"));
        await Assert.That(described.Disclosure.EventTimeZoneId).IsEqualTo("Europe/Brussels");
        await Assert.That(described.Disclosure.OrganizerMerchant.ProviderEnvironment).IsEqualTo("test");
        await Assert.That(described.Disclosure.OrganizerMerchant.ProviderCredentialOwner).IsEqualTo("instance-operator");
        await Assert.That(described.Disclosure.PaymentOperations.ComplaintOwner).IsEqualTo("Trust and Safety");
        await Assert.That(accepted.Snapshot!.Lines.Count).IsEqualTo(1);
        await Assert.That(accepted.Snapshot.Lines.Single().OrderLineId).IsEqualTo(order.Lines.Single().Id);
        string snapshotJson = JsonSerializer.Serialize(
            accepted.Snapshot,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await Assert.That(snapshotJson).Contains("\"tenantDirectoryOperatorRevisionId\"");
        await Assert.That(snapshotJson).Contains("\"organizerActorId\"");
    }

    [Test]
    public async Task DescribeAsync_MissingDirectoryIdentityFailsBeforeAcceptanceComposition()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog) = OrderAndCatalog();
        ConfigureCurrentFacts(order, catalog);
        _directoryReadiness.EvaluateAsync(
                order.TenantId,
                TenantDirectoryOperatorIdentityCapability.PaidCommerce,
                Arg.Any<CancellationToken>())
            .Returns(TenantDirectoryOperatorReadinessAssessment.Missing);

        PaidOrderAcceptanceResult result = await Service(Governance())
            .DescribeAsync(order, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode)
            .IsEqualTo("tenant_directory_operator_identity_unavailable");
        await Assert.That(result.Disclosure).IsNull();
    }

    [Test]
    public async Task DisclosureRevisionChangesWhenTypedLineOrScheduleChanges()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog) = OrderAndCatalog();
        DomainEvent eventTarget = EventTarget();
        ConfigureCurrentFacts(order, catalog, eventTarget);
        PaidOrderAcceptanceResult first = await Service(Governance()).DescribeAsync(order, CancellationToken.None);
        eventTarget.LastSessionEndUtc = eventTarget.LastSessionEndUtc!.Value.AddHours(1);
        PaidOrderAcceptanceResult changed = await Service(Governance()).DescribeAsync(order, CancellationToken.None);

        await Assert.That(changed.Disclosure!.DisclosureRevision).IsNotEqualTo(first.Disclosure!.DisclosureRevision);
    }

    [Test]
    public async Task DisclosureRevisionChangesWhenDirectoryIdentityRevisionChanges()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog) = OrderAndCatalog();
        ConfigureCurrentFacts(order, catalog);
        PaidOrderAcceptanceResult first = await Service(Governance())
            .DescribeAsync(order, CancellationToken.None);
        TenantDirectoryOperatorReadinessAssessment current = DirectoryIdentityReadiness();
        _directoryReadiness.EvaluateAsync(
                order.TenantId,
                TenantDirectoryOperatorIdentityCapability.PaidCommerce,
                Arg.Any<CancellationToken>())
            .Returns(TenantDirectoryOperatorReadinessAssessment.Ready(
                current.Identity!,
                Guid.CreateVersion7(),
                current.DocumentId!.Value));

        PaidOrderAcceptanceResult changed = await Service(Governance())
            .DescribeAsync(order, CancellationToken.None);

        await Assert.That(changed.Disclosure!.DisclosureRevision)
            .IsNotEqualTo(first.Disclosure!.DisclosureRevision);
    }

    [Test]
    public async Task MissingScheduleOrStartupApprovedOwnershipFailsClosed()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog) = OrderAndCatalog();
        ConfigureCurrentFacts(order, catalog, EventTarget(withSchedule: false));
        PaidOrderAcceptanceResult missingSchedule = await Service(Governance()).DescribeAsync(order, CancellationToken.None);
        ConfigureCurrentFacts(order, catalog);
        PaidOrderAcceptanceResult suspended = await Service(Governance(activationStatus: "suspended")).DescribeAsync(order, CancellationToken.None);

        await Assert.That(missingSchedule.FailureCode).IsEqualTo("payment_acceptance_unavailable");
        await Assert.That(suspended.FailureCode).IsEqualTo("payment_acceptance_unavailable");
    }

    [Test]
    public async Task AcceptBindsThePolicyFactsUsedByTheAcknowledgedRevision()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog) = OrderAndCatalog();
        ConfigureCurrentFacts(order, catalog);
        PaidEventPolicyVersion policyA = EnabledPolicy();
        PaidEventPolicyVersion policyB = EnabledPolicy();
        _policies.GetActiveInstanceAsync(Arg.Any<CancellationToken>()).Returns(policyA);
        PaidOrderAcceptanceService service = Service(Governance());
        PaidOrderAcceptanceResult described = await service.DescribeAsync(order, CancellationToken.None);
        _policies.GetActiveInstanceAsync(Arg.Any<CancellationToken>()).Returns(policyA, policyB);

        PaidOrderAcceptanceResult accepted = await service.AcceptAsync(order, new PaidOrderAcceptanceAcknowledgementDto
        {
            Acknowledged = true,
            DisclosureRevision = described.Disclosure!.DisclosureRevision
        }, UtcNow, CancellationToken.None);

        await Assert.That(accepted.Snapshot).IsNotNull();
        await Assert.That(accepted.Snapshot!.InstancePolicyVersionId).IsEqualTo(policyA.Id);
        await Assert.That(accepted.Authority!.InstancePolicyVersionId).IsEqualTo(policyA.Id);
    }

    [Test]
    public async Task AcceptRejectsNormalizedEquivalentOfCanonicalRevision()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog) = OrderAndCatalog();
        ConfigureCurrentFacts(order, catalog);
        PaidOrderAcceptanceService service = Service(Governance());
        PaidOrderAcceptanceResult described = await service.DescribeAsync(order, CancellationToken.None);

        PaidOrderAcceptanceResult accepted = await service.AcceptAsync(order, new PaidOrderAcceptanceAcknowledgementDto
        {
            Acknowledged = true,
            DisclosureRevision = $" {described.Disclosure!.DisclosureRevision.ToUpperInvariant()} "
        }, UtcNow, CancellationToken.None);

        await Assert.That(accepted.FailureCode).IsEqualTo("payment_acceptance_stale");
        await Assert.That(accepted.Snapshot).IsNull();
    }

    [Test]
    public async Task SuccessRequiresDisclosureAndNoFailureCode()
    {
        var result = new PaidOrderAcceptanceResult(null, null, null, null);

        await Assert.That(result.Success).IsFalse();
    }

    [Test]
    public async Task DescribeRejectsNullOrderBeforeCallingDependencies()
    {
        PaidOrderAcceptanceService service = Service(Governance());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.DescribeAsync(null!, CancellationToken.None));
        await _activation.DidNotReceive().EvaluateAsync(
            Arg.Any<PaidCheckoutActivationRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task MissingCatalogNullInstanceOrDisabledInstancePolicyFailsClosed()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog) = OrderAndCatalog();
        ConfigureCurrentFacts(order, catalog);
        PaidOrderAcceptanceService service = Service(Governance());

        _catalogs.GetOrderCatalogAsync(
            order.TicketCatalogVersionId, order.EventId, order.TenantId, Arg.Any<CancellationToken>())
            .Returns((EventTicketCatalogVersion?)null);
        PaidOrderAcceptanceResult missingCatalog = await service.DescribeAsync(order, CancellationToken.None);

        _catalogs.GetOrderCatalogAsync(
            order.TicketCatalogVersionId, order.EventId, order.TenantId, Arg.Any<CancellationToken>())
            .Returns(catalog);
        _policies.GetActiveInstanceAsync(Arg.Any<CancellationToken>())
            .Returns((PaidEventPolicyVersion?)null);
        PaidOrderAcceptanceResult missingInstance = await service.DescribeAsync(order, CancellationToken.None);

        _policies.GetActiveInstanceAsync(Arg.Any<CancellationToken>())
            .Returns(PaidEventPolicyVersion.CreateDefaultInstance());
        PaidOrderAcceptanceResult disabledInstance = await service.DescribeAsync(order, CancellationToken.None);

        await Assert.That(missingCatalog.FailureCode).IsEqualTo("payment_acceptance_unavailable");
        await Assert.That(missingInstance.FailureCode).IsEqualTo("payment_acceptance_unavailable");
        await Assert.That(disabledInstance.FailureCode).IsEqualTo("payment_acceptance_unavailable");
    }

    [Test]
    public async Task AcceptRequiresAcknowledgedFlagWithCurrentRevision()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog) = OrderAndCatalog();
        ConfigureCurrentFacts(order, catalog);
        PaidOrderAcceptanceService service = Service(Governance());
        PaidOrderAcceptanceResult described = await service.DescribeAsync(order, CancellationToken.None);

        PaidOrderAcceptanceResult accepted = await service.AcceptAsync(order, new PaidOrderAcceptanceAcknowledgementDto
        {
            Acknowledged = false,
            DisclosureRevision = described.Disclosure!.DisclosureRevision
        }, UtcNow, CancellationToken.None);

        await Assert.That(accepted.FailureCode).IsEqualTo("payment_acceptance_required");
        await Assert.That(accepted.Snapshot).IsNull();
    }

    [Test]
    public async Task InactiveActivationWithoutFailureCodeUsesMachineFallback()
    {
        (RegistrationOrder order, _) = OrderAndCatalog();
        PaidOrderAcceptanceService service = Service(Governance());
        _activation.EvaluateAsync(Arg.Any<PaidCheckoutActivationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaidCheckoutActivationResult(false, null, "inactive"));

        PaidOrderAcceptanceResult result = await service.DescribeAsync(order, CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("payment_activation_unavailable");
    }

    [Test]
    public async Task TenantPolicyWithInstanceScopeIsRejectedAsInvalid()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog) = OrderAndCatalog();
        ConfigureCurrentFacts(order, catalog);
        _policies.GetActiveTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(EnabledPolicy());

        PaidOrderAcceptanceResult result = await Service(Governance()).DescribeAsync(order, CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("payment_policy_invalid");
    }

    [Test]
    public async Task DisabledTenantPolicyIsUnavailable()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog) = OrderAndCatalog();
        ConfigureCurrentFacts(order, catalog);
        _policies.GetActiveTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(TenantPolicy(isPaymentsEnabled: false));

        PaidOrderAcceptanceResult result = await Service(Governance()).DescribeAsync(order, CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("payment_policy_unavailable");
    }

    [Test]
    public async Task TenantPolicyIdentityChangesAuthorityAndRevision()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog) = OrderAndCatalog();
        ConfigureCurrentFacts(order, catalog);
        PaidEventPolicyVersion policyA = TenantPolicy();
        PaidEventPolicyVersion policyB = TenantPolicy();
        _policies.GetActiveTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(policyA, policyB);
        PaidOrderAcceptanceService service = Service(Governance());

        PaidOrderAcceptanceResult first = await service.DescribeAsync(order, CancellationToken.None);
        PaidOrderAcceptanceResult second = await service.DescribeAsync(order, CancellationToken.None);

        await Assert.That(first.Success).IsTrue();
        await Assert.That(second.Success).IsTrue();
        await Assert.That(first.Authority!.TenantPolicyVersionId).IsEqualTo(policyA.Id);
        await Assert.That(second.Authority!.TenantPolicyVersionId).IsEqualTo(policyB.Id);
        await Assert.That(second.Disclosure!.DisclosureRevision).IsNotEqualTo(first.Disclosure!.DisclosureRevision);
    }

    [Test]
    public async Task DisclosureLinesHaveStableAscendingOrder()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog) = OrderAndCatalog(includeSecondLine: true);
        ConfigureCurrentFacts(order, catalog);

        PaidOrderAcceptanceResult result = await Service(Governance()).DescribeAsync(order, CancellationToken.None);
        Guid[] lineIds = result.Disclosure!.Lines.Select(line => line.OrderLineId).ToArray();

        await Assert.That(lineIds.SequenceEqual(lineIds.Order())).IsTrue();
    }

    private PaidOrderAcceptanceService Service(IPaidCheckoutGovernance governance)
    {
        _activation.EvaluateAsync(Arg.Any<PaidCheckoutActivationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaidCheckoutActivationResult(true, null, "active"));
        return new(
            _catalogs,
            _events,
            _policies,
            InstanceIdentity(),
            governance,
            _directoryReadiness,
            _connections,
            _commerce,
            _activation,
            _provider,
            new FixedTimeProvider(UtcNow));
    }

    private void ConfigureCurrentFacts(RegistrationOrder order, EventTicketCatalogVersion catalog, DomainEvent? eventTarget = null)
    {
        _catalogs.GetOrderCatalogAsync(order.TicketCatalogVersionId, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _events.GetEventWithDetailsAsync(_eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(eventTarget ?? EventTarget());
        _policies.GetActiveInstanceAsync(Arg.Any<CancellationToken>()).Returns(EnabledPolicy());
        _provider.Describe().Returns(new PaymentProviderDescriptor(
            "stripe", "OrganizerDirect", "2026-07-29.dahlia", "test", "instance-operator"));
        OrganizerPaymentProviderConnection connection = OrganizerPaymentProviderConnection.Create(
            Guid.CreateVersion7(), _tenantId, _organizerActorId, "stripe", "platform-live-eu", "acct_123", UtcNow);
        typeof(OrganizerPaymentProviderConnection).GetProperty(nameof(OrganizerPaymentProviderConnection.MerchantCountryCode))!
            .SetValue(connection, "BE");
        _connections.GetActiveByScopeAsync(
            _tenantId, _organizerActorId, "stripe", "platform-live-eu", Arg.Any<CancellationToken>())
            .Returns(connection);
    }

    private (RegistrationOrder Order, EventTicketCatalogVersion Catalog) OrderAndCatalog(bool includeSecondLine = false)
    {
        EventTicketCatalogVersion catalog = EventTicketCatalogVersion.Create(_tenantId, _eventId, "EUR", 7);
        EventTicketType ticket = EventTicketType.Create(
            Guid.CreateVersion7(), _tenantId, catalog.Id, "General admission", "EUR", TicketPricingModeEnum.Fixed,
            Money.Create(1_000, "EUR"), null, null, ParticipantDataCollectionModeEnum.None, null, null, null, false, false,
            null, null, null, null);
        catalog.AddTicketType(ticket, null);
        catalog.AddEntitlement(ticket, TicketTypeEntitlement.CreateForEvent(ticket.Id, _tenantId, _eventId, 1));
        EventTicketType? secondTicket = null;
        if (includeSecondLine)
        {
            secondTicket = EventTicketType.Create(
                Guid.CreateVersion7(), _tenantId, catalog.Id, "Reserved admission", "EUR", TicketPricingModeEnum.Fixed,
                Money.Create(500, "EUR"), null, null, ParticipantDataCollectionModeEnum.None, null, null, null, false, false,
                null, null, null, null);
            catalog.AddTicketType(secondTicket, null);
            catalog.AddEntitlement(secondTicket, TicketTypeEntitlement.CreateForEvent(secondTicket.Id, _tenantId, _eventId, 1));
        }
        catalog.UpdateCommercialDisclosures(
            "Example Organizer, legal merchant for this order", "Refund policy", "support@example.test");
        catalog.Publish();
        RegistrationOrder order = RegistrationOrder.Create(
            Guid.CreateVersion7(), _tenantId, _eventId, Guid.CreateVersion7(), Guid.CreateVersion7(),
            BookingPartyTypeEnum.Individual, catalog.Id,
            RegistrationParticipationSnapshot.Create(Guid.CreateVersion7(), 1, 1, 1, GuestRecoveryPolicyEnum.VerifiedEmailRequired),
            null, null, "EUR", UtcNow.AddMinutes(-5), UtcNow.AddMinutes(30));
        order.AddLine(RegistrationOrderLine.Create(catalog, ticket, order.Id, 1, null, null));
        if (secondTicket is not null)
        {
            order.AddLine(RegistrationOrderLine.Create(catalog, secondTicket, order.Id, 1, null, null));
        }
        long organizerAmountMinor = includeSecondLine ? 1_500 : 1_000;
        order.ApplyTotals(RegistrationOrderTotalsSnapshot.Create(
            "EUR", organizerAmountMinor, 75, organizerAmountMinor - 75, 0));
        order.ConcurrencyStamp = Guid.CreateVersion7();
        return (order, catalog);
    }

    private DomainEvent EventTarget(bool withSchedule = true) => new(EventStatusEnum.Published)
    {
        Id = _eventId,
        TenantId = _tenantId,
        Title = "Scheduled event",
        Actor = null!,
        Tenant = null!,
        VisibilityType = null!,
        EventStatus = null!,
        EventFormat = null!,
        FirstSessionStartUtc = withSchedule ? DateTimeOffset.Parse("2026-09-10T17:00:00Z") : null,
        LastSessionEndUtc = withSchedule ? DateTimeOffset.Parse("2026-09-10T20:00:00Z") : null,
        EventTimeZoneId = withSchedule ? "Europe/Brussels" : null,
        OrganizerActorId = _organizerActorId
    };

    private static PaidEventPolicyVersion EnabledPolicy()
    {
        PaidEventPolicyVersion disabled = PaidEventPolicyVersion.CreateDefaultInstance();
        return disabled.CreateRevision(true, disabled.AllowedOrganizerKinds, disabled.RequiresLocalVerification,
            disabled.AllowedCurrencyCodes, "EUR", disabled.RefundProtections, [], false, null);
    }

    private PaidEventPolicyVersion TenantPolicy(bool isPaymentsEnabled = true)
    {
        PaidEventPolicyVersion defaults = PaidEventPolicyVersion.CreateDefaultInstance();
        return PaidEventPolicyVersion.CreateTenant(
            _tenantId, isPaymentsEnabled, defaults.AllowedOrganizerKinds, defaults.RequiresLocalVerification,
            ["EUR"], "EUR", defaults.RefundProtections, [], false, null);
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private static IPaidCheckoutGovernance Governance(string activationStatus = "approved")
    {
        var governance = Substitute.For<IPaidCheckoutGovernance>();
        governance.ComplaintOwner.Returns("Trust and Safety");
        governance.RefundOwner.Returns("Payments Operations");
        governance.DisputeOwner.Returns("Dispute Operations");
        governance.ReconciliationOwner.Returns("Payment Reconciliation");
        governance.ActivationStatus.Returns(activationStatus);
        governance.RefundPolicyLanguageTag.Returns("en-GB");
        governance.StatementDescriptor.Returns("EXAMPLE EVENT");
        governance.ChargeType.Returns("direct-charge");
        governance.IsConfigured.Returns(true);
        governance.IsActivated.Returns(activationStatus == "approved");
        return governance;
    }

    private static IInstanceOperatorIdentity InstanceIdentity() =>
        InstanceOperatorIdentity.Create(new InstanceOperatorIdentityOptions
        {
            OperatorId = Guid.CreateVersion7(),
            PublicName = "Independent Operator",
            LegalName = "Independent Operator ASBL",
            IsOfficialInstance = false,
            OfficialOrigin = "https://events.example.test",
            OperatorKindCode = "registered_organization",
            JurisdictionCountryCode = "BE",
            RegistrationIdentifier = "BE 0123.456.789",
            PublicContactEmail = "complaints@example.test",
            WebsiteUrl = "https://events.example.test",
            LegalNoticeUrl = "https://events.example.test/legal",
            TermsUrl = "https://events.example.test/terms",
            PrivacyUrl = "https://events.example.test/privacy"
        });

    private static TenantDirectoryOperatorReadinessAssessment DirectoryIdentityReadiness()
    {
        TenantDirectoryOperatorIdentity identity =
            TenantDirectoryOperatorIdentity.Evaluate(
                new TenantDirectoryOperatorIdentitySettings
                {
                    PublicName = "Community Events",
                    LegalName = "Community Events ASBL",
                    OperatorKindCode =
                        TenantDirectoryOperatorKinds.RegisteredOrganization,
                    JurisdictionCountryCode = "BE",
                    PublicContactEmail = "contact@example.test",
                    LegalNoticeUrl = "https://example.test/legal",
                    TermsUrl = "https://example.test/terms",
                    PrivacyUrl = "https://example.test/privacy"
                },
                TenantDirectoryOperatorIdentityCapability.PaidCommerce)
                .Identity!;
        return TenantDirectoryOperatorReadinessAssessment.Ready(
            identity,
            Guid.CreateVersion7(),
            Guid.CreateVersion7());
    }

}
