// ABOUTME: Provider-agnostic contract for messaging operations across the application.
// ABOUTME: Implemented by RabbitMqMessagingProvider, InMemoryMessagingProvider, and NullMessagingProvider.

namespace Explore.Application.Contracts.Infrastructure;

public interface IMessagingProvider
{
    Task PublishAsync<T>(
        T message,
        string channel,
        CancellationToken cancellationToken = default) where T : class;

    Task BulkPublishAsync<T>(
        IEnumerable<T> messages,
        string channel,
        CancellationToken cancellationToken = default) where T : class;

    Task SubscribeAsync<T>(
        Func<T, Task> messageReceived,
        Action<Exception> errorReceived,
        string channel,
        string? group = null,
        CancellationToken cancellationToken = default) where T : class;
}
