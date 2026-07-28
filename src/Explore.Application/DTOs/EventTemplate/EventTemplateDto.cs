// ABOUTME: Read-only detail DTO for event templates, includes full definition list with nested options.
// ABOUTME: Used by GetEventTemplateDetails query handler and HATEOAS detail resource.

namespace Explore.Application.DTOs.EventTemplate;

public class EventTemplateDto
{
    public Guid Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    public Guid TenantId { get; set; }
    public string TemplateKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? EventTypeId { get; set; }
    public int Version { get; set; }
    public bool IsPublished { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public List<EventTemplateDefinitionDto> Definitions { get; set; } = [];
}
