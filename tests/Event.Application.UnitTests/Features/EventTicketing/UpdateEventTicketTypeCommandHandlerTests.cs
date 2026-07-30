// ABOUTME: Tests direct ticket-type update handler ownership and no-partial-mutation behavior.
// ABOUTME: Proves full-field replacement, foreign masking, scoped resolver failures, and cache timing.

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
public sealed class UpdateEventTicketTypeCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _eventId = Guid.CreateVersion7();
    private readonly IEventRepository _events = Substitute.For<IEventRepository>();
    private readonly IEventTicketCatalogRepository _catalogs = Substitute.For<IEventTicketCatalogRepository>();
    private readonly IEventDayRepository _days = Substitute.For<IEventDayRepository>();
    private readonly IEventSessionRepository _sessions = Substitute.For<IEventSessionRepository>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly TicketingTestUnitOfWork _unitOfWork = new();

    public UpdateEventTicketTypeCommandHandlerTests()
    {
        _tenant.TenantId.Returns(_tenantId);
        _events.GetAuthorizationTargetByIdAsync(_eventId, Arg.Any<CancellationToken>()).Returns(CreatePlatformEvent());
        _catalogs.RemoveEntitlementsAsync(Arg.Any<IEnumerable<TicketTypeEntitlement>>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _catalogs.UpdateAsync(Arg.Any<EventTicketCatalogVersion>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
    }

    [Test]
    public async Task Handle_WithFullAuthoringFields_ReplacesDraftTicketThenInvalidatesCache()
    {
        EventTicketCatalogVersion catalog = CreateDraftCatalog();
        EventTicketType ticket = AddFreeTicket(catalog);
        TicketTypeEntitlement existingEntitlement = ticket.Entitlements.Single();
        EventCapacityPool pool = CreatePool();
        _catalogs.GetDraftCatalogForUpdateAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _catalogs.GetActiveCapacityPoolForUpdateAsync(pool.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(pool);

        var result = await CreateHandler().Handle(
            new UpdateEventTicketTypeCommand { EventId = _eventId, TicketTypeId = ticket.Id, TicketType = FullTicketDto(pool.Id) },
            CancellationToken.None);

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
        Received.InOrder(() =>
        {
            _catalogs.RemoveEntitlementsAsync(
                Arg.Is<IEnumerable<TicketTypeEntitlement>>(entitlements => entitlements.SequenceEqual(new[] { existingEntitlement })),
                Arg.Any<CancellationToken>());
            _catalogs.UpdateAsync(catalog, Arg.Any<CancellationToken>());
            _cache.RemoveAsync($"event:detail:{_eventId}", Arg.Any<CancellationToken>());
        });
    }

    [Test]
    public async Task Handle_WhenRepositoryReportsConcurrencyConflict_ReturnsConflictWithoutCacheInvalidation()
    {
        EventTicketCatalogVersion catalog = CreateDraftCatalog();
        EventTicketType ticket = AddFreeTicket(catalog);
        _catalogs.GetDraftCatalogForUpdateAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _catalogs.UpdateAsync(Arg.Any<EventTicketCatalogVersion>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new ConcurrencyConflictException(
                ConcurrencyConflictException.ConcurrentUpdate,
                "The ticket type was modified by another request."));

        var result = await CreateHandler().Handle(
            new UpdateEventTicketTypeCommand { EventId = _eventId, TicketTypeId = ticket.Id, TicketType = FreeTicketDto("Revised admission") },
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_concurrency_conflict");
        await Assert.That(result.Errors).Contains("The ticket type was modified by another request.");
        await _catalogs.Received(1).UpdateAsync(catalog, Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenPoolBelongsToAnotherEvent_ReturnsGenericNotFoundWithoutChangingTicket()
    {
        EventTicketCatalogVersion catalog = CreateDraftCatalog();
        EventTicketType ticket = AddFreeTicket(catalog);
        Guid foreignPoolId = Guid.CreateVersion7();
        _catalogs.GetDraftCatalogForUpdateAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);

        var result = await CreateHandler().Handle(
            new UpdateEventTicketTypeCommand
            {
                EventId = _eventId,
                TicketTypeId = ticket.Id,
                TicketType = FreeTicketDto(capacityPoolId: foreignPoolId)
            },
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_not_found");
        await Assert.That(ticket.Name).IsEqualTo("General admission");
        await _catalogs.DidNotReceive().UpdateAsync(Arg.Any<EventTicketCatalogVersion>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenScopedEntitlementResolverFails_DoesNotPartiallyMutateTicket()
    {
        EventTicketCatalogVersion catalog = CreateDraftCatalog();
        EventTicketType ticket = AddFreeTicket(catalog);
        Guid foreignSessionId = Guid.CreateVersion7();
        _catalogs.GetDraftCatalogForUpdateAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _sessions.GetByIdForEventAsync(foreignSessionId, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(new EventSession
        {
            Id = foreignSessionId,
            EventId = Guid.CreateVersion7(),
            TenantId = _tenantId,
            Event = null!,
            Tenant = null!
        });

        var result = await CreateHandler().Handle(
            new UpdateEventTicketTypeCommand
            {
                EventId = _eventId,
                TicketTypeId = ticket.Id,
                TicketType = FreeTicketDto("Changed admission", [SessionEntitlement(foreignSessionId)])
            },
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_not_found");
        await Assert.That(ticket.Name).IsEqualTo("General admission");
        await Assert.That(ticket.Entitlements.Single().TargetEventId).IsEqualTo(_eventId);
        await _catalogs.DidNotReceive().UpdateAsync(Arg.Any<EventTicketCatalogVersion>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private UpdateEventTicketTypeCommandHandler CreateHandler() => new(
        _events,
        _catalogs,
        new TicketTypeEntitlementResolver(_days, _sessions, _tenant),
        _tenant,
        _unitOfWork,
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

    private EventTicketType AddFreeTicket(EventTicketCatalogVersion catalog)
    {
        EventTicketType ticket = EventTicketType.Create(
            Guid.CreateVersion7(),
            _tenantId,
            catalog.Id,
            "General admission",
            "USD",
            TicketPricingModeEnum.Free,
            null,
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
        catalog.AddTicketType(ticket, null);
        catalog.AddEntitlement(ticket, TicketTypeEntitlement.CreateForEvent(ticket.Id, _tenantId, _eventId, 1));
        return ticket;
    }

    private EventCapacityPool CreatePool() => EventCapacityPool.Create(
        _tenantId,
        _eventId,
        "Main hall",
        200,
        900,
        CapacityHoldPolicyEnum.TimedHoldOnSelection,
        CapacityOversellPolicyEnum.Disallow,
        true);

    private static ManageEventTicketTypeDto FreeTicketDto(
        string name = "General admission",
        IReadOnlyList<ManageTicketTypeEntitlementDto>? entitlements = null,
        Guid? capacityPoolId = null) => new()
        {
            Name = name,
            TicketPricingModeId = (int)TicketPricingModeEnum.Free,
            ParticipantDataCollectionModeId = (int)ParticipantDataCollectionModeEnum.None,
            CapacityPoolId = capacityPoolId,
            Entitlements = entitlements ?? [EventEntitlement()]
        };

    private static ManageEventTicketTypeDto FullTicketDto(Guid poolId) => new()
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
        Entitlements = [EventEntitlement()]
    };

    private static ManageTicketTypeEntitlementDto EventEntitlement() => new()
    {
        EntitlementScopeTypeId = (int)EntitlementScopeTypeEnum.Event,
        IncludedQuantity = 1,
        EntitlementSelectionRuleId = (int)EntitlementSelectionRuleEnum.AllIncluded
    };

    private static ManageTicketTypeEntitlementDto SessionEntitlement(Guid sessionId) => new()
    {
        EntitlementScopeTypeId = (int)EntitlementScopeTypeEnum.EventSession,
        EventSessionId = sessionId,
        IncludedQuantity = 1,
        EntitlementSelectionRuleId = (int)EntitlementSelectionRuleEnum.FixedSelection
    };
}
