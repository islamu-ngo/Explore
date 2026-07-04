// ABOUTME: Model representing an event template in a list context.
// ABOUTME: Supports HAL link gating for edit/delete capabilities.

using System.Text.Json.Serialization;
using Explore.Blazor.Client.Models;

namespace Explore.Blazor.Client.Models.EventTemplates;

public class EventTemplateListModel
{
    public Guid Id { get; set; }
    public string TemplateKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int EventTypeId { get; set; }
    public int Version { get; set; }
    public bool IsPublished { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    [JsonPropertyName("definitionCount")]
    public int DefinitionsCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("_links")]
    public IReadOnlyDictionary<string, HalLinkDto>? Links { get; set; }
}
