// ABOUTME: Write DTO for setting event custom property values (single or multi-value).
// ABOUTME: Ordinal distinguishes multi-value entries; single-value uses Ordinal=0.

namespace Explore.Application.DTOs.EventCustomProperty;

public class SetEventCustomPropertyValueDto
{
    public Guid EventCustomPropertyDefinitionId { get; set; }
    public Guid EventId { get; set; }
    public int Ordinal { get; set; }
    public string? TextValue { get; set; }
    public decimal? NumberValue { get; set; }
    public bool? BooleanValue { get; set; }
    public DateTimeOffset? DateTimeValue { get; set; }
    public Guid? OptionId { get; set; }
}
