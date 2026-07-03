// ABOUTME: API request DTO for creating a short-lived Svix App Portal access URL.
// ABOUTME: Carries optional consumer and session-scoped portal flags without exposing provider credentials.

namespace Explore.Application.DTOs.Webhooks;

public sealed class OpenSvixAppPortalRequestDto
{
    public Guid? ConsumerId { get; set; }

    public bool ReadOnly { get; set; }

    public int? ExpiresInSeconds { get; set; }

    public IReadOnlyCollection<string>? FeatureFlags { get; set; }
}
