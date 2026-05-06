// ABOUTME: Client-side read model for event program sections/tracks/devrooms.
// ABOUTME: Bridges HAL-generated session group responses into typed Blazor UI state.

using System.Text.Json.Serialization;

namespace Explore.Blazor.Client.Models.EventSessionGroups;

public sealed class EventSessionGroupListModel
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public Guid? LocationId { get; set; }
    public string? LocationName { get; set; }
    public Guid? RoomId { get; set; }
    public string? RoomName { get; set; }
    public string? Color { get; set; }
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; }
    public Guid TenantId { get; set; }

    [JsonExtensionData]
    public IDictionary<string, object>? AdditionalProperties { get; set; }
}
