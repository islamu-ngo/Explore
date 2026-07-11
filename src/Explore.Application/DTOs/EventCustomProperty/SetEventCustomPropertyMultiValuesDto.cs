// ABOUTME: Write DTO for atomically replacing all values of a multi-value custom property definition.
// ABOUTME: Wraps the definition ID, event ID, and the replacement value set for the API endpoint.

namespace Explore.Application.DTOs.EventCustomProperty;

public class SetEventCustomPropertyMultiValuesDto
{
    public Guid DefinitionId { get; set; }
    public Guid EventId { get; set; }
    public List<SetEventCustomPropertyValueDto> Values { get; set; } = [];
}
