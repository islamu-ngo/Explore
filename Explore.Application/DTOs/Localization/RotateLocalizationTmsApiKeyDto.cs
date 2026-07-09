// ABOUTME: Admin-only request DTO for rotating the backend TMS API key used by localization providers.
// ABOUTME: Carries plaintext only across the authenticated API boundary and is never returned to clients.

namespace Explore.Application.DTOs.Localization;

public class RotateLocalizationTmsApiKeyDto
{
    public string? TmsApiKey { get; set; }
}
