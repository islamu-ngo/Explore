// ABOUTME: API request body for resuming a manually or automatically paused Local endpoint.
// ABOUTME: Carries optimistic delivery-state evidence and a normalized mandatory audit reason.

namespace Explore.Application.DTOs.Webhooks;

public sealed class ResumeWebhookEndpointRequestDto
{
    public long ExpectedDeliveryStateVersion { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
}
