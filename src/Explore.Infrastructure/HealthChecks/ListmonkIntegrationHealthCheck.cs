// ABOUTME: Health check for the native Listmonk subscriber sync integration.
// ABOUTME: Validates generated-client reachability without exposing tenant API credentials.

using System.Net.Http.Headers;
using System.Text;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Secrets;
using Explore.Application.Settings;
using Explore.Domain.Constants;
using Explore.Domain.Secrets;
using Explore.Infrastructure.Integrations.Listmonk;
using Explore.Infrastructure.Integrations.Listmonk.Generated;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Explore.Infrastructure.HealthChecks;

public sealed class ListmonkIntegrationHealthCheck(
    IHierarchicalSettingsResolver settingsResolver,
    ISecretResolver secretResolver,
    IHttpClientFactory httpClientFactory)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var settingContext = new SettingContext();
        var enabled = await settingsResolver.ResolveAsync<bool>(
            GovernanceSettingKeys.Integrations.Listmonk.Enabled,
            settingContext,
            cancellationToken);
        var syncOnRegistration = await settingsResolver.ResolveAsync<bool>(
            GovernanceSettingKeys.Integrations.Listmonk.SyncOnRegistration,
            settingContext,
            cancellationToken);
        var preconfirmSubscriptions = await settingsResolver.ResolveAsync<bool>(
            GovernanceSettingKeys.Integrations.Listmonk.PreconfirmSubscriptions,
            settingContext,
            cancellationToken);

        if (!enabled)
        {
            return HealthCheckResult.Healthy(
                "Listmonk integration is disabled.",
                BuildData(enabled, syncOnRegistration, preconfirmSubscriptions));
        }

        var instanceUrl = await settingsResolver.ResolveAsync<string>(
            GovernanceSettingKeys.Integrations.Listmonk.InstanceUrl,
            settingContext,
            cancellationToken);
        var defaultListId = await settingsResolver.ResolveAsync<int>(
            GovernanceSettingKeys.Integrations.Listmonk.DefaultListId,
            settingContext,
            cancellationToken);
        var username = await secretResolver.ResolveAsync(
            SecretDefinitionRegistry.Keys.Integrations.Listmonk.ApiUsername,
            tenantId: null,
            cancellationToken);
        var apiKey = await secretResolver.ResolveAsync(
            SecretDefinitionRegistry.Keys.Integrations.Listmonk.ApiKey,
            tenantId: null,
            cancellationToken);

        var data = BuildData(
            enabled,
            syncOnRegistration,
            preconfirmSubscriptions,
            !string.IsNullOrWhiteSpace(instanceUrl),
            defaultListId > 0,
            username is not null && !string.IsNullOrWhiteSpace(username.Value),
            apiKey is not null && !string.IsNullOrWhiteSpace(apiKey.Value));

        if (string.IsNullOrWhiteSpace(instanceUrl) ||
            defaultListId <= 0 ||
            username is null || string.IsNullOrWhiteSpace(username.Value) ||
            apiKey is null || string.IsNullOrWhiteSpace(apiKey.Value))
        {
            return HealthCheckResult.Degraded(
                "Listmonk integration is enabled but configuration is incomplete.",
                data: data);
        }

        try
        {
            var client = httpClientFactory.CreateClient(ListmonkSyncService.HttpClientName);
            client.BaseAddress = NormalizeApiBaseUri(instanceUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = BuildBasicAuthHeader(username.Value, apiKey.Value);

            var apiClient = new ListmonkApiClient(client);
            await apiClient.GetHealthCheckAsync(cancellationToken);

            return HealthCheckResult.Healthy("Listmonk API health check succeeded.", data);
        }
        catch (ApiException ex)
        {
            return HealthCheckResult.Degraded(
                $"Listmonk API returned HTTP {ex.StatusCode}.",
                ex,
                data);
        }
        catch (HttpRequestException ex)
        {
            return HealthCheckResult.Degraded("Listmonk API request failed.", ex, data);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Degraded("Listmonk API request timed out.", ex, data);
        }
        catch (InvalidOperationException ex)
        {
            return HealthCheckResult.Degraded("Listmonk integration configuration is invalid.", ex, data);
        }
    }

    private static Dictionary<string, object> BuildData(
        bool enabled,
        bool syncOnRegistration,
        bool preconfirmSubscriptions,
        bool instanceUrlConfigured = false,
        bool defaultListIdConfigured = false,
        bool apiUsernameResolved = false,
        bool apiKeyResolved = false)
    {
        return new Dictionary<string, object>
        {
            ["enabled"] = enabled,
            ["syncOnRegistration"] = syncOnRegistration,
            ["preconfirmSubscriptions"] = preconfirmSubscriptions,
            ["instanceUrlConfigured"] = instanceUrlConfigured,
            ["defaultListIdConfigured"] = defaultListIdConfigured,
            ["apiUsernameResolved"] = apiUsernameResolved,
            ["apiKeyResolved"] = apiKeyResolved
        };
    }

    private static AuthenticationHeaderValue BuildBasicAuthHeader(string username, string apiKey)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{apiKey}"));
        return new AuthenticationHeaderValue("Basic", credentials);
    }

    private static Uri NormalizeApiBaseUri(string instanceUrl)
    {
        if (!Uri.TryCreate(instanceUrl.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("Listmonk instance URL must be an absolute HTTP(S) URL.");
        }

        var builder = new UriBuilder(uri) { Query = string.Empty, Fragment = string.Empty };
        var path = builder.Path.TrimEnd('/');

        if (!path.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            path = string.IsNullOrEmpty(path) ? "/api" : $"{path}/api";
        }

        builder.Path = $"{path}/";
        return builder.Uri;
    }
}
