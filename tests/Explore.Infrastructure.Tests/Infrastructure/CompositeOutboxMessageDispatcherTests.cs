// ABOUTME: Unit tests for composite outbox dispatch routing to internal side-effect handlers.
// ABOUTME: Verifies retired broker events fail closed while local fanout and provider sync still route.

using System.Text.Json;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.Events.Handlers.Commands;
using Explore.Application.Features.Management.Handlers;
using Explore.Application.Features.Management.Requests;
using Explore.Application.Models.InternalEvents;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Infrastructure.Messaging;
using Explore.Infrastructure.Services.Moderation;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Infrastructure;

public sealed class CompositeOutboxMessageDispatcherTests
{
    [Test]
    public async Task DispatchAsync_WithInternalFanoutEvent_RoutesToFanoutServiceOnly()
    {
        var fanoutService = Substitute.For<IEventPublishedNotificationFanoutService>();
        var moderationFanoutService = Substitute.For<IEventModerationNotificationFanoutService>();
        var dispatcher = CreateDispatcher(fanoutService, moderationFanoutService);
        var request = new EventPublishedNotificationFanoutRequested
        {
            TenantId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            EventTitle = "Community Iftar",
            SourceActorId = Guid.NewGuid(),
            StartDate = DateTimeOffset.UtcNow,
            PublishedAt = DateTimeOffset.UtcNow
        };

        await dispatcher.DispatchAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateType = "Event",
            AggregateId = request.EventId,
            EventType = PublishEventCommandHandler.EventPublishedNotificationFanoutRequestedEventType,
            Payload = JsonSerializer.Serialize(request)
        });

        await fanoutService.Received(1).FanoutAsync(
            Arg.Is<EventPublishedNotificationFanoutRequested>(payload =>
                payload.TenantId == request.TenantId
                && payload.EventId == request.EventId
                && payload.SourceActorId == request.SourceActorId),
            Arg.Any<CancellationToken>());
        await moderationFanoutService.DidNotReceiveWithAnyArgs().FanoutLightModerationAsync(default!, default);
        await moderationFanoutService.DidNotReceiveWithAnyArgs().FanoutHeavyRedactionAsync(default!, default);
    }

    [Test]
    public async Task DispatchAsync_WithEventReportProviderSyncEvent_RoutesToReportProviderSyncDispatcherOnly()
    {
        var fanoutService = Substitute.For<IEventPublishedNotificationFanoutService>();
        var moderationFanoutService = Substitute.For<IEventModerationNotificationFanoutService>();
        var reportProviderSyncDispatcher = Substitute.For<IReportProviderSyncDispatcher>();
        var dispatcher = CreateDispatcher(
            fanoutService,
            moderationFanoutService,
            reportProviderSyncDispatcher);
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateType = "EventReport",
            AggregateId = Guid.NewGuid(),
            EventType = EventReportOutboxMessageFactory.EventReportProviderSyncRequestedEventType,
            Payload = "{}"
        };

        await dispatcher.DispatchAsync(message);

        await reportProviderSyncDispatcher.Received(1).DispatchAsync(
            Arg.Is<OutboxMessage>(payload => payload.Id == message.Id),
            Arg.Any<CancellationToken>());
        await fanoutService.DidNotReceiveWithAnyArgs().FanoutAsync(default!, default);
        await moderationFanoutService.DidNotReceiveWithAnyArgs().FanoutLightModerationAsync(default!, default);
        await moderationFanoutService.DidNotReceiveWithAnyArgs().FanoutHeavyRedactionAsync(default!, default);
    }

    [Test]
    public async Task DispatchAsync_WithLightModerationFanoutEvent_RoutesToModerationFanoutServiceOnly()
    {
        var fanoutService = Substitute.For<IEventPublishedNotificationFanoutService>();
        var moderationFanoutService = Substitute.For<IEventModerationNotificationFanoutService>();
        var dispatcher = CreateDispatcher(fanoutService, moderationFanoutService);
        var request = new EventLightModeratedNotificationFanoutRequested
        {
            TenantId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            ModerationRecordId = Guid.NewGuid(),
            EventTitle = "Community Iftar",
            SourceActorId = Guid.NewGuid(),
            ModeratedAt = DateTimeOffset.UtcNow
        };

        await dispatcher.DispatchAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateType = "Event",
            AggregateId = request.EventId,
            EventType = EventModerationOutboxMessageFactory.EventLightModeratedNotificationFanoutRequestedEventType,
            Payload = JsonSerializer.Serialize(request)
        });

        await moderationFanoutService.Received(1).FanoutLightModerationAsync(
            Arg.Is<EventLightModeratedNotificationFanoutRequested>(payload =>
                payload.TenantId == request.TenantId
                && payload.EventId == request.EventId
                && payload.ModerationRecordId == request.ModerationRecordId),
            Arg.Any<CancellationToken>());
        await fanoutService.DidNotReceiveWithAnyArgs().FanoutAsync(default!, default);
        await moderationFanoutService.DidNotReceiveWithAnyArgs().FanoutHeavyRedactionAsync(default!, default);
    }

    [Test]
    public async Task DispatchAsync_WithHeavyRedactionFanoutEvent_RoutesToModerationFanoutServiceOnly()
    {
        var fanoutService = Substitute.For<IEventPublishedNotificationFanoutService>();
        var moderationFanoutService = Substitute.For<IEventModerationNotificationFanoutService>();
        var dispatcher = CreateDispatcher(fanoutService, moderationFanoutService);
        var request = new EventHeavyRedactedNotificationFanoutRequested
        {
            TenantId = Guid.NewGuid(),
            ModerationRecordId = Guid.NewGuid(),
            SourceActorId = Guid.NewGuid(),
            RedactedAt = DateTimeOffset.UtcNow
        };

        await dispatcher.DispatchAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateType = "Event",
            AggregateId = Guid.NewGuid(),
            EventType = EventModerationOutboxMessageFactory.EventHeavyRedactedNotificationFanoutRequestedEventType,
            Payload = JsonSerializer.Serialize(request)
        });

        await moderationFanoutService.Received(1).FanoutHeavyRedactionAsync(
            Arg.Is<EventHeavyRedactedNotificationFanoutRequested>(payload =>
                payload.TenantId == request.TenantId
                && payload.ModerationRecordId == request.ModerationRecordId
                && payload.SourceActorId == request.SourceActorId),
            Arg.Any<CancellationToken>());
        await moderationFanoutService.DidNotReceiveWithAnyArgs().FanoutLightModerationAsync(default!, default);
        await fanoutService.DidNotReceiveWithAnyArgs().FanoutAsync(default!, default);
    }

    [Test]
    public async Task DispatchAsync_WithRetiredEventPublishedEvent_Throws()
    {
        var fanoutService = Substitute.For<IEventPublishedNotificationFanoutService>();
        var moderationFanoutService = Substitute.For<IEventModerationNotificationFanoutService>();
        var dispatcher = CreateDispatcher(fanoutService, moderationFanoutService);

        await Assert.That(async () => await dispatcher.DispatchAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateType = "Event",
            AggregateId = Guid.NewGuid(),
            EventType = "EventPublished",
            Payload = "{}"
        })).Throws<InvalidOperationException>();
        await fanoutService.DidNotReceiveWithAnyArgs().FanoutAsync(default!, default);
        await moderationFanoutService.DidNotReceiveWithAnyArgs().FanoutLightModerationAsync(default!, default);
        await moderationFanoutService.DidNotReceiveWithAnyArgs().FanoutHeavyRedactionAsync(default!, default);
    }

    [Test]
    public async Task DispatchAsync_WithUnknownEventType_Throws()
    {
        var dispatcher = CreateDispatcher(
            Substitute.For<IEventPublishedNotificationFanoutService>(),
            Substitute.For<IEventModerationNotificationFanoutService>());

        await Assert.That(async () => await dispatcher.DispatchAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateType = "Event",
            AggregateId = Guid.NewGuid(),
            EventType = "UnknownEvent",
            Payload = "{}"
        })).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ReconcileDeadLetterAsync_WithManagedTenantPointer_RoutesTerminalCommand()
    {
        var mediator = Substitute.For<IMediator>();
        var dispatcher = CreateDispatcher(
            Substitute.For<IEventPublishedNotificationFanoutService>(),
            Substitute.For<IEventModerationNotificationFanoutService>(),
            mediator: mediator);
        Guid operationId = Guid.CreateVersion7();

        await dispatcher.ReconcileDeadLetterAsync(new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            AggregateType = nameof(ManagedTenantProvisioningOperation),
            AggregateId = operationId,
            EventType = ManagedTenantProvisioningOutboxEvents.ProcessRequested
        });

        await mediator.Received(1).Send(
            Arg.Is<ReconcileManagedTenantProvisioningDeadLetterCommand>(command =>
                command.OperationId == operationId),
            Arg.Any<CancellationToken>());
    }

    private static CompositeOutboxMessageDispatcher CreateDispatcher(
        IEventPublishedNotificationFanoutService fanoutService,
        IEventModerationNotificationFanoutService moderationFanoutService,
        IReportProviderSyncDispatcher? reportProviderSyncDispatcher = null,
        IMediator? mediator = null)
    {
        return new CompositeOutboxMessageDispatcher(
            fanoutService,
            moderationFanoutService,
            reportProviderSyncDispatcher ?? Substitute.For<IReportProviderSyncDispatcher>(),
            mediator ?? Substitute.For<IMediator>(),
            NullLogger<CompositeOutboxMessageDispatcher>.Instance);
    }
}
