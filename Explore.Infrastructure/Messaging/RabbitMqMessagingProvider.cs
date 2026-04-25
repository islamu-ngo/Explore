// ABOUTME: RabbitMQ messaging provider using MQContract library for contract-first messaging.
// ABOUTME: Singleton connection with OpenTelemetry, Polly resilience, and compression middleware.

namespace Explore.Infrastructure.Messaging;

using Explore.Application.Contracts.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQContract;
using MQContract.Interfaces;
using MQContract.RabbitMQ;
using Polly;
using RabbitMQ.Client;

/// <summary>
/// RabbitMQ messaging provider using MQContract abstraction.
/// Connection is initialized lazily on first use; config is resolved via a short-lived scope (IServiceScopeFactory)
/// because IMessagingConfigResolver is Scoped and this provider is a Singleton.
/// </summary>
public sealed class RabbitMqMessagingProvider : IMessagingProvider, IDisposable
{
    private readonly ILogger<RabbitMqMessagingProvider> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private IContractConnection? _contractConnection;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _disposed;

    public RabbitMqMessagingProvider(ILogger<RabbitMqMessagingProvider> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task PublishAsync<T>(T message, string channel, CancellationToken cancellationToken = default) where T : class
    {
        var connection = await GetConnectionAsync(cancellationToken);
        await connection.PublishAsync(message, channel: channel);
        _logger.LogDebug("Published message of type {MessageType} to channel {Channel}", typeof(T).Name, channel);
    }

    public async Task BulkPublishAsync<T>(IEnumerable<T> messages, string channel, CancellationToken cancellationToken = default) where T : class
    {
        var connection = await GetConnectionAsync(cancellationToken);
        var messageList = messages.ToList();
        await connection.BulkPublishAsync(messageList.Select(m => (m, (MQContract.Messages.MessageHeader?)null)), channel: channel);
        _logger.LogDebug("Bulk published {Count} messages of type {MessageType} to channel {Channel}", messageList.Count, typeof(T).Name, channel);
    }

    public async Task SubscribeAsync<T>(Func<T, Task> messageReceived, Action<Exception> errorReceived, string channel, string? group = null, CancellationToken cancellationToken = default) where T : class
    {
        var connection = await GetConnectionAsync(cancellationToken);
        await connection.SubscribeAsync<T>(
            messageReceived: async (msg) => await messageReceived(msg.Message!),
            errorReceived: (error) => errorReceived(error),
            channel: channel,
            group: group,
            cancellationToken: cancellationToken);
        _logger.LogDebug("Subscribed to channel {Channel} (group: {Group}) for message type {MessageType}", channel, group ?? "none", typeof(T).Name);
    }

    private async Task<IContractConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_contractConnection is not null)
            return _contractConnection;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_contractConnection is not null)
                return _contractConnection;

            using var scope = _scopeFactory.CreateScope();
            var config = await scope.ServiceProvider
                .GetRequiredService<IMessagingConfigResolver>()
                .ResolveAsync(cancellationToken);

            var factory = new ConnectionFactory
            {
                HostName = config.HostName ?? "localhost",
                Port = config.Port,
                UserName = config.UserName ?? "guest",
                Password = config.Password ?? "guest",
                VirtualHost = config.VirtualHost ?? "/",
                MaxInboundMessageBodySize = (uint)config.MaxInboundMessageBodySize
            };

            var serviceConnection = new Connection(factory);
            var contractConnection = ContractConnection.Instance(serviceConnection);

            if (config.EnableOpenTelemetry)
            {
                contractConnection.EnableOpenTelemetry(
                    activitySource: "MQContract",
                    linkActivitiesAcrossSystems: true
                );
            }

            contractConnection.RegisterResiliencePolicy(
                (config.RetryAttempts, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)))
            );

            _contractConnection = contractConnection;
            _logger.LogInformation("RabbitMQ connection initialized (Host: {Host}, Port: {Port}, VirtualHost: {VHost})", factory.HostName, factory.Port, factory.VirtualHost);

            return _contractConnection;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        (_contractConnection as IDisposable)?.Dispose();
        _initLock.Dispose();
        _disposed = true;
    }
}
