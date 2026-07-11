// ABOUTME: Lightweight list DTO for event templates, used in paginated collection responses.
// ABOUTME: Includes DefinitionCount instead of full definitions to reduce payload size.

namespace Explore.Application.DTOs.EventTemplate;

public class EventTemplateListDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string TemplateKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? EventTypeId { get; set; }
    public int Version { get; set; }
    public bool IsPublished { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public int DefinitionCount { get; set; }
}
