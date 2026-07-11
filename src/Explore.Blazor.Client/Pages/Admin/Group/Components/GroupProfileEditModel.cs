// ABOUTME: Local edit model for Group profile forms in the Blazor admin surface.
// ABOUTME: Keeps UI binding separate from generated grouped PATCH transport DTOs.

namespace Explore.Blazor.Client.Pages.Admin.Group.Components;

public sealed class GroupProfileEditModel
{
    public string FullName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
