// ABOUTME: Model representing an event session template in list context.
// ABOUTME: Supports session blueprint selection scoped by parent event template.

namespace Explore.Blazor.Client.Models.EventSessionTemplates;

public class EventSessionTemplateListModel
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
    public int DefinitionCount { get; set; }
    public IReadOnlyDictionary<string, HalLinkDto>? Links { get; set; }
}
