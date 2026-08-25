// ABOUTME: API request body for manually pausing an active Local webhook endpoint.
// ABOUTME: Carries a normalized audit reason while tenant and actor authority remain server-owned.

namespace Explore.Application.DTOs.Webhooks;

public sealed record PauseWebhookEndpointRequestDto
{
    public long ExpectedDeliveryStateVersion { get; init; }
    public string ReasonCode { get; init; } = string.Empty;
}
