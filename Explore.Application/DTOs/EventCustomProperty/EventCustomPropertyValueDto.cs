// ABOUTME: Read-only DTO for event custom property values, the actual data stored per-event.
// ABOUTME: Typed value columns are mutually exclusive based on the definition's PropertyType.

namespace Explore.Application.DTOs.EventCustomProperty;

public class EventCustomPropertyValueDto
{
    public Guid Id { get; set; }
    public Guid EventCustomPropertyDefinitionId { get; set; }
    public Guid EventId { get; set; }
    public int Ordinal { get; set; }
    public string? TextValue { get; set; }
    public decimal? NumberValue { get; set; }
    public bool? BooleanValue { get; set; }
    public DateTimeOffset? DateTimeValue { get; set; }
    public Guid? OptionId { get; set; }
}
