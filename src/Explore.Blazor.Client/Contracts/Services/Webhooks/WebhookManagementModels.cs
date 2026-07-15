// ABOUTME: UI-facing webhook management snapshot and result models.
// ABOUTME: Preserves generated HAL resources while centralizing link-rel checks and command outcomes.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Webhooks;

public enum WebhookOwnerKind
{
    Tenant = 1,
    Organization = 2,
    Group = 3,
    User = 4,
    Instance = 5
}

public sealed record WebhookOwnerSelection
{
    private WebhookOwnerSelection(WebhookOwnerKind kind, Guid? ownerId)
    {
        Kind = kind;
        OwnerId = ownerId;
    }

    public WebhookOwnerKind Kind { get; }

    public int OwnerKindId => (int)Kind;

    public Guid? OwnerId { get; }

    public string DisplayName => Kind.ToString();

    public static WebhookOwnerSelection Tenant { get; } = new(WebhookOwnerKind.Tenant, null);

    public static WebhookOwnerSelection User { get; } = new(WebhookOwnerKind.User, null);

    public static WebhookOwnerSelection Instance { get; } = new(WebhookOwnerKind.Instance, null);

    public static WebhookOwnerSelection ForOrganization(Guid organizationId) =>
        new(WebhookOwnerKind.Organization, RequireOwnerId(organizationId, nameof(organizationId)));

    public static WebhookOwnerSelection ForGroup(Guid groupId) =>
        new(WebhookOwnerKind.Group, RequireOwnerId(groupId, nameof(groupId)));

    private static Guid RequireOwnerId(Guid ownerId, string parameterName) =>
        ownerId != Guid.Empty
            ? ownerId
            : throw new ArgumentException("Webhook owner id must not be empty.", parameterName);
}

public static class WebhookClientLinkRelations
{
    public const string Create = "create";
    public const string Update = "update";
    public const string Delete = "delete";
    public const string RotateSecret = "rotate-secret";
    public const string Test = "test";
    public const string Retry = "retry";
    public const string OpenProviderPortal = "open-provider-portal";
    public const string Pause = "pause";
    public const string Resume = "resume";
    public const string Payload = "payload";
    public const string DeliveryAttempts = "delivery-attempts";
    public const string ProviderPublications = "provider-publications";
    public const string Reconcile = "reconcile";
    public const string Abandon = "abandon";
    public const string BulkReplayPreview = "bulk-replay-preview";
    public const string BulkReplays = "bulk-replays";
    public const string Cancel = "cancel";
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

    public bool CanViewProviderPublications { get; init; }

    public bool CanUseBulkReplay { get; init; }

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

public sealed class WebhookProviderPublicationSnapshot
{
    public IReadOnlyList<HalResourceOfWebhookProviderPublicationDto> Publications { get; init; } = [];

    public bool IsSuccess { get; init; } = true;

    public string? ErrorMessage { get; init; }

    public static WebhookProviderPublicationSnapshot Failed(string message) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = message
        };
}

public sealed class WebhookBulkReplaySnapshot
{
    public IReadOnlyList<HalResourceOfWebhookBulkReplayOperationDto> Operations { get; init; } = [];

    public bool CanPreview { get; init; }

    public bool CanSchedule { get; init; }

    public bool IsSuccess { get; init; } = true;

    public string? ErrorMessage { get; init; }

    public static WebhookBulkReplaySnapshot Failed(string message) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = message
        };
}

public sealed record WebhookPayloadResult(
    bool Success,
    string Message,
    WebhookMessagePayloadDto? Payload = null);

public sealed record WebhookBulkReplayPreviewResult(
    bool Success,
    string Message,
    WebhookBulkReplayPreviewDto? Preview = null);
