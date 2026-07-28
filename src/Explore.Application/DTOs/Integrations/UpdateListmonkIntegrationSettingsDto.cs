// ABOUTME: Grouped PATCH contract for tenant Listmonk non-secret integration settings.
// ABOUTME: Credentials remain isolated behind the dedicated rotation endpoint.

namespace Explore.Application.DTOs.Integrations;

public sealed class UpdateListmonkIntegrationSettingsDto
{
    public ListmonkConnectionUpdateDto? Connection { get; set; }
    public ListmonkBehaviorUpdateDto? Behavior { get; set; }
}

public sealed class ListmonkConnectionUpdateDto
{
    public string? InstanceUrl { get; set; }
    public int DefaultListId { get; set; }
}

public sealed class ListmonkBehaviorUpdateDto
{
    public bool Enabled { get; set; }
    public bool PreconfirmSubscriptions { get; set; }
    public bool SyncOnRegistration { get; set; }
}
