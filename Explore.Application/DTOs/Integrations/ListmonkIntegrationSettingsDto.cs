// ABOUTME: Sanitized API response for tenant Listmonk integration settings.
// ABOUTME: Exposes non-secret configuration and credential presence flags only.

namespace Explore.Application.DTOs.Integrations;

public sealed class ListmonkIntegrationSettingsDto
{
    public bool Enabled { get; set; }
    public string? InstanceUrl { get; set; }
    public int DefaultListId { get; set; }
    public bool PreconfirmSubscriptions { get; set; }
    public bool SyncOnRegistration { get; set; }
    public bool ApiUsernameConfigured { get; set; }
    public bool ApiKeyConfigured { get; set; }
    public bool CanEdit { get; set; }
}
