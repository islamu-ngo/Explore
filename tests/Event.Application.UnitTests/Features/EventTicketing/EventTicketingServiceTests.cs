// ABOUTME: Focused Application tests for platform-managed ticket catalog authoring and error boundaries.
// ABOUTME: Proves draft mutation, scoped child validation, preflight mapping, and capacity safety behavior.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventTicketing;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventTicketing;

[Category("Phase43Ticketing")]
public sealed class EventTicketingServiceTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _eventId = Guid.CreateVersion7();
    private readonly IEventRepository _events = Substitute.For<IEventRepository>();
    private readonly IEventTicketCatalogRepository _catalogs = Substitute.For<IEventTicketCatalogRepository>();
    private readonly IEventDayRepository _days = Substitute.For<IEventDayRepository>();
    private readonly IEventSessionRepository _sessions = Substitute.For<IEventSessionRepository>();
    private readonly EventTicketingService _service;

    public EventTicketingServiceTests()
    {
        var tenant = Substitute.For<ITenantContext>();
        tenant.TenantId.Returns(_tenantId);
        var user = Substitute.For<ICurrentUserService>();
        user.UserId.Returns(Guid.CreateVersion7());
        _events.GetAuthorizationTargetByIdAsync(_eventId, Arg.Any<CancellationToken>())
            .Returns(CreatePlatformEvent());
        _catalogs.AddAsync(Arg.Any<EventTicketCatalogVersion>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _catalogs.UpdateAsync(Arg.Any<EventTicketCatalogVersion>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _catalogs.UpdateCapacityPoolAsync(Arg.Any<EventCapacityPool>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _service = new EventTicketingService(
            _events,
            _catalogs,
            _days,
            _sessions,
            tenant,
            user,
            Substitute.For<HybridCache>());
    }

    [Test]
    public async Task CloneDraft_WithPublishedCatalogAndNoDraft_ClonesPublishedCatalog()
    {
        EventTicketCatalogVersion published = CreatePublishedCatalog();
        _catalogs.GetPublishedCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(published);
        _catalogs.GetManagementCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns((EventTicketCatalogVersion?)null);

        var result = await _service.Handle(new CloneEventTicketCatalogDraftCommand { EventId = _eventId }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _catalogs.Received(1).AddAsync(
            Arg.Is<EventTicketCatalogVersion>(catalog =>
                catalog.TicketCatalogStatusId == (int)TicketCatalogStatusEnum.Draft
                && catalog.VersionNumber == published.VersionNumber + 1
                && catalog.TicketTypes.Single().Id != published.TicketTypes.Single().Id),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CloneDraft_WithExistingDraft_ReturnsTicketingValidationFailure()
    {
        _catalogs.GetPublishedCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(CreatePublishedCatalog());
        _catalogs.GetManagementCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(CreateDraftCatalog());

        var result = await _service.Handle(new CloneEventTicketCatalogDraftCommand { EventId = _eventId }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_validation_failed");
        await Assert.That(result.Errors).Contains("A ticket catalog draft already exists.");
        await _catalogs.DidNotReceive().AddAsync(Arg.Any<EventTicketCatalogVersion>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateTicketType_WithFullAuthoringFields_ReplacesTheDraftTicket()
    {
        EventTicketCatalogVersion catalog = CreateDraftCatalog();
        EventTicketType ticket = AddFreeTicket(catalog);
        EventCapacityPool pool = CreatePool();
        _catalogs.GetManagementCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _catalogs.GetCapacityPoolByIdEventAndTenantAsync(pool.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(pool);

        var result = await _service.Handle(new UpdateEventTicketTypeCommand
        {
            EventId = _eventId,
            TicketTypeId = ticket.Id,
            TicketType = new EventTicketTypeDto
            {
                Name = "Verified adult admission",
                TicketPricingModeId = (int)TicketPricingModeEnum.Fixed,
                FixedPriceMinor = 2_500,
                ParticipantDataCollectionModeId = (int)ParticipantDataCollectionModeEnum.PerTicketRequired,
                CapacityPoolId = pool.Id,
                MinimumAge = 18,
                MaximumAge = 90,
                RequiresGuardian = true,
                RequiresApproval = true,
                PerOrderLimit = 2,
                PerAccountLimit = 3,
                PerVerifiedContactLimit = 4,
                PerBookingPartyLimit = 5,
                Entitlements = [EventEntitlement()]
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(ticket.Name).IsEqualTo("Verified adult admission");
        await Assert.That(ticket.TicketPricingModeId).IsEqualTo((int)TicketPricingModeEnum.Fixed);
        await Assert.That(ticket.FixedPriceMinor).IsEqualTo(2_500);
        await Assert.That(ticket.ParticipantDataCollectionModeId).IsEqualTo((int)ParticipantDataCollectionModeEnum.PerTicketRequired);
        await Assert.That(ticket.CapacityPoolId).IsEqualTo(pool.Id);
        await Assert.That(ticket.MinimumAge).IsEqualTo(18);
        await Assert.That(ticket.MaximumAge).IsEqualTo(90);
        await Assert.That(ticket.RequiresGuardian).IsTrue();
        await Assert.That(ticket.RequiresApproval).IsTrue();
        await Assert.That(ticket.PerOrderLimit).IsEqualTo(2);
        await Assert.That(ticket.PerAccountLimit).IsEqualTo(3);
        await Assert.That(ticket.PerVerifiedContactLimit).IsEqualTo(4);
        await Assert.That(ticket.PerBookingPartyLimit).IsEqualTo(5);
        await Assert.That(ticket.Entitlements.Single().TargetEventId).IsEqualTo(_eventId);
        await _catalogs.Received(1).UpdateAsync(catalog, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateCapacityPool_WithValidDraftEvent_UpdatesAllPoolFields()
    {
        EventCapacityPool pool = CreatePool();
        _catalogs.GetCapacityPoolByIdEventAndTenantAsync(pool.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(pool);

        var result = await _service.Handle(new UpdateEventCapacityPoolCommand
        {
            EventId = _eventId,
            CapacityPoolId = pool.Id,
            CapacityPool = new EventCapacityPoolDto
            {
                Name = "Main hall revised",
                MaximumQuantity = 300,
                HoldDurationSeconds = 1_200,
                CapacityOversellPolicyId = (int)CapacityOversellPolicyEnum.Allow,
                IsActive = false
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(pool.Name).IsEqualTo("Main hall revised");
        await Assert.That(pool.MaximumQuantity).IsEqualTo(300);
        await Assert.That(pool.HoldDurationSeconds).IsEqualTo(1_200);
        await Assert.That(pool.CapacityOversellPolicyId).IsEqualTo((int)CapacityOversellPolicyEnum.Allow);
        await Assert.That(pool.IsActive).IsFalse();
        await _catalogs.Received(1).UpdateCapacityPoolAsync(pool, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteCapacityPool_WhenAnActiveTicketReferencesIt_ReturnsTicketingValidationFailure()
    {
        EventTicketCatalogVersion catalog = CreateDraftCatalog();
        EventCapacityPool pool = CreatePool();
        AddFreeTicket(catalog, pool);
        _catalogs.GetCapacityPoolByIdEventAndTenantAsync(pool.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(pool);
        _catalogs.GetManagementCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);

        var result = await _service.Handle(new DeleteEventCapacityPoolCommand
        {
            EventId = _eventId,
            CapacityPoolId = pool.Id
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_validation_failed");
        await Assert.That(pool.IsDeleted).IsFalse();
        await _catalogs.DidNotReceive().UpdateCapacityPoolAsync(Arg.Any<EventCapacityPool>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateTicketType_WithPoolFromAnotherEvent_ReturnsGenericNotFound()
    {
        _catalogs.GetManagementCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(CreateDraftCatalog());
        Guid foreignPoolId = Guid.CreateVersion7();
        _catalogs.GetCapacityPoolByIdEventAndTenantAsync(foreignPoolId, _eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns((EventCapacityPool?)null);

        var result = await _service.Handle(new CreateEventTicketTypeCommand
        {
            EventId = _eventId,
            TicketType = FreeTicketDto(capacityPoolId: foreignPoolId)
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_not_found");
    }

    [Test]
    public async Task CreateTicketType_WithDayFromAnotherEvent_ReturnsGenericNotFound()
    {
        _catalogs.GetManagementCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(CreateDraftCatalog());
        Guid foreignDayId = Guid.CreateVersion7();
        _days.GetById(foreignDayId).Returns(new EventDay
        {
            Id = foreignDayId,
            EventId = Guid.CreateVersion7(),
            TenantId = _tenantId,
            Event = null!,
            Tenant = null!
        });

        var result = await _service.Handle(new CreateEventTicketTypeCommand
        {
            EventId = _eventId,
            TicketType = FreeTicketDto(entitlements:
            [
                new TicketTypeEntitlementDto
                {
                    EntitlementScopeTypeId = (int)EntitlementScopeTypeEnum.EventDay,
                    EventDayId = foreignDayId,
                    IncludedQuantity = 1,
                    EntitlementSelectionRuleId = (int)EntitlementSelectionRuleEnum.FixedSelection
                }
            ])
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_not_found");
    }

    [Test]
    public async Task CreateTicketType_WithSessionFromAnotherEvent_ReturnsGenericNotFound()
    {
        _catalogs.GetManagementCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(CreateDraftCatalog());
        Guid foreignSessionId = Guid.CreateVersion7();
        _sessions.GetById(foreignSessionId).Returns(new EventSession
        {
            Id = foreignSessionId,
            EventId = Guid.CreateVersion7(),
            TenantId = _tenantId,
            Event = null!,
            Tenant = null!
        });

        var result = await _service.Handle(new CreateEventTicketTypeCommand
        {
            EventId = _eventId,
            TicketType = FreeTicketDto(entitlements:
            [
                new TicketTypeEntitlementDto
                {
                    EntitlementScopeTypeId = (int)EntitlementScopeTypeEnum.EventSession,
                    EventSessionId = foreignSessionId,
                    IncludedQuantity = 1,
                    EntitlementSelectionRuleId = (int)EntitlementSelectionRuleEnum.FixedSelection
                }
            ])
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_not_found");
    }

    [Test]
    public async Task CreateTicketType_WithInvalidDto_ReturnsTicketingValidationFailure()
    {
        _catalogs.GetManagementCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(CreateDraftCatalog());

        var result = await _service.Handle(new CreateEventTicketTypeCommand
        {
            EventId = _eventId,
            TicketType = new EventTicketTypeDto
            {
                Name = string.Empty,
                Entitlements = []
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_validation_failed");
        await _catalogs.DidNotReceive().UpdateAsync(Arg.Any<EventTicketCatalogVersion>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PublishCatalog_WhenDomainPreflightFails_MapsToTicketingValidationFailure()
    {
        EventTicketCatalogVersion draft = CreateDraftCatalog();
        _catalogs.GetManagementCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(draft);
        _catalogs.PublishDraftReplacingCurrentAsync(draft, _eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(call => PublishWithPreflight(call.Arg<EventTicketCatalogVersion>()));

        var result = await _service.Handle(new PublishEventTicketCatalogCommand { EventId = _eventId }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_validation_failed");
        await Assert.That(result.Errors).Contains("A published ticket catalog requires at least one ticket type.");
    }

    private Explore.Domain.Event CreatePlatformEvent() => new()
    {
        Id = _eventId,
        TenantId = _tenantId,
        Title = "Platform event",
        Actor = null!,
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

    private EventTicketCatalogVersion CreateDraftCatalog() => EventTicketCatalogVersion.Create(_tenantId, _eventId, "USD", 1);

    private EventTicketCatalogVersion CreatePublishedCatalog()
    {
        EventTicketCatalogVersion catalog = CreateDraftCatalog();
        AddFreeTicket(catalog);
        catalog.Publish();
        return catalog;
    }

    private EventTicketType AddFreeTicket(EventTicketCatalogVersion catalog, EventCapacityPool? pool = null)
    {
        EventTicketType ticket = EventTicketType.Create(
            _tenantId,
            catalog.Id,
            "General admission",
            "USD",
            TicketPricingModeEnum.Free,
            null,
            null,
            null,
            ParticipantDataCollectionModeEnum.None,
            pool?.Id,
            null,
            null,
            false,
            false,
            null,
            null,
            null,
            null);
        catalog.AddTicketType(ticket, pool);
        catalog.AddEntitlement(ticket, TicketTypeEntitlement.CreateForEvent(ticket.Id, _tenantId, _eventId, 1));
        return ticket;
    }

    private EventCapacityPool CreatePool() => EventCapacityPool.Create(
        _tenantId,
        _eventId,
        "Main hall",
        200,
        900,
        CapacityOversellPolicyEnum.Disallow,
        true);

    private static EventTicketTypeDto FreeTicketDto(
        Guid? capacityPoolId = null,
        IReadOnlyList<TicketTypeEntitlementDto>? entitlements = null) => new()
        {
            Name = "General admission",
            TicketPricingModeId = (int)TicketPricingModeEnum.Free,
            ParticipantDataCollectionModeId = (int)ParticipantDataCollectionModeEnum.None,
            CapacityPoolId = capacityPoolId,
            Entitlements = entitlements ?? [EventEntitlement()]
        };

    private static TicketTypeEntitlementDto EventEntitlement() => new()
    {
        EntitlementScopeTypeId = (int)EntitlementScopeTypeEnum.Event,
        IncludedQuantity = 1,
        EntitlementSelectionRuleId = (int)EntitlementSelectionRuleEnum.AllIncluded
    };

    private static Task PublishWithPreflight(EventTicketCatalogVersion draft)
    {
        draft.Publish();
        return Task.CompletedTask;
    }
}
