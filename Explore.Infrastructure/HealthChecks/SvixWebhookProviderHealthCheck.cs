// ABOUTME: Readiness health check for the Svix outgoing webhook provider.
// ABOUTME: Verifies provider selection and server-side secret resolution without exposing tokens or endpoint URLs.

using Explore.Application.Contracts.Secrets;
using Explore.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.HealthChecks;

public sealed class SvixWebhookProviderHealthCheck(
    IOptionsMonitor<WebhookOptions> options,
    IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var currentOptions = options.CurrentValue;
        var svixSelected = !currentOptions.IsDisabled
            && (currentOptions.IsProvider(WebhookOptions.ProviderSvix)
                || currentOptions.IsProvider(WebhookOptions.ProviderComposite));
        var data = new Dictionary<string, object>
        {
            ["enabled"] = currentOptions.Enabled,
            ["provider"] = currentOptions.Provider,
            ["svixProviderSelected"] = svixSelected,
            ["baseUrlConfigured"] = !string.IsNullOrWhiteSpace(currentOptions.Svix.BaseUrl),
            ["appPortalEnabled"] = currentOptions.Svix.AppPortalEnabled,
            ["syncEventTypesOnStartup"] = currentOptions.Svix.SyncEventTypesOnStartup,
            ["authTokenBindingConfigured"] = !string.IsNullOrWhiteSpace(currentOptions.Svix.AuthTokenSecretRef),
            ["operationalWebhookBindingConfigured"] = !string.IsNullOrWhiteSpace(currentOptions.Svix.OperationalWebhookSecretRef)
        };

        if (!svixSelected)
        {
            return HealthCheckResult.Healthy(
                "Svix webhook provider is not the selected outgoing provider.",
                data);
        }

        var authTokenSettingKey = currentOptions.Svix.AuthTokenSecretRef?.Trim();
        if (string.IsNullOrWhiteSpace(authTokenSettingKey))
        {
            return HealthCheckResult.Unhealthy(
                "Svix webhook auth token binding is not configured.",
                data: data);
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var secretResolver = scope.ServiceProvider.GetRequiredService<ISecretResolver>();
        var authToken = await secretResolver.ResolveAsync(authTokenSettingKey, tenantId: null, cancellationToken);
        var authTokenResolved = authToken is not null && !string.IsNullOrWhiteSpace(authToken.Value);
        data["authTokenResolved"] = authTokenResolved;
        if (!authTokenResolved)
        {
            return HealthCheckResult.Unhealthy(
                "Svix webhook auth token could not be resolved.",
                data: data);
        }

        var operationalWebhookSettingKey = currentOptions.Svix.OperationalWebhookSecretRef?.Trim();
        if (!string.IsNullOrWhiteSpace(operationalWebhookSettingKey))
        {
            var operationalSecret = await secretResolver.ResolveAsync(
                operationalWebhookSettingKey,
                tenantId: null,
                cancellationToken);
            data["operationalWebhookSecretResolved"] = operationalSecret is not null
                && !string.IsNullOrWhiteSpace(operationalSecret.Value);
        }

        return HealthCheckResult.Healthy(
            "Svix webhook provider configuration is ready.",
            data);
    }
}
