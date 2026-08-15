// ABOUTME: Tests paid-ticket publication preflight blocker and success matrix.
// ABOUTME: Verifies effective policy fallback, organizer connection readiness, and commerce authorization facts.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventTicketing;
using Explore.Application.Features.EventTicketing.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.ValueObjects;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;
using DomainEvent = Explore.Domain.Event;

namespace Event.Application.UnitTests.Features.EventTicketing;

[Category("WaveCPaidPublication")]
public sealed class PaidEventPublicationPreflightServiceTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _eventId = Guid.CreateVersion7();
    private readonly IEventRepository _events = Substitute.For<IEventRepository>();
    private readonly IEventTicketCatalogRepository _catalogs = Substitute.For<IEventTicketCatalogRepository>();
    private readonly IPaidEventPolicyRepository _policies = Substitute.For<IPaidEventPolicyRepository>();
    private readonly IOrganizerPaymentProviderConnectionRepository _connections = Substitute.For<IOrganizerPaymentProviderConnectionRepository>();
    private readonly IOrganizationTenantRepository _organizationTenants = Substitute.For<IOrganizationTenantRepository>();
    private readonly IGroupTenantRepository _groupTenants = Substitute.For<IGroupTenantRepository>();
    private readonly IAuthorizationProvider _authorization = Substitute.For<IAuthorizationProvider>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly IOrganizerPaymentCommerceConfiguration _commerceConfiguration = Substitute.For<IOrganizerPaymentCommerceConfiguration>();

    public PaidEventPublicationPreflightServiceTests()
    {
        _tenant.TenantId.Returns(_tenantId);
        _commerceConfiguration.ProviderCode.Returns("stripe");
        _commerceConfiguration.ConnectPlatformId.Returns("platform-live-eu");
        _authorization.AuthorizeAsync(Arg.Any<AuthorizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(AuthorizationDecision.Allow(AuthorizationProviderMetadata.Runtime));
    }

    [Test]
    public async Task Assess_WhenInstancePolicyOnlyAndFactsAreReady_ReturnsReady()
    {
        Actor organizer = CreateOrganizer(ActorTypeEnum.Organization);
        EventTicketCatalogVersion draft = CreatePaidDraft("USD");
        draft.UpdateCommercialDisclosures("Merchant", "Refund", "Support");
        ConfigureInstancePolicy(allowedOrganizerKinds: [ActorTypeEnum.Organization], requiresLocalVerification: false, currencies: ["USD"]);
        _connections.GetActiveByScopeAsync(_tenantId, organizer.Id, "stripe", "platform-live-eu", Arg.Any<CancellationToken>())
            .Returns(CreateConnection(organizer.Id, ["USD"]));

        PaidEventPublicationPreflightDto result = await CreateService().AssessAsync(_eventId, CreateEvent(organizer), draft, CancellationToken.None);

        await Assert.That(result.IsPaidCatalog).IsTrue();
        await Assert.That(result.IsReady).IsTrue();
        await Assert.That(result.Blockers).IsEmpty();
    }

    [Test]
    public async Task Assess_WhenTenantPolicyBroadensInstancePolicy_ReturnsInvalidPolicyBlocker()
    {
        Actor organizer = CreateOrganizer(ActorTypeEnum.Organization);
        EventTicketCatalogVersion draft = CreateReadyPaidDraft();
        ConfigureInstancePolicy(allowedOrganizerKinds: [ActorTypeEnum.Organization], requiresLocalVerification: false, currencies: ["USD"]);
        ConfigureTenantPolicy(allowedOrganizerKinds: [ActorTypeEnum.Organization], requiresLocalVerification: false, currencies: ["USD", "EUR"]);

        PaidEventPublicationPreflightDto result = await CreateService().AssessAsync(_eventId, CreateEvent(organizer), draft, CancellationToken.None);

        await Assert.That(Codes(result)).Contains("paid_event_policy_invalid");
    }

    [Test]
    public async Task Assess_WhenPaidDraftLacksDisclosures_ReturnsDisclosureBlocker()
    {
        Actor organizer = CreateOrganizer(ActorTypeEnum.Organization);
        EventTicketCatalogVersion draft = CreatePaidDraft("USD");
        ConfigureReadyFacts(organizer, ["USD"]);

        PaidEventPublicationPreflightDto result = await CreateService().AssessAsync(_eventId, CreateEvent(organizer), draft, CancellationToken.None);

        await Assert.That(Codes(result)).Contains("commercial_disclosures_missing");
    }

    [Test]
    public async Task Assess_WhenOrganizerConnectionIsMissing_ReturnsConnectionMissingBlocker()
    {
        Actor organizer = CreateOrganizer(ActorTypeEnum.Organization);
        ConfigureReadyFacts(organizer, ["USD"], configureConnection: false);

        PaidEventPublicationPreflightDto result = await CreateService().AssessAsync(_eventId, CreateEvent(organizer), CreateReadyPaidDraft(), CancellationToken.None);

        await Assert.That(Codes(result)).Contains("organizer_connection_missing");
    }

    [Test]
    public async Task Assess_WhenConnectionIsNotReady_ReturnsConnectionReadinessBlockers()
    {
        Actor organizer = CreateOrganizer(ActorTypeEnum.Organization);
        ConfigureReadyFacts(organizer, ["USD"], configureConnection: false);
        _connections.GetActiveByScopeAsync(_tenantId, organizer.Id, "stripe", "platform-live-eu", Arg.Any<CancellationToken>())
            .Returns(CreateConnection(organizer.Id, ["USD"], ChargeCapabilityState.Inactive, ProviderRequirementsState.CurrentlyDue));

        PaidEventPublicationPreflightDto result = await CreateService().AssessAsync(_eventId, CreateEvent(organizer), CreateReadyPaidDraft(), CancellationToken.None);

        await Assert.That(Codes(result)).Contains("organizer_connection_not_ready");
        await Assert.That(Codes(result)).Contains("charge_capability_inactive");
        await Assert.That(Codes(result)).Contains("provider_requirements_pending");
    }

    [Test]
    public async Task Assess_WhenConnectionDoesNotSupportCurrency_ReturnsUnsupportedCurrencyBlocker()
    {
        Actor organizer = CreateOrganizer(ActorTypeEnum.Organization);
        ConfigureReadyFacts(organizer, ["EUR"]);

        PaidEventPublicationPreflightDto result = await CreateService().AssessAsync(_eventId, CreateEvent(organizer), CreateReadyPaidDraft(), CancellationToken.None);

        await Assert.That(Codes(result)).Contains("connection_currency_unsupported");
    }

    [Test]
    public async Task Assess_WhenOrganizerKindIsDenied_ReturnsOrganizerKindBlocker()
    {
        Actor organizer = CreateOrganizer(ActorTypeEnum.Organization);
        ConfigureInstancePolicy(allowedOrganizerKinds: [ActorTypeEnum.User], requiresLocalVerification: false, currencies: ["USD"]);
        ConfigureConnectionAndAuthorization(organizer, ["USD"]);

        PaidEventPublicationPreflightDto result = await CreateService().AssessAsync(_eventId, CreateEvent(organizer), CreateReadyPaidDraft(), CancellationToken.None);

        await Assert.That(Codes(result)).Contains("organizer_kind_not_allowed");
    }

    [Test]
    public async Task Assess_WhenLocalVerificationIsRequiredAndMissing_ReturnsVerificationBlocker()
    {
        Actor organizer = CreateOrganizer(ActorTypeEnum.Organization);
        ConfigureInstancePolicy(allowedOrganizerKinds: [ActorTypeEnum.Organization], requiresLocalVerification: true, currencies: ["USD"]);
        ConfigureConnectionAndAuthorization(organizer, ["USD"]);

        PaidEventPublicationPreflightDto result = await CreateService().AssessAsync(_eventId, CreateEvent(organizer), CreateReadyPaidDraft(), CancellationToken.None);

        await Assert.That(Codes(result)).Contains("organizer_verification_required");
    }

    [Test]
    public async Task Assess_WhenCommerceAuthorityDenied_ReturnsAuthorizationBlocker()
    {
        Actor organizer = CreateOrganizer(ActorTypeEnum.Organization);
        ConfigureReadyFacts(organizer, ["USD"]);
        _authorization.AuthorizeAsync(
                Arg.Is<AuthorizationRequest>(request =>
                    request != null &&
                    request.ResourceKind == ResourceKinds.Event &&
                    request.ResourceId == _eventId.ToString() &&
                    request.Action == AuthorizationActions.Events.ManagePaidEventCommerce),
                Arg.Any<CancellationToken>())
            .Returns(AuthorizationDecision.Deny(AuthorizationProviderMetadata.Runtime));

        PaidEventPublicationPreflightDto result = await CreateService().AssessAsync(_eventId, CreateEvent(organizer), CreateReadyPaidDraft(), CancellationToken.None);

        await Assert.That(Codes(result)).Contains("commerce_authorization_denied");
    }

    [Test]
    public async Task Assess_WhenCommerceAuthorityMatchesExactOrganizer_ReturnsReady()
    {
        Actor organizer = CreateOrganizer(ActorTypeEnum.Organization);
        ConfigureReadyFacts(organizer, ["USD"]);
        _authorization.AuthorizeAsync(
                Arg.Is<AuthorizationRequest>(request =>
                    request != null &&
                    request.ResourceKind == ResourceKinds.Event &&
                    request.ResourceId == _eventId.ToString() &&
                    request.Action == AuthorizationActions.Events.ManagePaidEventCommerce &&
                    request.ResourceAttributes == null &&
                    request.Facts != null &&
                    request.Facts.GetType() == typeof(EventAuthorizationFacts) &&
                    ((EventAuthorizationFacts)request.Facts).OrganizerActorId == organizer.Id),
                Arg.Any<CancellationToken>())
            .Returns(AuthorizationDecision.Allow(AuthorizationProviderMetadata.Runtime));

        PaidEventPublicationPreflightDto result = await CreateService().AssessAsync(_eventId, CreateEvent(organizer), CreateReadyPaidDraft(), CancellationToken.None);

        await Assert.That(result.IsReady).IsTrue();
        await Assert.That(result.Blockers).IsEmpty();
        await Assert.That(result.TenantId).IsEqualTo(_tenantId);
        await Assert.That(result.ActorId).IsEqualTo(organizer.Id);
        await Assert.That(result.ActorOrganizationId).IsEqualTo(organizer.OrganizationId);
        await Assert.That(result.OrganizerActorId).IsEqualTo(organizer.Id);
        await Assert.That(result.OrganizerOrganizationId).IsEqualTo(organizer.OrganizationId);
    }

    private PaidEventPublicationPreflightService CreateService() => new(
        _events,
        _catalogs,
        _policies,
        _connections,
        _organizationTenants,
        _groupTenants,
        _authorization,
        _tenant,
        _commerceConfiguration);

    private void ConfigureReadyFacts(Actor organizer, IReadOnlyList<string> connectionCurrencies, bool configureConnection = true)
    {
        ConfigureInstancePolicy(allowedOrganizerKinds: [ActorTypeEnum.Organization], requiresLocalVerification: false, currencies: ["USD"]);
        if (configureConnection)
        {
            ConfigureConnectionAndAuthorization(organizer, connectionCurrencies);
        }
    }

    private void ConfigureConnectionAndAuthorization(Actor organizer, IReadOnlyList<string> currencies)
    {
        _connections.GetActiveByScopeAsync(_tenantId, organizer.Id, "stripe", "platform-live-eu", Arg.Any<CancellationToken>())
            .Returns(CreateConnection(organizer.Id, currencies));
    }

    private void ConfigureInstancePolicy(IReadOnlyList<ActorTypeEnum> allowedOrganizerKinds, bool requiresLocalVerification, IReadOnlyList<string> currencies)
    {
        PaidEventPolicyVersion policy = PaidEventPolicyVersion.CreateDefaultInstance().CreateRevision(
            isPaymentsEnabled: true,
            allowedOrganizerKinds,
            requiresLocalVerification,
            currencies,
            currencies[0],
            RefundProtections(),
            currencyRiskLimits: [],
            requiresFirstPaidEventReview: false,
            farFutureReviewThresholdDays: null);
        _policies.GetActiveInstanceAsync(Arg.Any<CancellationToken>()).Returns(policy);
        _policies.GetActiveTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns((PaidEventPolicyVersion?)null);
    }

    private void ConfigureTenantPolicy(IReadOnlyList<ActorTypeEnum> allowedOrganizerKinds, bool requiresLocalVerification, IReadOnlyList<string> currencies)
    {
        PaidEventPolicyVersion policy = PaidEventPolicyVersion.CreateTenant(
            _tenantId,
            isPaymentsEnabled: true,
            allowedOrganizerKinds,
            requiresLocalVerification,
            currencies,
            currencies[0],
            RefundProtections(),
            currencyRiskLimits: [],
            requiresFirstPaidEventReview: false,
            farFutureReviewThresholdDays: null);
        _policies.GetActiveTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(policy);
    }

    private EventTicketCatalogVersion CreateReadyPaidDraft()
    {
        EventTicketCatalogVersion draft = CreatePaidDraft("USD");
        draft.UpdateCommercialDisclosures("Merchant", "Refund", "Support");
        return draft;
    }

    private EventTicketCatalogVersion CreatePaidDraft(string currencyCode)
    {
        EventTicketCatalogVersion catalog = EventTicketCatalogVersion.Create(_tenantId, _eventId, currencyCode, 1);
        EventTicketType ticketType = EventTicketType.Create(
            Guid.CreateVersion7(),
            catalog.TenantId,
            catalog.Id,
            "Paid admission",
            catalog.CurrencyCode,
            TicketPricingModeEnum.Fixed,
            fixedPriceMinor: 2_500,
            minimumPriceMinor: null,
            suggestedPriceMinor: null,
            participantDataCollectionMode: ParticipantDataCollectionModeEnum.None,
            capacityPoolId: null,
            minimumAge: null,
            maximumAge: null,
            requiresGuardian: false,
            requiresApproval: false,
            perOrderLimit: null,
            perAccountLimit: null,
            perVerifiedContactLimit: null,
            perBookingPartyLimit: null);
        catalog.AddTicketType(ticketType, capacityPool: null);
        catalog.AddEntitlement(ticketType, TicketTypeEntitlement.CreateForEvent(ticketType.Id, _tenantId, _eventId, 1));
        return catalog;
    }

    private DomainEvent CreateEvent(Actor organizer) => new()
    {
        Id = _eventId,
        TenantId = _tenantId,
        ActorId = organizer.Id,
        Title = "Ticketing event",
        Actor = organizer,
        OrganizerActorId = organizer.Id,
        OrganizerActor = organizer,
        Tenant = null!,
        VisibilityType = null!,
        EventStatus = null!,
        EventFormat = null!,
        ParticipationConfiguration = EventParticipationConfiguration.Create(
            _eventId,
            _tenantId,
            (int)ParticipationHandlingModeEnum.PlatformManaged,
            (int)AdvanceRegistrationObligationEnum.Required,
            (int)IdentityAccessModeEnum.AccountRequired,
            guestRecoveryPolicy: null,
            DateTime.UtcNow)
    };

    private Actor CreateOrganizer(ActorTypeEnum actorType) => new()
    {
        Id = Guid.CreateVersion7(),
        ActorTypeId = (int)actorType,
        ActorType = new ActorType { Id = (int)actorType, MasterCode = actorType.ToString(), FullName = actorType.ToString() },
        UserId = actorType == ActorTypeEnum.User ? Guid.CreateVersion7() : null,
        OrganizationId = actorType == ActorTypeEnum.Organization ? Guid.CreateVersion7() : null,
        GroupId = actorType == ActorTypeEnum.Group ? Guid.CreateVersion7() : null,
        Pii = new ActorPii { DisplayName = "Organizer" }
    };

    private OrganizerPaymentProviderConnection CreateConnection(
        Guid organizerActorId,
        IReadOnlyList<string> currencies,
        ChargeCapabilityState chargeCapabilityState = ChargeCapabilityState.Active,
        ProviderRequirementsState requirementsState = ProviderRequirementsState.Satisfied)
    {
        OrganizerPaymentProviderConnection connection = OrganizerPaymentProviderConnection.Create(
            Guid.CreateVersion7(),
            _tenantId,
            organizerActorId,
            "stripe",
            "platform-live-eu",
            $"acct_{Guid.CreateVersion7():N}",
            DateTime.UtcNow.AddMinutes(-5));
        connection.ApplyReadiness(OrganizerPaymentProviderReadinessObservation.Create(
            "BE",
            chargeCapabilityState,
            requirementsState,
            currencies,
            DateTime.UtcNow,
            "ready-1"));
        return connection;
    }

    private static string[] Codes(PaidEventPublicationPreflightDto result) => result.Blockers.Select(blocker => blocker.Code).ToArray();

    private static PaidEventRefundProtection[] RefundProtections() => Enum.GetValues<PaidEventRefundProtection>();
}
