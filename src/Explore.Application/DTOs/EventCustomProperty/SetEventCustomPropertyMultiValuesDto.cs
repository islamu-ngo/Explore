// ABOUTME: Write DTO for atomically replacing all values of a multi-value custom property definition.
// ABOUTME: Wraps the definition ID, event ID, and the replacement value set for the API endpoint.

namespace Explore.Application.DTOs.EventCustomProperty;

public sealed record SetEventCustomPropertyMultiValuesDto
{
    public Guid DefinitionId { get; init; }
    public Guid EventId { get; init; }
    public List<SetEventCustomPropertyValueDto> Values { get; init; } = [];
}
