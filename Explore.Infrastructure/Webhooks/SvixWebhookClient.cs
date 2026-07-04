// ABOUTME: Production adapter around the official Svix C# SDK for outgoing webhook delivery.
// ABOUTME: Resolves backend-only auth token secrets and maps canonical messages to Svix applications/messages.

using System.Text.Json;
using Explore.Application.Contracts.Secrets;
using Explore.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Svix;
using Svix.Models;

namespace Explore.Infrastructure.Webhooks;

internal sealed class SvixWebhookClient(
    ISecretResolver secretResolver,
    IOptionsMonitor<WebhookOptions> options,
    ILogger<SvixClient> svixLogger) : ISvixWebhookClient
{
    public async Task<SvixApplicationSyncResult> GetOrCreateApplicationAsync(
        SvixApplicationSyncRequest request,
        CancellationToken cancellationToken)
    {
        var client = await CreateClientAsync(cancellationToken);
        var application = await client.Application.GetOrCreateAsync(
            new ApplicationIn
            {
                Name = request.Name,
                Uid = request.AppUid,
                Metadata = request.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            },
            new ApplicationCreateOptions
            {
                IdempotencyKey = request.IdempotencyKey
            },
            cancellationToken);

        return new SvixApplicationSyncResult(
            application.Id,
            string.IsNullOrWhiteSpace(application.Uid) ? request.AppUid : application.Uid);
    }

    public async Task<SvixMessageCreateResult> CreateMessageAsync(
        SvixMessageCreateRequest request,
        CancellationToken cancellationToken)
    {
        var client = await CreateClientAsync(cancellationToken);
        var message = Message.messageInRaw(
            request.EventType,
            request.PayloadJson,
            "application/json",
            application: null,
            channels: null,
            eventId: request.EventId,
            payloadRetentionHours: null,
            payloadRetentionPeriod: request.PayloadRetentionDays,
            tags: null,
            transformationsParams: null);

        var created = await client.Message.CreateAsync(
            request.AppUid,
            message,
            new MessageCreateOptions
            {
                IdempotencyKey = request.IdempotencyKey
            },
            cancellationToken);

        return new SvixMessageCreateResult(created.Id);
    }

    public async Task<SvixAppPortalAccessResult> CreateAppPortalAccessAsync(
        SvixAppPortalAccessRequest request,
        CancellationToken cancellationToken)
    {
        var client = await CreateClientAsync(cancellationToken);
        var access = await client.Authentication.AppPortalAccessAsync(
            request.AppId,
            new AppPortalAccessIn
            {
                SessionId = request.SessionId,
                ReadOnly = request.ReadOnly,
                Expiry = (ulong)Math.Ceiling(request.ExpiresIn.TotalSeconds),
                FeatureFlags = request.FeatureFlags.Count == 0
                    ? null
                    : request.FeatureFlags.ToList()
            },
            new AuthenticationAppPortalAccessOptions
            {
                IdempotencyKey = request.IdempotencyKey
            },
            cancellationToken);

        return new SvixAppPortalAccessResult(access.Url, access.Token);
    }

    public async Task<SvixEventTypeSyncResult> UpsertEventTypeAsync(
        SvixEventTypeSyncRequest request,
        CancellationToken cancellationToken)
    {
        var client = await CreateClientAsync(cancellationToken);
        var schema = ParseSchema(request.SchemaJson);
        try
        {
            var created = await client.EventType.CreateAsync(
                new EventTypeIn
                {
                    Name = request.Name,
                    Description = request.Description,
                    GroupName = request.GroupName,
                    Schemas = schema
                },
                new EventTypeCreateOptions
                {
                    IdempotencyKey = request.IdempotencyKey
                },
                cancellationToken);

            return new SvixEventTypeSyncResult(created.Name);
        }
        catch (ApiException ex) when (ex.ErrorCode == 409)
        {
            var updated = await client.EventType.UpdateAsync(
                request.Name,
                new EventTypeUpdate
                {
                    Description = request.Description,
                    GroupName = request.GroupName,
                    Schemas = schema
                },
                cancellationToken);

            return new SvixEventTypeSyncResult(updated.Name);
        }
    }

    private async Task<SvixClient> CreateClientAsync(CancellationToken cancellationToken)
    {
        var currentOptions = options.CurrentValue.Svix;
        var settingKey = currentOptions.AuthTokenSecretRef?.Trim();
        if (string.IsNullOrWhiteSpace(settingKey))
        {
            throw new SvixWebhookConfigurationException("svix_auth_token_secret_missing");
        }

        var resolved = await secretResolver.ResolveAsync(settingKey, tenantId: null, cancellationToken);
        if (resolved is null || string.IsNullOrWhiteSpace(resolved.Value))
        {
            throw new SvixWebhookConfigurationException("svix_auth_token_unresolved");
        }

        var svixOptions = string.IsNullOrWhiteSpace(currentOptions.BaseUrl)
            ? null
            : new SvixOptions(serverUrl: currentOptions.BaseUrl.Trim());

        return new SvixClient(resolved.Value, svixOptions, svixLogger);
    }

    private static JsonElement ParseSchema(string schemaJson)
    {
        using var document = JsonDocument.Parse(schemaJson);
        return document.RootElement.Clone();
    }
}

internal sealed class SvixWebhookConfigurationException(string failureCategory) : Exception(failureCategory)
{
    public string FailureCategory { get; } = failureCategory;
}
