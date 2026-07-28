// ABOUTME: Grouped partial-update contract for event template metadata and definitions.
// ABOUTME: Identity and concurrency come from the route and If-Match header, never the body.

using Explore.Application.Models.Common;

namespace Explore.Application.DTOs.EventTemplate;

public sealed class UpdateEventTemplateDto
{
    public UpdateEventTemplateMetadataDto? Metadata { get; set; }
    public UpdateEventTemplateDefinitionsDto? Definitions { get; set; }
}

public sealed class UpdateEventTemplateMetadataDto
{
    public string? TemplateKey { get; set; }
    public string? DisplayName { get; set; }
    public OptionalUpdate<string> Description { get; set; }
    public OptionalUpdate<int> EventTypeId { get; set; }
    public int? Version { get; set; }
    public bool? IsPublished { get; set; }
    public bool? IsActive { get; set; }
    public int? SortOrder { get; set; }
}

public sealed class UpdateEventTemplateDefinitionsDto
{
    public List<CreateEventTemplateDefinitionDto>? Items { get; set; }
}
