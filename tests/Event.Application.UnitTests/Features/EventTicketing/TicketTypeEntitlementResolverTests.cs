// ABOUTME: Tests sequential ticket entitlement target resolution before catalog mutation.
// ABOUTME: Proves ordering, ownership boundaries, short-circuiting, and cancellation propagation.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventTicketing;
using Explore.Application.Exceptions;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventTicketing;

[Category("Phase43Ticketing")]
public sealed class TicketTypeEntitlementResolverTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _eventId = Guid.CreateVersion7();
    private readonly Guid _ticketTypeId = Guid.CreateVersion7();
    private readonly IEventDayRepository _days = Substitute.For<IEventDayRepository>();
    private readonly IEventSessionRepository _sessions = Substitute.For<IEventSessionRepository>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();

    public TicketTypeEntitlementResolverTests()
    {
        _tenant.TenantId.Returns(_tenantId);
    }

    [Test]
    public async Task ResolveAsync_PreservesInputOrder()
    {
        Guid dayId = Guid.CreateVersion7();
        Guid sessionId = Guid.CreateVersion7();
        _days.GetByIdForEventAsync(dayId, _eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EventDay?>(CreateDay(dayId, _eventId, _tenantId)));
        _sessions.GetByIdForEventAsync(sessionId, _eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EventSession?>(CreateSession(sessionId, _eventId, _tenantId)));

        var resolved = await CreateResolver().ResolveAsync(
            _ticketTypeId,
            [DayEntitlement(dayId), EventEntitlement(), SessionEntitlement(sessionId)],
            _eventId,
            CancellationToken.None);

        await Assert.That(resolved.Count).IsEqualTo(3);
        await Assert.That(resolved[0].EventDayId).IsEqualTo(dayId);
        await Assert.That(resolved[1].EntitlementScopeTypeId).IsEqualTo((int)EntitlementScopeTypeEnum.Event);
        await Assert.That(resolved[2].EventSessionId).IsEqualTo(sessionId);
    }

    [Test]
    public async Task ResolveAsync_StopsAtFirstInvalidTarget()
    {
        Guid firstDayId = Guid.CreateVersion7();
        Guid invalidDayId = Guid.CreateVersion7();
        Guid sessionId = Guid.CreateVersion7();
        _days.GetByIdForEventAsync(firstDayId, _eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EventDay?>(CreateDay(firstDayId, _eventId, _tenantId)));
        _days.GetByIdForEventAsync(invalidDayId, _eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EventDay?>(null));

        await Assert.ThrowsAsync<TicketingNotFoundException>(() => CreateResolver().ResolveAsync(
            _ticketTypeId,
            [DayEntitlement(firstDayId), DayEntitlement(invalidDayId), SessionEntitlement(sessionId)],
            _eventId,
            CancellationToken.None));

        await _sessions.DidNotReceive().GetByIdForEventAsync(
            sessionId,
            _eventId,
            _tenantId,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ResolveAsync_RejectsTargetFromAnotherEvent()
    {
        Guid dayId = Guid.CreateVersion7();
        _days.GetByIdForEventAsync(dayId, _eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EventDay?>(CreateDay(dayId, Guid.CreateVersion7(), _tenantId)));

        await Assert.ThrowsAsync<TicketingNotFoundException>(() => CreateResolver().ResolveAsync(
            _ticketTypeId,
            [DayEntitlement(dayId)],
            _eventId,
            CancellationToken.None));
    }

    [Test]
    public async Task ResolveAsync_RejectsTargetFromAnotherTenant()
    {
        Guid sessionId = Guid.CreateVersion7();
        _sessions.GetByIdForEventAsync(sessionId, _eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EventSession?>(CreateSession(sessionId, _eventId, Guid.CreateVersion7())));

        await Assert.ThrowsAsync<TicketingNotFoundException>(() => CreateResolver().ResolveAsync(
            _ticketTypeId,
            [SessionEntitlement(sessionId)],
            _eventId,
            CancellationToken.None));
    }

    [Test]
    public async Task ResolveAsync_PassesCancellationAndStopsBeforeNextTarget()
    {
        Guid dayId = Guid.CreateVersion7();
        Guid sessionId = Guid.CreateVersion7();
        using var cancellation = new CancellationTokenSource();
        CancellationToken observedToken = default;
        _days.GetByIdForEventAsync(dayId, _eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                observedToken = callInfo.Arg<CancellationToken>();
                cancellation.Cancel();
                return Task.FromResult<EventDay?>(CreateDay(dayId, _eventId, _tenantId));
            });

        await Assert.ThrowsAsync<OperationCanceledException>(() => CreateResolver().ResolveAsync(
            _ticketTypeId,
            [DayEntitlement(dayId), SessionEntitlement(sessionId)],
            _eventId,
            cancellation.Token));

        await Assert.That(observedToken).IsEqualTo(cancellation.Token);
        await _sessions.DidNotReceive().GetByIdForEventAsync(
            sessionId,
            _eventId,
            _tenantId,
            Arg.Any<CancellationToken>());
    }

    private TicketTypeEntitlementResolver CreateResolver() => new(_days, _sessions, _tenant);

    private static EventDay CreateDay(Guid id, Guid eventId, Guid tenantId) => new()
    {
        Id = id,
        EventId = eventId,
        TenantId = tenantId,
        Event = null!,
        Tenant = null!
    };

    private static EventSession CreateSession(Guid id, Guid eventId, Guid tenantId) => new()
    {
        Id = id,
        EventId = eventId,
        TenantId = tenantId,
        Event = null!,
        Tenant = null!
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
