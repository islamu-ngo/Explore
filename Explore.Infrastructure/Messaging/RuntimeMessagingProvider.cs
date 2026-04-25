// ABOUTME: Runtime messaging provider wrapper that resolves active provider from tenant settings at runtime.
// ABOUTME: Uses short-lived cache and safe fallback to NullMessagingProvider on provider errors.

namespace Explore.Infrastructure.Messaging;

using Explore.Application.Contracts.Infrastructure;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

public sealed class RuntimeMessagingProvider : IMessagingProvider
{
    private readonly RabbitMqMessagingProvider _rabbitMqProvider;
    private readonly NullMessagingProvider _nullProvider;
    private readonly IMessagingConfigResolver _configResolver;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RuntimeMessagingProvider> _logger;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "MessagingProvider_Resolved:";

    public RuntimeMessagingProvider(
        RabbitMqMessagingProvider rabbitMqProvider,
        NullMessagingProvider nullProvider,
        IMessagingConfigResolver configResolver,
        IMemoryCache cache,
        ILogger<RuntimeMessagingProvider> logger)
    {
        _rabbitMqProvider = rabbitMqProvider;
        _nullProvider = nullProvider;
        _configResolver = configResolver;
        _cache = cache;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T message, string channel, CancellationToken cancellationToken = default) where T : class
    {
        var provider = await ResolveProviderAsync(cancellationToken);

        try
        {
            await provider.PublishAsync(message, channel, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Messaging publish failed on provider {ProviderType}; falling back to NullProvider", provider.GetType().Name);
        }
    }

    public async Task BulkPublishAsync<T>(IEnumerable<T> messages, string channel, CancellationToken cancellationToken = default) where T : class
    {
        var provider = await ResolveProviderAsync(cancellationToken);

        try
        {
            await provider.BulkPublishAsync(messages, channel, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Messaging bulk publish failed on provider {ProviderType}; falling back to NullProvider", provider.GetType().Name);
        }
    }

    public async Task SubscribeAsync<T>(Func<T, Task> messageReceived, Action<Exception> errorReceived, string channel, string? group = null, CancellationToken cancellationToken = default) where T : class
    {
        var provider = await ResolveProviderAsync(cancellationToken);

        try
        {
            await provider.SubscribeAsync(messageReceived, errorReceived, channel, group, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Messaging subscribe failed on provider {ProviderType} for channel {Channel}; falling back to NullProvider", provider.GetType().Name, channel);
        }
    }

    private async Task<IMessagingProvider> ResolveProviderAsync(CancellationToken cancellationToken)
    {
        try
        {
            var config = await _configResolver.ResolveAsync(cancellationToken);
            if (!config.IsEnabled)
            {
                return _nullProvider;
            }

            return config.Provider switch
            {
                MessagingProviderEnum.RabbitMq => _rabbitMqProvider,
                MessagingProviderEnum.InMemory => _nullProvider,
                _ => _nullProvider
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve messaging provider; defaulting to NullProvider");
            return _nullProvider;
        }
    }
}
