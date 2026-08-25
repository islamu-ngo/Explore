// ABOUTME: Tests direct capacity-pool creation handler validation, ownership, and cache timing.
// ABOUTME: Proves full-field domain creation and generic missing behavior for non-platform events.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventTicketing;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventTicketing.Handlers.Commands;
using Explore.Application.Features.EventTicketing.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;
using DomainEvent = global::Explore.Domain.Event;

namespace Event.Application.UnitTests.Features.EventTicketing;

[Category("Phase43Ticketing")]
public sealed class CreateEventCapacityPoolCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _eventId = Guid.CreateVersion7();
    private readonly IEventRepository _events = Substitute.For<IEventRepository>();
    private readonly IEventTicketCatalogRepository _catalogs = Substitute.For<IEventTicketCatalogRepository>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();

    public CreateEventCapacityPoolCommandHandlerTests()
    {
        _tenant.TenantId.Returns(_tenantId);
        _events.GetAuthorizationTargetByIdAsync(_eventId, Arg.Any<CancellationToken>()).Returns(CreatePlatformEvent());
        _catalogs.AddCapacityPoolAsync(Arg.Any<EventCapacityPool>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
    }

    [Test]
    public async Task Handle_WithAllFields_PersistsThenInvalidatesCache()
    {
        var result = await CreateHandler().Handle(
            new CreateEventCapacityPoolCommand { EventId = _eventId, CapacityPool = FullPoolDto() },
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _catalogs.Received(1).AddCapacityPoolAsync(
            Arg.Is<EventCapacityPool>(pool =>
                pool.TenantId == _tenantId
                && pool.EventId == _eventId
                && pool.Name == "Main hall revised"
                && pool.MaximumQuantity == 300
                && pool.HoldDurationSeconds == 1_200
                && pool.CapacityHoldPolicyId == (int)CapacityHoldPolicyEnum.WaitlistWhenFull
                && pool.CapacityOversellPolicyId == (int)CapacityOversellPolicyEnum.Allow
                && !pool.IsActive),
            Arg.Any<CancellationToken>());
        Received.InOrder(() =>
        {
            _catalogs.AddCapacityPoolAsync(Arg.Any<EventCapacityPool>(), Arg.Any<CancellationToken>());
            _cache.RemoveAsync($"event:detail:{_eventId}", Arg.Any<CancellationToken>());
        });
    }

    [Test]
    public async Task Handle_WhenCapacityPoolIsInvalid_ReturnsValidationFailureWithoutCacheInvalidation()
    {
        var result = await CreateHandler().Handle(
            new CreateEventCapacityPoolCommand
            {
                EventId = _eventId,
                CapacityPool = new ManageEventCapacityPoolDto { Name = string.Empty, HoldDurationSeconds = 0 }
            },
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_validation_failed");
        await _catalogs.DidNotReceive().AddCapacityPoolAsync(Arg.Any<EventCapacityPool>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenEventIsNotPlatformManaged_ReturnsGenericNotFound()
    {
        _events.GetAuthorizationTargetByIdAsync(_eventId, Arg.Any<CancellationToken>())
            .Returns(CreatePlatformEvent(ParticipationHandlingModeEnum.ExternalManaged));

        var result = await CreateHandler().Handle(
            new CreateEventCapacityPoolCommand { EventId = _eventId, CapacityPool = FullPoolDto() },
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_not_found");
        await _catalogs.DidNotReceive().AddCapacityPoolAsync(Arg.Any<EventCapacityPool>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenRepositoryReportsConcurrencyConflict_ReturnsConflictWithoutCacheInvalidation()
    {
        _catalogs.AddCapacityPoolAsync(Arg.Any<EventCapacityPool>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new ConcurrencyConflictException(
                ConcurrencyConflictException.ConcurrentUpdate,
                "The capacity pool name was created by another request."));

        var result = await CreateHandler().Handle(
            new CreateEventCapacityPoolCommand { EventId = _eventId, CapacityPool = FullPoolDto() },
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_concurrency_conflict");
        await Assert.That(result.Message).IsEqualTo("Ticketing configuration was updated by another request.");
        await Assert.That(result.Errors).Contains("The capacity pool name was created by another request.");
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private CreateEventCapacityPoolCommandHandler CreateHandler() => new(_events, _catalogs, _tenant, _cache);

    private DomainEvent CreatePlatformEvent(ParticipationHandlingModeEnum mode = ParticipationHandlingModeEnum.PlatformManaged) => new()
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
            (int)mode,
            (int)AdvanceRegistrationObligationEnum.Required,
            mode == ParticipationHandlingModeEnum.PlatformManaged ? (int)IdentityAccessModeEnum.AccountRequired : null,
            guestRecoveryPolicy: null,
            DateTime.UtcNow)
    };

    private static ManageEventCapacityPoolDto FullPoolDto() => new()
    {
        Name = "Main hall revised",
        MaximumQuantity = 300,
        HoldDurationSeconds = 1_200,
        CapacityHoldPolicyId = (int)CapacityHoldPolicyEnum.WaitlistWhenFull,
        CapacityOversellPolicyId = (int)CapacityOversellPolicyEnum.Allow,
        IsActive = false
    };
}
