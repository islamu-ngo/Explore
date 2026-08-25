// ABOUTME: Write DTO for atomically replacing all values of a multi-value session custom property definition.
// ABOUTME: Wraps the definition ID, session ID, and the replacement value set for the API endpoint.

namespace Explore.Application.DTOs.EventSessionCustomProperty;

public sealed record SetEventSessionCustomPropertyMultiValuesDto
{
    private IReadOnlyList<SetEventSessionCustomPropertyValueDto>? _values = Array.AsReadOnly(Array.Empty<SetEventSessionCustomPropertyValueDto>());

    public Guid DefinitionId { get; init; }
    public Guid EventSessionId { get; init; }
    public IReadOnlyList<SetEventSessionCustomPropertyValueDto> Values
    {
        get => _values!;
        init => _values = value is null ? null : Array.AsReadOnly(value.ToArray());
    }
}
