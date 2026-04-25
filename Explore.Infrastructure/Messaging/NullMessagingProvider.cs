// ABOUTME: No-op messaging provider used when messaging is disabled or provider resolution fails.
// ABOUTME: All publish/subscribe calls are silently ignored with debug logging.

namespace Explore.Infrastructure.Messaging;

using Explore.Application.Contracts.Infrastructure;
using Microsoft.Extensions.Logging;

public sealed class NullMessagingProvider : IMessagingProvider
{
    private readonly ILogger<NullMessagingProvider> _logger;

    public NullMessagingProvider(ILogger<NullMessagingProvider> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<T>(T message, string channel, CancellationToken cancellationToken = default) where T : class
    {
        _logger.LogDebug("Messaging disabled: Message of type {MessageType} not published to channel {Channel}", typeof(T).Name, channel);
        return Task.CompletedTask;
    }

    public Task BulkPublishAsync<T>(IEnumerable<T> messages, string channel, CancellationToken cancellationToken = default) where T : class
    {
        var count = messages.Count();
        _logger.LogDebug("Messaging disabled: {Count} messages of type {MessageType} not published to channel {Channel}", count, typeof(T).Name, channel);
        return Task.CompletedTask;
    }

    public Task SubscribeAsync<T>(Func<T, Task> messageReceived, Action<Exception> errorReceived, string channel, string? group = null, CancellationToken cancellationToken = default) where T : class
    {
        _logger.LogDebug("Messaging disabled: Subscription to channel {Channel} (group: {Group}) for message type {MessageType} not created", channel, group ?? "none", typeof(T).Name);
        return Task.CompletedTask;
    }
}
