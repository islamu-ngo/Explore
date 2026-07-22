// ABOUTME: Holds authority-revalidated shell workspace, actor, and Settings-scope preferences.
// ABOUTME: Keeps durable shell selection state separate from its persistence contract.

namespace Explore.Blazor.Client.Contracts.Services.Shell;

public sealed record ShellPreferenceState(
    string LastWorkspace,
    Guid? LastActorId,
    string LastSettingsScopeHref);
