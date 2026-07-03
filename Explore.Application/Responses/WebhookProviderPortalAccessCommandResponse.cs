// ABOUTME: Command response for webhook provider portal access creation.
// ABOUTME: Extends the standard command response with retryability for provider failure mapping.

using Explore.Application.DTOs.Webhooks;

namespace Explore.Application.Responses;

public sealed class WebhookProviderPortalAccessCommandResponse : BaseCommandResponse<WebhookProviderPortalAccessDto>
{
    public bool IsRetryable { get; set; }
}
