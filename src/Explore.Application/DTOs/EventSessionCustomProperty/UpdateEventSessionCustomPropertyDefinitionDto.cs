// ABOUTME: Grouped PATCH DTO for session-local custom-property definition updates.
// ABOUTME: Parent identity and provenance remain persisted while omitted groups preserve existing state.

using Explore.Application.DTOs.CustomPropertyDefinition;

namespace Explore.Application.DTOs.EventSessionCustomProperty;

public sealed class UpdateEventSessionCustomPropertyDefinitionDto
{
    public UpdateCustomPropertyDefinitionMetadataDto? Metadata { get; set; }
    public UpdateCustomPropertyDefinitionValidationDto? Validation { get; set; }
    public UpdateEventSessionCustomPropertyDefinitionOptionsDto? Options { get; set; }
}

public sealed class UpdateEventSessionCustomPropertyDefinitionOptionsDto
{
    public List<CreateEventSessionCustomPropertyOptionDto>? Items { get; set; }
}
