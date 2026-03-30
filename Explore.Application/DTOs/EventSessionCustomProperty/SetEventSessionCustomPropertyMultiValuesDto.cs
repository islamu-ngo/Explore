// ABOUTME: Write DTO for atomically replacing all values of a multi-value session custom property definition.
// ABOUTME: Wraps the definition ID, session ID, and the replacement value set for the API endpoint.

namespace Explore.Application.DTOs.EventSessionCustomProperty;

public class SetEventSessionCustomPropertyMultiValuesDto
{
    public Guid DefinitionId { get; set; }
    public Guid EventSessionId { get; set; }
    public List<SetEventSessionCustomPropertyValueDto> Values { get; set; } = [];
}
