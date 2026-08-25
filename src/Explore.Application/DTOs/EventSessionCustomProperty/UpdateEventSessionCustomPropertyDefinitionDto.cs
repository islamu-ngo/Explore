// ABOUTME: Grouped PATCH DTO for session-local custom-property definition updates.
// ABOUTME: Parent identity and provenance remain persisted while omitted groups preserve existing state.

using Explore.Application.DTOs.CustomPropertyDefinition;

namespace Explore.Application.DTOs.EventSessionCustomProperty;

public sealed record UpdateEventSessionCustomPropertyDefinitionDto
{
    public UpdateCustomPropertyDefinitionMetadataDto? Metadata { get; init; }
    public UpdateCustomPropertyDefinitionValidationDto? Validation { get; init; }
    public UpdateEventSessionCustomPropertyDefinitionOptionsDto? Options { get; init; }
}

public sealed record UpdateEventSessionCustomPropertyDefinitionOptionsDto
{
    public List<CreateEventSessionCustomPropertyOptionDto>? Items { get; init; }
}
