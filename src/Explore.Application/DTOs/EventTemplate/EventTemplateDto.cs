// ABOUTME: Read-only detail DTO for event templates, includes full definition list with nested options.
// ABOUTME: Used by GetEventTemplateDetails query handler and HATEOAS detail resource.

namespace Explore.Application.DTOs.EventTemplate;

public sealed record EventTemplateDto
{
    public Guid Id { get; init; }
    public Guid ConcurrencyStamp { get; init; }
    public Guid TenantId { get; init; }
    public string TemplateKey { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int? EventTypeId { get; init; }
    public int Version { get; init; }
    public bool IsPublished { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
    public List<EventTemplateDefinitionDto> Definitions { get; init; } = [];
}
