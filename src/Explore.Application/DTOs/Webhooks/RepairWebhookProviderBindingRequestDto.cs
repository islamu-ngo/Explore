// ABOUTME: API request for verifying or rebinding one consumer to a self-hosted provider application.
// ABOUTME: Carries no tenant authority, credentials, capabilities, or provider response data.

namespace Explore.Application.DTOs.Webhooks;

public sealed class RepairWebhookProviderBindingRequestDto
{
    public string ExternalApplicationId { get; set; } = string.Empty;

    public string ReasonCode { get; set; } = string.Empty;
}
