// ABOUTME: Grouped PATCH DTO for event-local custom-property definition updates.
// ABOUTME: Parent identity and provenance remain persisted while omitted groups preserve existing state.

using Explore.Application.DTOs.CustomPropertyDefinition;

namespace Explore.Application.DTOs.EventCustomProperty;

public sealed record UpdateEventCustomPropertyDefinitionDto
{
    public UpdateCustomPropertyDefinitionMetadataDto? Metadata { get; init; }
    public UpdateCustomPropertyDefinitionValidationDto? Validation { get; init; }
    public UpdateEventCustomPropertyDefinitionOptionsDto? Options { get; init; }
}

public sealed record UpdateEventCustomPropertyDefinitionOptionsDto
{
    public List<CreateEventCustomPropertyOptionDto>? Items { get; init; }
}
