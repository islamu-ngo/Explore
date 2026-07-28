// ABOUTME: Tests direct ticket-type creation handler ownership and scoped input resolution.
// ABOUTME: Proves validation, foreign-child masking, resolver-before-mutation, and cache timing.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventTicketing;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventTicketing.Handlers.Commands;
using Explore.Application.Features.EventTicketing.Requests.Commands;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;
using DomainEvent = global::Explore.Domain.Event;

namespace Event.Application.UnitTests.Features.EventTicketing;

[Category("Phase43Ticketing")]
public sealed class CreateEventTicketTypeCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _eventId = Guid.CreateVersion7();
    private readonly IEventRepository _events = Substitute.For<IEventRepository>();
    private readonly IEventTicketCatalogRepository _catalogs = Substitute.For<IEventTicketCatalogRepository>();
    private readonly IEventDayRepository _days = Substitute.For<IEventDayRepository>();
    private readonly IEventSessionRepository _sessions = Substitute.For<IEventSessionRepository>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();

    public CreateEventTicketTypeCommandHandlerTests()
    {
        _tenant.TenantId.Returns(_tenantId);
        _events.GetAuthorizationTargetByIdAsync(_eventId, Arg.Any<CancellationToken>()).Returns(CreatePlatformEvent());
        _catalogs.UpdateAsync(Arg.Any<EventTicketCatalogVersion>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
    }

    [Test]
    public async Task Handle_WithScopedPoolAndAllEntitlements_PersistsThenInvalidatesCache()
    {
        EventTicketCatalogVersion catalog = CreateDraftCatalog();
        EventCapacityPool pool = CreatePool();
        EventDay day = CreateDay();
        EventSession session = CreateSession();
        _catalogs.GetManagementCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _catalogs.GetCapacityPoolByIdEventAndTenantAsync(pool.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(pool);
        _days.GetByIdForEventAsync(day.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(day);
        _sessions.GetByIdForEventAsync(session.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(session);

        var result = await CreateHandler().Handle(
            new CreateEventTicketTypeCommand { EventId = _eventId, TicketType = FullTicketDto(pool.Id, day.Id, session.Id) },
            CancellationToken.None);

        EventTicketType ticket = catalog.TicketTypes.Single();
        await Assert.That(result.Success).IsTrue();
        await Assert.That(ticket.Name).IsEqualTo("Verified adult admission");
        await Assert.That(ticket.FixedPriceMinor).IsEqualTo(2_500);
        await Assert.That(ticket.CapacityPoolId).IsEqualTo(pool.Id);
        await Assert.That(ticket.Entitlements.Count).IsEqualTo(3);
        Received.InOrder(() =>
        {
            _catalogs.UpdateAsync(catalog, Arg.Any<CancellationToken>());
            _cache.RemoveAsync($"event:detail:{_eventId}", Arg.Any<CancellationToken>());
        });
    }

    [Test]
    public async Task Handle_WhenPoolBelongsToAnotherEvent_ReturnsGenericNotFoundWithoutMutation()
    {
        EventTicketCatalogVersion catalog = CreateDraftCatalog();
        Guid foreignPoolId = Guid.CreateVersion7();
        _catalogs.GetManagementCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);

        var result = await CreateHandler().Handle(
            new CreateEventTicketTypeCommand { EventId = _eventId, TicketType = FreeTicketDto(capacityPoolId: foreignPoolId) },
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_not_found");
        await Assert.That(catalog.TicketTypes.Count).IsEqualTo(0);
        await _catalogs.DidNotReceive().UpdateAsync(Arg.Any<EventTicketCatalogVersion>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenScopedEntitlementResolverFails_ReturnsGenericNotFoundWithoutPartialMutation()
    {
        EventTicketCatalogVersion catalog = CreateDraftCatalog();
        Guid foreignDayId = Guid.CreateVersion7();
        _catalogs.GetManagementCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _days.GetByIdForEventAsync(foreignDayId, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(new EventDay
        {
            Id = foreignDayId,
            EventId = Guid.CreateVersion7(),
            TenantId = _tenantId,
            Event = null!,
            Tenant = null!
        });

        var result = await CreateHandler().Handle(
            new CreateEventTicketTypeCommand
            {
                EventId = _eventId,
                TicketType = FreeTicketDto(entitlements: [DayEntitlement(foreignDayId)])
            },
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_not_found");
        await Assert.That(catalog.TicketTypes.Count).IsEqualTo(0);
        await _catalogs.DidNotReceive().UpdateAsync(Arg.Any<EventTicketCatalogVersion>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenTicketTypeIsInvalid_ReturnsValidationFailureWithoutCacheInvalidation()
    {
        var result = await CreateHandler().Handle(
            new CreateEventTicketTypeCommand
            {
                EventId = _eventId,
            TicketType = new ManageEventTicketTypeDto { Name = string.Empty, Entitlements = [] }
            },
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_validation_failed");
        await _catalogs.DidNotReceive().UpdateAsync(Arg.Any<EventTicketCatalogVersion>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private CreateEventTicketTypeCommandHandler CreateHandler() => new(
        _events,
        _catalogs,
        new TicketTypeEntitlementResolver(_days, _sessions, _tenant),
        _tenant,
        _cache);

    private DomainEvent CreatePlatformEvent() => new()
    {
        Id = _eventId,
        TenantId = _tenantId,
        Title = "Ticketing event",
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

    private EventCapacityPool CreatePool() => EventCapacityPool.Create(
        _tenantId,
        _eventId,
        "Main hall",
        200,
        900,
        CapacityOversellPolicyEnum.Disallow,
        true);

    private EventDay CreateDay() => new() { Id = Guid.CreateVersion7(), EventId = _eventId, TenantId = _tenantId, Event = null!, Tenant = null! };

    private EventSession CreateSession() => new() { Id = Guid.CreateVersion7(), EventId = _eventId, TenantId = _tenantId, Event = null!, Tenant = null! };

    private static ManageEventTicketTypeDto FreeTicketDto(
        Guid? capacityPoolId = null,
        IReadOnlyList<ManageTicketTypeEntitlementDto>? entitlements = null) => new()
        {
            Name = "General admission",
            TicketPricingModeId = (int)TicketPricingModeEnum.Free,
            ParticipantDataCollectionModeId = (int)ParticipantDataCollectionModeEnum.None,
            CapacityPoolId = capacityPoolId,
            Entitlements = entitlements ?? [EventEntitlement()]
        };

    private static ManageEventTicketTypeDto FullTicketDto(Guid poolId, Guid dayId, Guid sessionId) => new()
    {
        Name = "Verified adult admission",
        TicketPricingModeId = (int)TicketPricingModeEnum.Fixed,
        FixedPriceMinor = 2_500,
        ParticipantDataCollectionModeId = (int)ParticipantDataCollectionModeEnum.PerTicketRequired,
        CapacityPoolId = poolId,
        MinimumAge = 18,
        MaximumAge = 90,
        RequiresGuardian = true,
        RequiresApproval = true,
        PerOrderLimit = 2,
        PerAccountLimit = 3,
        PerVerifiedContactLimit = 4,
        PerBookingPartyLimit = 5,
        Entitlements = [EventEntitlement(), DayEntitlement(dayId), SessionEntitlement(sessionId)]
    };

    private static ManageTicketTypeEntitlementDto EventEntitlement() => new()
    {
        EntitlementScopeTypeId = (int)EntitlementScopeTypeEnum.Event,
        IncludedQuantity = 1,
        EntitlementSelectionRuleId = (int)EntitlementSelectionRuleEnum.AllIncluded
    };

    private static ManageTicketTypeEntitlementDto DayEntitlement(Guid dayId) => new()
    {
        EntitlementScopeTypeId = (int)EntitlementScopeTypeEnum.EventDay,
        EventDayId = dayId,
        IncludedQuantity = 1,
        EntitlementSelectionRuleId = (int)EntitlementSelectionRuleEnum.FixedSelection
    };

    private static ManageTicketTypeEntitlementDto SessionEntitlement(Guid sessionId) => new()
    {
        EntitlementScopeTypeId = (int)EntitlementScopeTypeEnum.EventSession,
        EventSessionId = sessionId,
        IncludedQuantity = 1,
        EntitlementSelectionRuleId = (int)EntitlementSelectionRuleEnum.FixedSelection
    };
}
