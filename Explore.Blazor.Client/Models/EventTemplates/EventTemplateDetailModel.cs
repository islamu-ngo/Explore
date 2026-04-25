// ABOUTME: Full model for event templates including nested definitions.
// ABOUTME: Used for the details view and editing forms.

using Explore.Blazor.Client.Models.Responses;

namespace Explore.Blazor.Client.Models.EventTemplates;

public class EventTemplateDetailModel
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
    public int DefinitionsCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public IReadOnlyList<EventTemplateDefinitionModel> Definitions { get; set; } = new List<EventTemplateDefinitionModel>();
    
    public IReadOnlyDictionary<string, HalLinkDto>? Links { get; set; }
}