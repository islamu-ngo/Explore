// ABOUTME: Authenticated workspace-shell context projected from server-authoritative capabilities.
// ABOUTME: Carries workspace availability, managed actors, settings scopes, and navigation defaults.

namespace Explore.Application.DTOs.UiShell;

public sealed class UiShellContextDto
{
    public Guid TenantId { get; set; }
    public string DeploymentMode { get; set; } = "SingleTenant";
    public WorkspaceAvailabilityDto Workspaces { get; set; } = new();
    public IReadOnlyList<ManagedActorDto> ManagedActors { get; set; } = [];
    public IReadOnlyList<SettingsScopeDto> SettingsScopes { get; set; } = [];
    public Guid? PinnedActorId { get; set; }
    public UiShellNavigationDefaultsDto NavigationDefaults { get; set; } = new();
}

public sealed class WorkspaceAvailabilityDto
{
    public bool Events { get; set; } = true;
    public bool Studio { get; set; }
    public bool Ai { get; set; }
    public bool Settings { get; set; } = true;
}

public sealed class ManagedActorDto
{
    public Guid ActorId { get; set; }
    public Guid ScopeId { get; set; }
    public string ActorType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class SettingsScopeDto
{
    public string Scope { get; set; } = string.Empty;
    public Guid? ScopeId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class UiShellNavigationDefaultsDto
{
    public string Events { get; set; } = "Docked";
    public string Studio { get; set; } = "Docked";
    public string Ai { get; set; } = "Docked";
    public bool AllowUserOverride { get; set; } = true;
    public string OrganizerDefaultWorkspace { get; set; } = "Events";
}
