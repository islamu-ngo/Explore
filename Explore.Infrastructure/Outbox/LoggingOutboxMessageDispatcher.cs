// ABOUTME: Default no-op outbox dispatcher that logs a warning for every message.
// ABOUTME: Acts as a placeholder until real dispatchers are registered for specific event types.

using Explore.Application.Contracts.Infrastructure;
using Explore.Domain;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Outbox;

/// <summary>
/// Default dispatcher that logs a warning and returns successfully.
/// Register a real <see cref="IOutboxMessageDispatcher"/> implementation to replace this
/// when actual side-effect handlers (email, webhook, integration) are wired up.
/// </summary>
public sealed class LoggingOutboxMessageDispatcher(ILogger<LoggingOutboxMessageDispatcher> logger) : IOutboxMessageDispatcher
{
    public Task DispatchAsync(OutboxMessage message, CancellationToken ct = default)
    {
        logger.LogWarning(
            "No real dispatcher registered for outbox message. EventType={EventType}, AggregateType={AggregateType}, AggregateId={AggregateId}, MessageId={MessageId}",
            message.EventType,
            message.AggregateType,
            message.AggregateId,
            message.Id);

        return Task.CompletedTask;
    }
}
