// ABOUTME: Read-only detail DTO for event session templates, includes full definition list with nested options.
// ABOUTME: Used by GetEventSessionTemplateDetails query handler and HATEOAS detail resource.

namespace Explore.Application.DTOs.EventSessionTemplate;

public class EventSessionTemplateDto
{
    public Guid Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    public Guid EventTemplateId { get; set; }
    public Guid TenantId { get; set; }
    public string SessionTemplateKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Version { get; set; }
    public bool IsPublished { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public List<EventSessionTemplateDefinitionDto> Definitions { get; set; } = [];
}
