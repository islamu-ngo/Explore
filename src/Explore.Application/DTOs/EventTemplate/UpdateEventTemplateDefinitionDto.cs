// ABOUTME: Write DTO for updating template property definitions, extends create DTO with Id.
// ABOUTME: Replaces all options on update (full replacement, not partial patch).

namespace Explore.Application.DTOs.EventTemplate;

public sealed record UpdateEventTemplateDefinitionDto : CreateEventTemplateDefinitionDto
{
    public Guid Id { get; init; }
}
