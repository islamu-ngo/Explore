// ABOUTME: Model representing an option for an event template definition.
// ABOUTME: Used in the Blazor client to manage predefined choices.

namespace Explore.Blazor.Client.Models.EventTemplates;

public class EventTemplateOptionModel
{
    public string? Namespace { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Value { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
