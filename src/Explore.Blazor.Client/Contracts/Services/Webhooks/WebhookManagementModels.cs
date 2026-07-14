// ABOUTME: UI-facing webhook management snapshot and result models.
// ABOUTME: Preserves generated HAL resources while centralizing link-rel checks and command outcomes.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Webhooks;

public static class WebhookClientLinkRelations
{
    public const string Create = "create";
    public const string Update = "update";
    public const string Delete = "delete";
    public const string RotateSecret = "rotate-secret";
    public const string Test = "test";
    public const string Retry = "retry";
    public const string OpenProviderPortal = "open-provider-portal";
}

public static class WebhookPendingWorkDecisionIds
{
    public const int PreserveExisting = 1;
    public const int MigrateEligible = 2;
}

public static class WebhookHal
{
    public static bool HasLink<TLink>(IDictionary<string, TLink>? links, string relation) =>
        links?.ContainsKey(relation) == true;
}

public sealed class WebhookManagementSnapshot
{
    public IReadOnlyList<WebhookEventTypeDto> EventTypes { get; init; } = [];

    public IReadOnlyList<HalResourceOfWebhookConsumerDto> Consumers { get; init; } = [];

    public IReadOnlyList<HalResourceOfWebhookEndpointDto> Endpoints { get; init; } = [];

    public IReadOnlyList<HalResourceOfWebhookMessageDto> Messages { get; init; } = [];

    public IReadOnlyList<HalResourceOfWebhookDeliveryAttemptDto> DeliveryAttempts { get; init; } = [];

    public bool CanCreateConsumer { get; init; }

    public bool CanCreateEndpoint { get; init; }

    public bool IsSuccess { get; init; } = true;

    public string? ErrorMessage { get; init; }

    public static WebhookManagementSnapshot Failed(string message) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = message
        };
}

public sealed record WebhookActionResult(
    bool Success,
    string Message,
    Guid? Id = null)
{
    public static WebhookActionResult Succeeded(string message, Guid? id = null) =>
        new(true, message, id);

    public static WebhookActionResult Failed(string message) =>
        new(false, message);
}

public sealed record WebhookPortalResult(
    bool Success,
    string Message,
    string? Url = null);
