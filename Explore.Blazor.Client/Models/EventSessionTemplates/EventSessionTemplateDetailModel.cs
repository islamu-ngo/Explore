// ABOUTME: Full model for event session templates including nested property definitions.
// ABOUTME: Used for session blueprint previews inside the session editor drawer.

namespace Explore.Blazor.Client.Models.EventSessionTemplates;

public class EventSessionTemplateDetailModel
{
    public Guid Id { get; set; }
    public Guid EventTemplateId { get; set; }
    public Guid TenantId { get; set; }
    public string SessionTemplateKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Version { get; set; }
    public bool IsPublished { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public IReadOnlyList<EventSessionTemplateDefinitionModel> Definitions { get; set; } = new List<EventSessionTemplateDefinitionModel>();
    public IReadOnlyDictionary<string, HalLinkDto>? Links { get; set; }
}
