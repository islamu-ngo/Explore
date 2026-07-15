// ABOUTME: API request body for manually pausing an active Local webhook endpoint.
// ABOUTME: Carries a normalized audit reason while tenant and actor authority remain server-owned.

namespace Explore.Application.DTOs.Webhooks;

public sealed class PauseWebhookEndpointRequestDto
{
    public long ExpectedDeliveryStateVersion { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
}
