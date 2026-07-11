// ABOUTME: Runtime webhook provider selector for outgoing product webhook delivery modes.
// ABOUTME: Keeps provider selection in Infrastructure while exposing one Application-layer delivery contract.

using Explore.Application.Contracts.Webhooks;
using Explore.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Webhooks;

public sealed class RuntimeWebhookDeliveryProvider : IWebhookDeliveryProvider
{
    private readonly DisabledWebhookDeliveryProvider _disabledProvider;
    private readonly DryRunWebhookDeliveryProvider _dryRunProvider;
    private readonly LocalWebhookDeliveryProvider _localProvider;
    private readonly SvixWebhookDeliveryProvider _svixProvider;
    private readonly IOptionsMonitor<WebhookOptions> _options;
    private readonly ILogger<RuntimeWebhookDeliveryProvider> _logger;

    public RuntimeWebhookDeliveryProvider(
        DisabledWebhookDeliveryProvider disabledProvider,
        DryRunWebhookDeliveryProvider dryRunProvider,
        LocalWebhookDeliveryProvider localProvider,
        SvixWebhookDeliveryProvider svixProvider,
        IOptionsMonitor<WebhookOptions> options,
        ILogger<RuntimeWebhookDeliveryProvider> logger)
    {
        _disabledProvider = disabledProvider;
        _dryRunProvider = dryRunProvider;
        _localProvider = localProvider;
        _svixProvider = svixProvider;
        _options = options;
        _logger = logger;
    }

    public string ProviderName => ResolveProviderName(_options.CurrentValue);

    public Task<WebhookProviderPublishResult> PublishAsync(
        WebhookProviderMessage message,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = _options.CurrentValue;
        if (options.IsDisabled)
        {
            return _disabledProvider.PublishAsync(message, cancellationToken);
        }

        if (options.IsProvider(WebhookOptions.ProviderDryRun))
        {
            return _dryRunProvider.PublishAsync(message, cancellationToken);
        }

        if (options.IsProvider(WebhookOptions.ProviderLocal))
        {
            return ExecuteProviderAsync(_localProvider, message, cancellationToken);
        }

        if (options.IsProvider(WebhookOptions.ProviderSvix) || options.IsProvider(WebhookOptions.ProviderComposite))
        {
            return ExecuteProviderAsync(_svixProvider, message, cancellationToken);
        }

        return Task.FromResult(WebhookProviderPublishResult.Failure(
            "unsupported_webhook_provider",
            isRetryable: false,
            "Configured webhook provider is not supported."));
    }

    private async Task<WebhookProviderPublishResult> ExecuteProviderAsync(
        IWebhookDeliveryProvider provider,
        WebhookProviderMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            return await provider.PublishAsync(message, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Webhook provider {ProviderName} failed while publishing {EventType} with {FailureType}",
                provider.ProviderName,
                message.EventType,
                ex.GetType().Name);

            return WebhookProviderPublishResult.Failure(
                "webhook_provider_failed",
                isRetryable: true,
                ex.GetType().Name);
        }
    }

    private static string ResolveProviderName(WebhookOptions options)
    {
        if (options.IsDisabled)
        {
            return "Disabled";
        }

        if (options.IsProvider(WebhookOptions.ProviderDryRun))
        {
            return "DryRun";
        }

        if (options.IsProvider(WebhookOptions.ProviderSvix))
        {
            return "Svix";
        }

        if (options.IsProvider(WebhookOptions.ProviderComposite))
        {
            return "Composite";
        }

        return "Local";
    }
}
