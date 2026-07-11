// ABOUTME: Read-only DTO for event session custom property values, the actual data stored per-session.
// ABOUTME: Typed value columns are mutually exclusive based on the definition's PropertyType.

namespace Explore.Application.DTOs.EventSessionCustomProperty;

public class EventSessionCustomPropertyValueDto
{
    public Guid Id { get; set; }
    public Guid EventSessionCustomPropertyDefinitionId { get; set; }
    public Guid EventSessionId { get; set; }
    public int Ordinal { get; set; }
    public string? TextValue { get; set; }
    public decimal? NumberValue { get; set; }
    public bool? BooleanValue { get; set; }
    public DateTimeOffset? DateTimeValue { get; set; }
    public Guid? OptionId { get; set; }
}
