// ABOUTME: Client-side option model for Option-typed custom property definitions.
// ABOUTME: Mirrors the server CustomPropertyOptionDto for detail round-trip.

namespace Explore.Blazor.Client.Models.CustomProperties;

/// <summary>
/// Single option on an Option-typed custom-property definition.
/// </summary>
public sealed class CustomPropertyOptionModel
{
    public Guid Id { get; set; }
    public string Namespace { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Value { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}
