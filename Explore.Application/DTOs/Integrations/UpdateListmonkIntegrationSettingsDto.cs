// ABOUTME: API input DTO for tenant Listmonk non-secret integration settings.
// ABOUTME: Secrets are rotated through a dedicated credentials endpoint and never returned.

namespace Explore.Application.DTOs.Integrations;

public sealed class UpdateListmonkIntegrationSettingsDto
{
    public bool Enabled { get; set; }
    public string? InstanceUrl { get; set; }
    public int DefaultListId { get; set; }
    public bool PreconfirmSubscriptions { get; set; } = true;
    public bool SyncOnRegistration { get; set; }
}
