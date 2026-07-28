// ABOUTME: Grouped PATCH DTO for event-local custom-property definition updates.
// ABOUTME: Parent identity and provenance remain persisted while omitted groups preserve existing state.

using Explore.Application.DTOs.CustomPropertyDefinition;

namespace Explore.Application.DTOs.EventCustomProperty;

public sealed class UpdateEventCustomPropertyDefinitionDto
{
    public UpdateCustomPropertyDefinitionMetadataDto? Metadata { get; set; }
    public UpdateCustomPropertyDefinitionValidationDto? Validation { get; set; }
    public UpdateEventCustomPropertyDefinitionOptionsDto? Options { get; set; }
}

public sealed class UpdateEventCustomPropertyDefinitionOptionsDto
{
    public List<CreateEventCustomPropertyOptionDto>? Items { get; set; }
}
