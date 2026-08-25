// ABOUTME: Write DTO for updating session template property definitions, extends create with Id.
// ABOUTME: Manually instantiated validator pattern, following project convention.

namespace Explore.Application.DTOs.EventSessionTemplate;

public sealed record UpdateEventSessionTemplateDefinitionDto : CreateEventSessionTemplateDefinitionDto
{
    public Guid Id { get; init; }
}
