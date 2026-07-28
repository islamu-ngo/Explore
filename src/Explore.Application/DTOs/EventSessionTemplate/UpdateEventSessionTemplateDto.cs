// ABOUTME: Grouped partial-update contract for event session template metadata and definitions.
// ABOUTME: Identity, parent ownership, and concurrency are server-owned rather than body-owned.

using Explore.Application.Models.Common;

namespace Explore.Application.DTOs.EventSessionTemplate;

public sealed class UpdateEventSessionTemplateDto
{
    public UpdateEventSessionTemplateMetadataDto? Metadata { get; set; }
    public UpdateEventSessionTemplateDefinitionsDto? Definitions { get; set; }
}

public sealed class UpdateEventSessionTemplateMetadataDto
{
    public string? SessionTemplateKey { get; set; }
    public string? DisplayName { get; set; }
    public OptionalUpdate<string> Description { get; set; }
    public int? Version { get; set; }
    public bool? IsPublished { get; set; }
    public bool? IsActive { get; set; }
    public int? SortOrder { get; set; }
}

public sealed class UpdateEventSessionTemplateDefinitionsDto
{
    public List<CreateEventSessionTemplateDefinitionDto>? Items { get; set; }
}
