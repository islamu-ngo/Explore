// ABOUTME: Write DTO for creating event templates, optionally includes nested definition payloads.
// ABOUTME: Templates can be created empty then populated, or with definitions in a single call.

namespace Explore.Application.DTOs.EventTemplate;

public class CreateEventTemplateDto
{
    public string TemplateKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? EventTypeId { get; set; }
    public int Version { get; set; } = 1;
    public bool IsPublished { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public List<CreateEventTemplateDefinitionDto> Definitions { get; set; } = [];
}
