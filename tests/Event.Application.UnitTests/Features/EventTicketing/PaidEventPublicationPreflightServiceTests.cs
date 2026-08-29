// ABOUTME: Tests paid-ticket publication preflight blocker and success matrix.
// ABOUTME: Verifies effective policy fallback, organizer connection readiness, and commerce authorization facts.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventTicketing;
using Explore.Application.Features.EventTicketing.Services;
using Explore.Application.Services.Registration;
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
    private readonly ITenantDirectoryOperatorReadinessEvaluator _directoryReadiness =
        Substitute.For<ITenantDirectoryOperatorReadinessEvaluator>();

    public PaidEventPublicationPreflightServiceTests()
    {
        _tenant.TenantId.Returns(_tenantId);
        _commerceConfiguration.ProviderCode.Returns("stripe");
        _commerceConfiguration.ConnectPlatformId.Returns("platform-live-eu");
        _authorization.AuthorizeAsync(Arg.Any<AuthorizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(AuthorizationDecision.Allow(AuthorizationProviderMetadata.Runtime));
        _directoryReadiness.EvaluateAsync(
                Arg.Any<Guid>(),
                TenantDirectoryOperatorIdentityCapability.PaidCommerce,
                Arg.Any<CancellationToken>())
            .Returns(ReadyDirectoryIdentity());
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
    public async Task Assess_WhenDirectoryIdentityIsMissing_ReturnsPaidPublicationBlocker()
    {
        Actor organizer = CreateOrganizer(ActorTypeEnum.Organization);
        EventTicketCatalogVersion draft = CreatePaidDraft("USD");
        draft.UpdateCommercialDisclosures("Merchant", "Refund", "Support");
        ConfigureReadyFacts(organizer, ["USD"]);
        _directoryReadiness.EvaluateAsync(
                _tenantId,
                TenantDirectoryOperatorIdentityCapability.PaidCommerce,
                Arg.Any<CancellationToken>())
            .Returns(TenantDirectoryOperatorReadinessAssessment.Missing);

        PaidEventPublicationPreflightDto result = await CreateService().AssessAsync(
            _eventId,
            CreateEvent(organizer),
            draft,
            CancellationToken.None);

        await Assert.That(result.IsReady).IsFalse();
        await Assert.That(Codes(result))
            .Contains("tenant_directory_operator_identity_unavailable");
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

    [Test]
    public async Task Assess_LoadsPersistedEventAndDraftBeforeEvaluating()
    {
        Actor organizer = CreateOrganizer(ActorTypeEnum.Organization);
        DomainEvent eventTarget = CreateEvent(organizer);
        EventTicketCatalogVersion draft = CreateReadyPaidDraft();
        ConfigureReadyFacts(organizer, ["USD"]);
        _events.GetEventWithDetails(_eventId).Returns(eventTarget);
        _catalogs.GetDraftCatalogForUpdateAsync(
                _eventId,
                _tenantId,
                Arg.Any<CancellationToken>())
            .Returns(draft);

        PaidEventPublicationPreflightDto result =
            await CreateService().AssessAsync(_eventId, CancellationToken.None);

        await Assert.That(result.IsReady).IsTrue();
        _ = await _events.Received(1).GetEventWithDetails(_eventId);
        _ = await _catalogs.Received(1).GetDraftCatalogForUpdateAsync(
            _eventId,
            _tenantId,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Assess_MissingCrossTenantOrMissingDraft_FailsClosed()
    {
        Actor organizer = CreateOrganizer(ActorTypeEnum.Organization);
        DomainEvent validEvent = CreateEvent(organizer);
        DomainEvent crossTenantEvent = CreateEvent(organizer);
        crossTenantEvent.TenantId = Guid.CreateVersion7();
        EventTicketCatalogVersion draft = CreateReadyPaidDraft();

        foreach ((DomainEvent? eventTarget, EventTicketCatalogVersion? candidateDraft) in
                 new[]
                 {
                     ((DomainEvent?)null, (EventTicketCatalogVersion?)draft),
                     ((DomainEvent?)crossTenantEvent, (EventTicketCatalogVersion?)draft),
                     ((DomainEvent?)validEvent, (EventTicketCatalogVersion?)null)
                 })
        {
            PaidEventPublicationPreflightDto result = await CreateService().AssessAsync(
                _eventId,
                eventTarget,
                candidateDraft,
                CancellationToken.None);

            await Assert.That(result.IsReady).IsFalse();
            await Assert.That(result.IsPaidCatalog).IsFalse();
            await Assert.That(Codes(result)).Contains("ticketing_not_found");
        }
    }

    [Test]
    public async Task Assess_FreeCatalogBypassesPaidCommerceChecks()
    {
        Actor organizer = CreateOrganizer(ActorTypeEnum.Organization);
        EventTicketCatalogVersion freeDraft =
            EventTicketCatalogVersion.Create(_tenantId, _eventId, "USD", 1);

        PaidEventPublicationPreflightDto result = await CreateService().AssessAsync(
            _eventId,
            CreateEvent(organizer),
            freeDraft,
            CancellationToken.None);

        await Assert.That(result.IsPaidCatalog).IsFalse();
        await Assert.That(result.IsReady).IsTrue();
        _ = _directoryReadiness.DidNotReceiveWithAnyArgs().EvaluateAsync(
            default,
            default,
            default);
    }

    [Test]
    public async Task Assess_InactiveSaleControlReturnsItsBoundedBlocker()
    {
        Actor organizer = CreateOrganizer(ActorTypeEnum.Organization);
        var activation = Substitute.For<IPaidCheckoutActivationService>();
        activation.EvaluateSaleControlAsync(
                _tenantId,
                _eventId,
                Arg.Any<CancellationToken>())
            .Returns(new PaidCheckoutActivationResult(
                false,
                "sale_control_blocked",
                "Sale control blocked."));

        PaidEventPublicationPreflightDto result = await CreateService(activation).AssessAsync(
            _eventId,
            CreateEvent(organizer),
            CreateReadyPaidDraft(),
            CancellationToken.None);

        await Assert.That(result.IsReady).IsFalse();
        await Assert.That(Codes(result)).Contains("sale_control_blocked");
        _ = await _policies.DidNotReceiveWithAnyArgs().GetActiveInstanceAsync(default);
    }

    [Test]
    public async Task Assess_MissingOrganizerIdOrEntityFailsClosed()
    {
        Actor organizer = CreateOrganizer(ActorTypeEnum.Organization);
        ConfigureReadyFacts(organizer, ["USD"]);
        DomainEvent eventTarget = CreateEvent(organizer);
        eventTarget.OrganizerActorId = null;

        PaidEventPublicationPreflightDto missingId = await CreateService().AssessAsync(
            _eventId,
            eventTarget,
            CreateReadyPaidDraft(),
            CancellationToken.None);

        eventTarget.OrganizerActorId = organizer.Id;
        eventTarget.OrganizerActor = null;
        PaidEventPublicationPreflightDto missingEntity = await CreateService().AssessAsync(
            _eventId,
            eventTarget,
            CreateReadyPaidDraft(),
            CancellationToken.None);

        await Assert.That(Codes(missingId)).Contains("organizer_missing");
        await Assert.That(Codes(missingEntity)).Contains("organizer_missing");
    }

    [Test]
    public async Task Assess_MissingEitherCommerceCoordinateFailsClosed()
    {
        Actor organizer = CreateOrganizer(ActorTypeEnum.Organization);
        ConfigureReadyFacts(organizer, ["USD"]);

        _commerceConfiguration.ProviderCode.Returns((string?)null);
        PaidEventPublicationPreflightDto missingProvider = await CreateService().AssessAsync(
            _eventId,
            CreateEvent(organizer),
            CreateReadyPaidDraft(),
            CancellationToken.None);

        _commerceConfiguration.ProviderCode.Returns("stripe");
        _commerceConfiguration.ConnectPlatformId.Returns((string?)null);
        PaidEventPublicationPreflightDto missingPlatform = await CreateService().AssessAsync(
            _eventId,
            CreateEvent(organizer),
            CreateReadyPaidDraft(),
            CancellationToken.None);

        await Assert.That(Codes(missingProvider)).Contains("payment_platform_not_configured");
        await Assert.That(Codes(missingPlatform)).Contains("payment_platform_not_configured");
        _ = await _connections.DidNotReceiveWithAnyArgs().GetActiveByScopeAsync(
            default,
            default,
            default!,
            default!,
            default);
    }

    [Test]
    public async Task Assess_EachCommercialDisclosureIsIndependentlyRequired()
    {
        Actor organizer = CreateOrganizer(ActorTypeEnum.Organization);
        ConfigureReadyFacts(organizer, ["USD"]);

        foreach (string propertyName in new[]
                 {
                     nameof(EventTicketCatalogVersion.MerchantDisclosureText),
                     nameof(EventTicketCatalogVersion.RefundPolicyDisclosureText),
                     nameof(EventTicketCatalogVersion.SupportContactDisclosureText)
                 })
        {
            EventTicketCatalogVersion draft = CreateReadyPaidDraft();
            typeof(EventTicketCatalogVersion).GetProperty(propertyName)!.SetValue(draft, null);

            PaidEventPublicationPreflightDto result = await CreateService().AssessAsync(
                _eventId,
                CreateEvent(organizer),
                draft,
                CancellationToken.None);

            await Assert.That(Codes(result)).Contains("commercial_disclosures_missing");
        }
    }

    [Test]
    public async Task Assess_LocallyVerifiedUserAndGroupRequireExactEligibilityFacts()
    {
        Actor user = CreateOrganizer(ActorTypeEnum.User);
        ConfigureInstancePolicy(
            allowedOrganizerKinds: [ActorTypeEnum.User],
            requiresLocalVerification: true,
            currencies: ["USD"]);
        ConfigureConnectionAndAuthorization(user, ["USD"]);
        PaidEventPublicationPreflightDto verifiedUser = await CreateService().AssessAsync(
            _eventId,
            CreateEvent(user),
            CreateReadyPaidDraft(),
            CancellationToken.None);
        await Assert.That(verifiedUser.IsReady).IsTrue();

        Actor group = CreateOrganizer(ActorTypeEnum.Group);
        ConfigureInstancePolicy(
            allowedOrganizerKinds: [ActorTypeEnum.Group],
            requiresLocalVerification: true,
            currencies: ["USD"]);
        ConfigureConnectionAndAuthorization(group, ["USD"]);
        GroupTenant validParticipation =
            CreateGroupParticipation(group.GroupId!.Value);
        _groupTenants.GetByGroupAndTenant(
                group.GroupId.Value,
                _tenantId,
                Arg.Any<CancellationToken>())
            .Returns(validParticipation);

        PaidEventPublicationPreflightDto verifiedGroup = await CreateService().AssessAsync(
            _eventId,
            CreateEvent(group),
            CreateReadyPaidDraft(),
            CancellationToken.None);

        await Assert.That(verifiedGroup.IsReady).IsTrue();

        foreach (GroupTenant invalidParticipation in new[]
                 {
                     CreateGroupParticipation(group.GroupId.Value, isDeleted: true),
                     CreateGroupParticipation(group.GroupId.Value, isSuspended: true),
                     CreateGroupParticipation(group.GroupId.Value, isOrganizerEligible: false),
                     CreateGroupParticipation(group.GroupId.Value, isApproved: false)
                 })
        {
            _groupTenants.GetByGroupAndTenant(
                    group.GroupId.Value,
                    _tenantId,
                    Arg.Any<CancellationToken>())
                .Returns(invalidParticipation);

            PaidEventPublicationPreflightDto invalid = await CreateService().AssessAsync(
                _eventId,
                CreateEvent(group),
                CreateReadyPaidDraft(),
                CancellationToken.None);

            await Assert.That(Codes(invalid)).Contains("organizer_verification_required");
        }
    }

    private PaidEventPublicationPreflightService CreateService(
        IPaidCheckoutActivationService? activation = null) => new(
        _events,
        _catalogs,
        _policies,
        _connections,
        _organizationTenants,
        _groupTenants,
        _authorization,
        _tenant,
        _commerceConfiguration,
        _directoryReadiness,
        activation ?? ReadyCheckoutActivation());

    private static TenantDirectoryOperatorReadinessAssessment ReadyDirectoryIdentity()
    {
        TenantDirectoryOperatorIdentity identity =
            TenantDirectoryOperatorIdentity.Evaluate(
                new Explore.Domain.Settings.Documents.Payloads
                    .TenantDirectoryOperatorIdentitySettings
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

    private static IPaidCheckoutActivationService ReadyCheckoutActivation()
    {
        var activation = Substitute.For<IPaidCheckoutActivationService>();
        activation.EvaluateSaleControlAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new PaidCheckoutActivationResult(true, null, "active"));
        return activation;
    }

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
            fixedPrice: Money.Create(2_500, catalog.CurrencyCode),
            minimumPrice: null,
            suggestedPrice: null,
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

    private GroupTenant CreateGroupParticipation(
        Guid groupId,
        bool isDeleted = false,
        bool isSuspended = false,
        bool isOrganizerEligible = true,
        bool isApproved = true) => new()
        {
            TenantId = _tenantId,
            Tenant = null!,
            GroupId = groupId,
            Group = null!,
            ApprovalStatus = null!,
            IsDeleted = isDeleted,
            IsSuspended = isSuspended,
            IsOrganizerEligible = isOrganizerEligible,
            ApprovedAt = isApproved ? DateTime.UtcNow : null
        };

    private static string[] Codes(PaidEventPublicationPreflightDto result) => result.Blockers.Select(blocker => blocker.Code).ToArray();

    private static PaidEventRefundProtection[] RefundProtections() => Enum.GetValues<PaidEventRefundProtection>();
}
