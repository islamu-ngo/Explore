// ABOUTME: Model representing an option for an event session template definition.
// ABOUTME: Used by the session editor blueprint preview in the Blazor client.

namespace Explore.Blazor.Client.Models.EventSessionTemplates;

public class EventSessionTemplateOptionModel
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
