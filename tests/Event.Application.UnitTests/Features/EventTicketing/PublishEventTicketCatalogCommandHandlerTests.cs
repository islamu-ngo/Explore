// ABOUTME: Tests ticket catalog publication handler validation, persistence, and cache behavior.
// ABOUTME: Ensures invalid or failed publication does not mutate the repository or clear event detail cache.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventTicketing.Handlers.Commands;
using Explore.Application.Features.EventTicketing.Requests.Commands;
using Explore.Application.Features.EventTicketing.Services;
using Explore.Application.Services.Registration;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.ValueObjects;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;
using DomainEvent = Explore.Domain.Event;

namespace Event.Application.UnitTests.Features.EventTicketing;

[Category("Phase43Ticketing")]
public sealed class PublishEventTicketCatalogCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _eventId = Guid.CreateVersion7();
    private readonly IEventRepository _events = Substitute.For<IEventRepository>();
    private readonly IEventTicketCatalogRepository _catalogs = Substitute.For<IEventTicketCatalogRepository>();
    private readonly IEventDayRepository _eventDays = Substitute.For<IEventDayRepository>();
    private readonly IEventSessionRepository _eventSessions = Substitute.For<IEventSessionRepository>();
    private readonly IPaidEventPolicyRepository _policies = Substitute.For<IPaidEventPolicyRepository>();
    private readonly IOrganizerPaymentProviderConnectionRepository _connections = Substitute.For<IOrganizerPaymentProviderConnectionRepository>();
    private readonly IOrganizationTenantRepository _organizationTenants = Substitute.For<IOrganizationTenantRepository>();
    private readonly IGroupTenantRepository _groupTenants = Substitute.For<IGroupTenantRepository>();
    private readonly IAuthorizationProvider _authorization = Substitute.For<IAuthorizationProvider>();
    private readonly IOrganizerPaymentCommerceConfiguration _commerceConfiguration = Substitute.For<IOrganizerPaymentCommerceConfiguration>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();

    public PublishEventTicketCatalogCommandHandlerTests()
    {
        _tenant.TenantId.Returns(_tenantId);
        _events.GetAuthorizationTargetByIdAsync(_eventId, Arg.Any<CancellationToken>())
            .Returns(CreatePlatformEvent());
        _events.GetEventWithDetails(_eventId).Returns(CreatePlatformEvent());
    }

    [Test]
    public async Task Handle_WhenTicketingIsMissing_ReturnsNotFoundWithoutMutationOrCacheInvalidation()
    {
        var unitOfWork = new RecordingUnitOfWork();
        _catalogs.GetDraftCatalogForUpdateAsync(_eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns((EventTicketCatalogVersion?)null);

        var result = await CreateHandler(unitOfWork).Handle(new PublishEventTicketCatalogCommand { EventId = _eventId }, CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_not_found");
        await Assert.That(unitOfWork.TransactionBoundaries).IsEqualTo(1);
        await _catalogs.DidNotReceive().GetPublishedForUpdateAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _catalogs.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenDraftFailsPublicationValidation_DoesNotMutateOrInvalidateCache()
    {
        EventTicketCatalogVersion draft = CreateDraftCatalog();
        var unitOfWork = new RecordingUnitOfWork();
        _catalogs.GetDraftCatalogForUpdateAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(draft);

        var result = await CreateHandler(unitOfWork).Handle(new PublishEventTicketCatalogCommand { EventId = _eventId }, CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_validation_failed");
        await Assert.That(result.Errors).Contains("A published ticket catalog requires at least one ticket type.");
        await Assert.That(unitOfWork.TransactionBoundaries).IsEqualTo(1);
        await _catalogs.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenDayEntitlementTargetIsStale_ReturnsValidationFailureWithoutMutation()
    {
        EventTicketCatalogVersion draft = CreateDraftCatalog();
        EventTicketType ticketType = CreateFreeTicket(draft);
        var eventDay = new EventDay
        {
            Id = Guid.CreateVersion7(),
            EventId = _eventId,
            TenantId = _tenantId,
            Event = null!,
            Tenant = null!,
            IsDeleted = true
        };
        draft.AddTicketType(ticketType, capacityPool: null);
        draft.AddEntitlement(ticketType, TicketTypeEntitlement.CreateForEventDay(
            ticketType.Id,
            eventDay,
            includedQuantity: 1,
            EntitlementSelectionRuleEnum.FixedSelection));
        var unitOfWork = new RecordingUnitOfWork();
        _catalogs.GetDraftCatalogForUpdateAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(draft);

        var result = await CreateHandler(unitOfWork).Handle(
            new PublishEventTicketCatalogCommand { EventId = _eventId },
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_validation_failed");
        await Assert.That(result.Errors).Contains(
            "Ticket entitlement targets must be active and belong to the catalog event and tenant.");
        await _eventDays.Received(1).GetByIdForEventForUpdateAsync(
            eventDay.Id,
            _eventId,
            _tenantId,
            Arg.Any<CancellationToken>());
        await _catalogs.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenSessionEntitlementTargetIsStale_ReturnsValidationFailureWithoutMutation()
    {
        EventTicketCatalogVersion draft = CreateDraftCatalog();
        EventTicketType ticketType = CreateFreeTicket(draft);
        var eventSession = new EventSession
        {
            Id = Guid.CreateVersion7(),
            EventId = _eventId,
            TenantId = _tenantId,
            Event = null!,
            Tenant = null!,
            IsDeleted = true
        };
        draft.AddTicketType(ticketType, capacityPool: null);
        draft.AddEntitlement(ticketType, TicketTypeEntitlement.CreateForEventSession(
            ticketType.Id,
            eventSession,
            includedQuantity: 1,
            EntitlementSelectionRuleEnum.FixedSelection));
        var unitOfWork = new RecordingUnitOfWork();
        _catalogs.GetDraftCatalogForUpdateAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(draft);

        var result = await CreateHandler(unitOfWork).Handle(
            new PublishEventTicketCatalogCommand { EventId = _eventId },
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_validation_failed");
        await Assert.That(result.Errors).Contains(
            "Ticket entitlement targets must be active and belong to the catalog event and tenant.");
        await _eventSessions.Received(1).GetByIdForEventForUpdateAsync(
            eventSession.Id,
            _eventId,
            _tenantId,
            Arg.Any<CancellationToken>());
        await _catalogs.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenTransactionFails_PropagatesAndDoesNotInvalidateCache()
    {
        EventTicketCatalogVersion draft = CreateValidDraftCatalog();
        var unitOfWork = new RecordingUnitOfWork(commitFailure: new InvalidOperationException("Catalog publication failed."));
        _catalogs.GetDraftCatalogForUpdateAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(draft);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateHandler(unitOfWork).Handle(
                new PublishEventTicketCatalogCommand { EventId = _eventId },
                CancellationToken.None));

        await Assert.That(exception.Message).IsEqualTo("Catalog publication failed.");
        await Assert.That(unitOfWork.HasCommitted).IsFalse();
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenPublicationSucceeds_FlushesRetirementBeforePublicationAndInvalidatesCacheAfterCommit()
    {
        EventTicketCatalogVersion draft = CreateValidDraftCatalog();
        EventTicketCatalogVersion currentPublication = CreatePublishedCatalog(_tenantId, _eventId, 2);
        var unitOfWork = new RecordingUnitOfWork();
        var flushes = new List<(int CurrentStatus, int DraftStatus)>();
        var cacheObservedCommittedUow = false;
        _catalogs.GetDraftCatalogForUpdateAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(draft);
        _catalogs.GetPublishedForUpdateAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(currentPublication);
        _catalogs.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            flushes.Add((currentPublication.TicketCatalogStatusId, draft.TicketCatalogStatusId));
            return Task.CompletedTask;
        });
        _cache.RemoveAsync($"event:detail:{_eventId}", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            cacheObservedCommittedUow = unitOfWork.HasCommitted;
            return ValueTask.CompletedTask;
        });

        var result = await CreateHandler(unitOfWork).Handle(new PublishEventTicketCatalogCommand { EventId = _eventId }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Id).IsEqualTo(draft.Id);
        await Assert.That(unitOfWork.TransactionBoundaries).IsEqualTo(1);
        await Assert.That(flushes.Count).IsEqualTo(2);
        await Assert.That(flushes[0]).IsEqualTo(((int)TicketCatalogStatusEnum.Retired, (int)TicketCatalogStatusEnum.Draft));
        await Assert.That(flushes[1]).IsEqualTo(((int)TicketCatalogStatusEnum.Retired, (int)TicketCatalogStatusEnum.Published));
        await Assert.That(cacheObservedCommittedUow).IsTrue();
        await _cache.Received(1).RemoveAsync($"event:detail:{_eventId}", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenPaidDraftLacksDisclosures_ReturnsValidationFailureWithoutPublication()
    {
        Actor organizer = CreateOrganizerActor();
        EventTicketCatalogVersion draft = CreateValidPaidDraftCatalog();
        var unitOfWork = new RecordingUnitOfWork();
        _events.GetAuthorizationTargetByIdAsync(_eventId, Arg.Any<CancellationToken>()).Returns(CreatePlatformEvent(organizer));
        _events.GetEventWithDetails(_eventId).Returns(CreatePlatformEvent(organizer));
        _catalogs.GetDraftCatalogForUpdateAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(draft);
        ConfigureEnabledPaidPolicy();

        var result = await CreateHandler(unitOfWork).Handle(new PublishEventTicketCatalogCommand { EventId = _eventId }, CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_validation_failed");
        await Assert.That(result.Errors.Single()).Contains("disclosures");
        await _catalogs.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenPaidDraftCommerceAuthorizationIsDenied_ThrowsBeforePublication()
    {
        Actor organizer = CreateOrganizerActor();
        EventTicketCatalogVersion draft = CreateValidPaidDraftCatalog();
        draft.UpdateCommercialDisclosures("Merchant", "Refund", "Support");
        var unitOfWork = new RecordingUnitOfWork();
        _events.GetAuthorizationTargetByIdAsync(_eventId, Arg.Any<CancellationToken>()).Returns(CreatePlatformEvent(organizer));
        _events.GetEventWithDetails(_eventId).Returns(CreatePlatformEvent(organizer));
        _catalogs.GetDraftCatalogForUpdateAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(draft);
        ConfigureEnabledPaidPolicy();
        ConfigurePaymentPlatform();
        _connections.GetActiveByScopeAsync(_tenantId, organizer.Id, "stripe", "platform-live-eu", Arg.Any<CancellationToken>())
            .Returns(CreateReadyConnection(organizer.Id));
        _authorization.AuthorizeAsync(Arg.Any<AuthorizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(AuthorizationDecision.Deny(AuthorizationProviderMetadata.Runtime));

        await Assert.ThrowsAsync<AuthorizationException>(() => CreateHandler(unitOfWork).Handle(
            new PublishEventTicketCatalogCommand { EventId = _eventId },
            CancellationToken.None));

        await _catalogs.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    [Category("WaveCPaidPublication")]
    public async Task Handle_WhenPaidDraftIsReady_PublishesAndInvalidatesCacheAfterCommit()
    {
        Actor organizer = CreateOrganizerActor();
        EventTicketCatalogVersion draft = CreateValidPaidDraftCatalog();
        draft.UpdateCommercialDisclosures("Merchant", "Refund", "Support");
        var unitOfWork = new RecordingUnitOfWork();
        var cacheObservedCommittedUow = false;
        _events.GetAuthorizationTargetByIdAsync(_eventId, Arg.Any<CancellationToken>()).Returns(CreatePlatformEvent(organizer));
        _events.GetEventWithDetails(_eventId).Returns(CreatePlatformEvent(organizer));
        _catalogs.GetDraftCatalogForUpdateAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(draft);
        ConfigurePaidPublishReady(organizer);
        _cache.RemoveAsync($"event:detail:{_eventId}", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            cacheObservedCommittedUow = unitOfWork.HasCommitted;
            return ValueTask.CompletedTask;
        });

        var result = await CreateHandler(unitOfWork).Handle(new PublishEventTicketCatalogCommand { EventId = _eventId }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(draft.TicketCatalogStatusId).IsEqualTo((int)TicketCatalogStatusEnum.Published);
        await _catalogs.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
        await Assert.That(cacheObservedCommittedUow).IsTrue();
        await _cache.Received(1).RemoveAsync($"event:detail:{_eventId}", Arg.Any<CancellationToken>());
    }

    [Test]
    [Category("WaveCPaidPublication")]
    public async Task Handle_WhenRetryReloadFindsOrganizerRemoved_FailsWithoutLastAttemptSaveOrCacheInvalidation()
    {
        Actor organizer = CreateOrganizerActor();
        EventTicketCatalogVersion firstDraft = CreateValidPaidDraftCatalog();
        EventTicketCatalogVersion retryDraft = CreateValidPaidDraftCatalog();
        firstDraft.UpdateCommercialDisclosures("Merchant", "Refund", "Support");
        retryDraft.UpdateCommercialDisclosures("Merchant", "Refund", "Support");
        var unitOfWork = new ResettingRetryUnitOfWork(() =>
        {
            _catalogs.ClearReceivedCalls();
            _cache.ClearReceivedCalls();
        });
        _events.GetAuthorizationTargetByIdAsync(_eventId, Arg.Any<CancellationToken>()).Returns(CreatePlatformEvent(organizer));
        _events.GetEventWithDetails(_eventId).Returns(CreatePlatformEvent(organizer), CreatePlatformEvent());
        _catalogs.GetDraftCatalogForUpdateAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(firstDraft, retryDraft);
        ConfigurePaidPublishReady(organizer);

        var result = await CreateHandler(unitOfWork).Handle(new PublishEventTicketCatalogCommand { EventId = _eventId }, CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_validation_failed");
        await Assert.That(result.Errors.Single()).Contains("persisted organizer actor");
        await _events.Received(2).GetEventWithDetails(_eventId);
        await _catalogs.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenUnitOfWorkRetries_ReloadsTrackedCatalogsForEachAttemptAndInvalidatesCacheOnce()
    {
        EventTicketCatalogVersion firstDraft = CreateValidDraftCatalog();
        EventTicketCatalogVersion retryDraft = CreateValidDraftCatalog();
        EventTicketCatalogVersion firstPublication = CreatePublishedCatalog(_tenantId, _eventId, 2);
        EventTicketCatalogVersion retryPublication = CreatePublishedCatalog(_tenantId, _eventId, 3);
        var unitOfWork = new RecordingUnitOfWork(attempts: 2);
        _catalogs.GetDraftCatalogForUpdateAsync(_eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(firstDraft, retryDraft);
        _catalogs.GetPublishedForUpdateAsync(_eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(firstPublication, retryPublication);
        _catalogs.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var result = await CreateHandler(unitOfWork).Handle(new PublishEventTicketCatalogCommand { EventId = _eventId }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Id).IsEqualTo(retryDraft.Id);
        await Assert.That(unitOfWork.TransactionBoundaries).IsEqualTo(1);
        await Assert.That(unitOfWork.DelegateAttempts).IsEqualTo(2);
        await _catalogs.Received(2).GetDraftCatalogForUpdateAsync(_eventId, _tenantId, Arg.Any<CancellationToken>());
        await _catalogs.Received(2).GetPublishedForUpdateAsync(_eventId, _tenantId, Arg.Any<CancellationToken>());
        await _catalogs.Received(4).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync($"event:detail:{_eventId}", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenUnitOfWorkReportsConcurrencyConflict_ReturnsConflictWithoutCacheInvalidation()
    {
        var unitOfWork = new RecordingUnitOfWork(commitFailure: new ConcurrencyConflictException(
            ConcurrencyConflictException.ConcurrentUpdate,
            "The catalog was modified by another request."));
        EventTicketCatalogVersion draft = CreateValidDraftCatalog();
        _catalogs.GetDraftCatalogForUpdateAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(draft);

        var result = await CreateHandler(unitOfWork).Handle(new PublishEventTicketCatalogCommand { EventId = _eventId }, CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_concurrency_conflict");
        await Assert.That(result.Errors).Contains("The catalog was modified by another request.");
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private PublishEventTicketCatalogCommandHandler CreateHandler(IUnitOfWork unitOfWork) =>
        new(_events, _catalogs, _eventDays, _eventSessions, _tenant, unitOfWork, CreatePreflight(), _cache);

    private PaidEventPublicationPreflightService CreatePreflight() => new(
        _events,
        _catalogs,
        _policies,
        _connections,
        _organizationTenants,
        _groupTenants,
        _authorization,
        _tenant,
        _commerceConfiguration,
        ReadyCheckoutActivation());

    private static IPaidCheckoutActivationService ReadyCheckoutActivation()
    {
        var activation = Substitute.For<IPaidCheckoutActivationService>();
        activation.EvaluateSaleControlAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new PaidCheckoutActivationResult(true, null, "active"));
        return activation;
    }

    private DomainEvent CreatePlatformEvent(Actor? organizer = null) => new()
    {
        Id = _eventId,
        TenantId = _tenantId,
        ActorId = Guid.CreateVersion7(),
        Title = "Ticketing event",
        Actor = null!,
        OrganizerActorId = organizer?.Id,
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

    private EventTicketCatalogVersion CreateDraftCatalog(int versionNumber = 1) =>
        EventTicketCatalogVersion.Create(_tenantId, _eventId, "USD", versionNumber);

    private EventTicketCatalogVersion CreateValidDraftCatalog(int versionNumber = 1)
    {
        EventTicketCatalogVersion catalog = CreateDraftCatalog(versionNumber);
        EventTicketType ticketType = CreateFreeTicket(catalog);
        catalog.AddTicketType(ticketType, capacityPool: null);
        catalog.AddEntitlement(ticketType, TicketTypeEntitlement.CreateForEvent(ticketType.Id, _tenantId, _eventId, 1));
        return catalog;
    }

    private EventTicketCatalogVersion CreateValidPaidDraftCatalog(int versionNumber = 1)
    {
        EventTicketCatalogVersion catalog = CreateDraftCatalog(versionNumber);
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

    private static EventTicketCatalogVersion CreatePublishedCatalog(Guid tenantId, Guid eventId, int versionNumber)
    {
        EventTicketCatalogVersion catalog = EventTicketCatalogVersion.Create(tenantId, eventId, "USD", versionNumber);
        EventTicketType ticketType = CreateFreeTicket(catalog);
        catalog.AddTicketType(ticketType, capacityPool: null);
        catalog.AddEntitlement(ticketType, TicketTypeEntitlement.CreateForEvent(ticketType.Id, tenantId, eventId, 1));
        catalog.Publish();
        return catalog;
    }

    private static EventTicketType CreateFreeTicket(EventTicketCatalogVersion catalog) => EventTicketType.Create(
        Guid.CreateVersion7(),
        catalog.TenantId,
        catalog.Id,
        "General admission",
        catalog.CurrencyCode,
        TicketPricingModeEnum.Free,
        fixedPrice: null,
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

    private void ConfigureEnabledPaidPolicy()
    {
        PaidEventPolicyVersion instancePolicy = PaidEventPolicyVersion.CreateDefaultInstance().CreateRevision(
            isPaymentsEnabled: true,
            allowedOrganizerKinds: [ActorTypeEnum.Organization],
            requiresLocalVerification: false,
            allowedCurrencyCodes: ["USD"],
            defaultCurrencyCode: "USD",
            refundProtections: RefundProtections(),
            currencyRiskLimits: [],
            requiresFirstPaidEventReview: false,
            farFutureReviewThresholdDays: null);
        PaidEventPolicyVersion tenantPolicy = PaidEventPolicyVersion.CreateTenant(
            _tenantId,
            isPaymentsEnabled: true,
            allowedOrganizerKinds: [ActorTypeEnum.Organization],
            requiresLocalVerification: false,
            allowedCurrencyCodes: ["USD"],
            defaultCurrencyCode: "USD",
            refundProtections: RefundProtections(),
            currencyRiskLimits: [],
            requiresFirstPaidEventReview: false,
            farFutureReviewThresholdDays: null);
        _policies.GetActiveInstanceAsync(Arg.Any<CancellationToken>()).Returns(instancePolicy);
        _policies.GetActiveTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(tenantPolicy);
    }

    private void ConfigurePaymentPlatform()
    {
        _commerceConfiguration.ProviderCode.Returns("stripe");
        _commerceConfiguration.ConnectPlatformId.Returns("platform-live-eu");
    }

    private void ConfigurePaidPublishReady(Actor organizer)
    {
        ConfigureEnabledPaidPolicy();
        ConfigurePaymentPlatform();
        _connections.GetActiveByScopeAsync(_tenantId, organizer.Id, "stripe", "platform-live-eu", Arg.Any<CancellationToken>())
            .Returns(CreateReadyConnection(organizer.Id));
        _authorization.AuthorizeAsync(
                Arg.Is<AuthorizationRequest>(request =>
                    request != null &&
                    request.ResourceKind == ResourceKinds.Event &&
                    request.ResourceId == _eventId.ToString() &&
                    request.Action == AuthorizationActions.Events.ManagePaidEventCommerce),
                Arg.Any<CancellationToken>())
            .Returns(AuthorizationDecision.Allow(AuthorizationProviderMetadata.Runtime));
    }

    private OrganizerPaymentProviderConnection CreateReadyConnection(Guid organizerActorId)
    {
        OrganizerPaymentProviderConnection connection = OrganizerPaymentProviderConnection.Create(
            Guid.CreateVersion7(),
            _tenantId,
            organizerActorId,
            "stripe",
            "platform-live-eu",
            "acct_ready",
            DateTime.UtcNow.AddMinutes(-5));
        connection.ApplyReadiness(OrganizerPaymentProviderReadinessObservation.Create(
            "BE",
            ChargeCapabilityState.Active,
            ProviderRequirementsState.Satisfied,
            ["USD"],
            DateTime.UtcNow,
            "ready-1"));
        return connection;
    }

    private static Actor CreateOrganizerActor() => new()
    {
        Id = Guid.CreateVersion7(),
        ActorTypeId = (int)ActorTypeEnum.Organization,
        ActorType = new ActorType { Id = (int)ActorTypeEnum.Organization, MasterCode = "ORGANIZATION", FullName = "Organization" },
        OrganizationId = Guid.CreateVersion7(),
        Pii = new ActorPii { DisplayName = "Organizer" }
    };

    private static PaidEventRefundProtection[] RefundProtections() => Enum.GetValues<PaidEventRefundProtection>();

    private sealed class RecordingUnitOfWork(int attempts = 1, Exception? commitFailure = null) : IUnitOfWork
    {
        public int TransactionBoundaries { get; private set; }
        public int DelegateAttempts { get; private set; }
        public bool HasCommitted { get; private set; }

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default) =>
            ExecuteInTransactionAsync<object?>(async token =>
            {
                await operation(token);
                return null;
            }, ct);

        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)
        {
            TransactionBoundaries++;
            T result = default!;
            for (var attempt = 0; attempt < attempts; attempt++)
            {
                DelegateAttempts++;
                result = await operation(ct);
            }

            if (commitFailure is not null)
            {
                throw commitFailure;
            }

            HasCommitted = true;
            return result;
        }

        public Task<T> ExecuteSerializableAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) =>
            ExecuteInTransactionAsync(operation, ct);
    }

    private sealed class ResettingRetryUnitOfWork(Action afterFirstAttempt) : IUnitOfWork
    {
        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default) =>
            ExecuteInTransactionAsync<object?>(async token =>
            {
                await operation(token);
                return null;
            }, ct);

        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)
        {
            await operation(ct);
            afterFirstAttempt();
            return await operation(ct);
        }

        public Task<T> ExecuteSerializableAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) =>
            ExecuteInTransactionAsync(operation, ct);
    }
}
