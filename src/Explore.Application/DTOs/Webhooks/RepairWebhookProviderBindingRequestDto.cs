// ABOUTME: API request for verifying or rebinding one consumer to a self-hosted provider application.
// ABOUTME: Carries no tenant authority, credentials, capabilities, or provider response data.

namespace Explore.Application.DTOs.Webhooks;

public sealed record RepairWebhookProviderBindingRequestDto
{
    public string ExternalApplicationId { get; init; } = string.Empty;

    public string ReasonCode { get; init; } = string.Empty;
}
