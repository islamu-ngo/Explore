// ABOUTME: Sends native integration sync outbox rows to Listmonk through the NSwag-generated API client.
// ABOUTME: Resolves tenant-scoped Listmonk settings and secrets without exposing credentials to logs.

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Secrets;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Secrets;
using Explore.Infrastructure.Integrations.Listmonk.Generated;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Integrations.Listmonk;

public sealed class ListmonkSyncService(
    IHttpClientFactory httpClientFactory,
    IHierarchicalSettingsResolver settingsResolver,
    ISecretResolver secretResolver,
    ILogger<ListmonkSyncService> logger)
{
    public const string HttpClientName = "ListmonkClient";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ListmonkSyncResult> SyncSubscriberAsync(
        IntegrationSyncOutbox outbox,
        CancellationToken cancellationToken)
    {
        if (outbox.Kind != IntegrationKind.Listmonk)
        {
            return ListmonkSyncResult.Failed("Integration outbox row is not a Listmonk sync.", isRetryable: false);
        }

        var settingContext = new SettingContext(TenantId: outbox.TenantId);
        var instanceUrl = await settingsResolver.ResolveAsync<string>(
            GovernanceSettingKeys.Integrations.Listmonk.InstanceUrl,
            settingContext,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(instanceUrl))
        {
            return ListmonkSyncResult.Failed("Listmonk instance URL is not configured.", isRetryable: true);
        }

        var username = await secretResolver.ResolveAsync(
            SecretDefinitionRegistry.Keys.Integrations.Listmonk.ApiUsername,
            outbox.TenantId,
            cancellationToken);
        var apiKey = await secretResolver.ResolveAsync(
            SecretDefinitionRegistry.Keys.Integrations.Listmonk.ApiKey,
            outbox.TenantId,
            cancellationToken);

        if (username is null || string.IsNullOrWhiteSpace(username.Value) ||
            apiKey is null || string.IsNullOrWhiteSpace(apiKey.Value))
        {
            return ListmonkSyncResult.Failed("Listmonk API credentials are not configured.", isRetryable: true);
        }

        var client = httpClientFactory.CreateClient(HttpClientName);
        client.BaseAddress = NormalizeApiBaseUri(instanceUrl);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Authorization = BuildBasicAuthHeader(username.Value, apiKey.Value);

        try
        {
            var apiClient = new ListmonkApiClient(client);
            var subscriber = BuildSubscriber(outbox);
            await apiClient.CreateSubscriberAsync(subscriber, cancellationToken);
            return ListmonkSyncResult.Success();
        }
        catch (ApiException ex)
        {
            var retryable = IsRetryableStatusCode(ex.StatusCode);
            logger.LogWarning(
                ex,
                "Listmonk subscriber sync failed for outbox {OutboxId} with status {StatusCode}",
                outbox.Id,
                ex.StatusCode);
            return ListmonkSyncResult.Failed($"Listmonk API returned HTTP {ex.StatusCode}.", retryable);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Listmonk subscriber sync transport failed for outbox {OutboxId}", outbox.Id);
            return ListmonkSyncResult.Failed("Listmonk API request failed.", isRetryable: true);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Listmonk subscriber sync timed out for outbox {OutboxId}", outbox.Id);
            return ListmonkSyncResult.Failed("Listmonk API request timed out.", isRetryable: true);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Listmonk subscriber payload is invalid for outbox {OutboxId}", outbox.Id);
            return ListmonkSyncResult.Failed("Listmonk subscriber payload is invalid.", isRetryable: false);
        }
    }

    private static AuthenticationHeaderValue BuildBasicAuthHeader(string username, string apiKey)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{apiKey}"));
        return new AuthenticationHeaderValue("Basic", credentials);
    }

    private static NewSubscriber BuildSubscriber(IntegrationSyncOutbox outbox)
    {
        var subscriber = JsonSerializer.Deserialize<NewSubscriber>(outbox.SubscriberPayloadJson, JsonOptions)
            ?? new NewSubscriber();

        subscriber.Email ??= outbox.SubscriberEmail;
        subscriber.Name ??= string.IsNullOrWhiteSpace(outbox.SubscriberName)
            ? outbox.SubscriberEmail
            : outbox.SubscriberName;
        subscriber.Status ??= "enabled";
        subscriber.Preconfirm_subscriptions ??= outbox.PreconfirmSubscriptions;

        if (subscriber.Lists is null || !subscriber.Lists.Contains(outbox.ListmonkListId))
        {
            subscriber.Lists = new List<int> { outbox.ListmonkListId };
        }

        return subscriber;
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

    private static bool IsRetryableStatusCode(int statusCode)
    {
        return statusCode == 408 || statusCode == 429 || statusCode >= 500;
    }
}

public sealed record ListmonkSyncResult(bool Succeeded, bool IsRetryable, string? ErrorMessage)
{
    public static ListmonkSyncResult Success() => new(true, false, null);

    public static ListmonkSyncResult Failed(string errorMessage, bool isRetryable) =>
        new(false, isRetryable, errorMessage);
}
