// ABOUTME: API request DTO for creating short-lived Svix App Portal access for one webhook consumer.
// ABOUTME: Excludes capability fields so callers cannot choose portal authority.

namespace Explore.Application.DTOs.Webhooks;

public sealed class OpenSvixAppPortalRequestDto
{
    public Guid ConsumerId { get; set; }

    public int? ExpiresInSeconds { get; set; }
}
