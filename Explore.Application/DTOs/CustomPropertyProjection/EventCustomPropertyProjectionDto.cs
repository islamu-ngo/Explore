// ABOUTME: Full admin-shape DTO for an event custom-property projection row.
// ABOUTME: Includes all flags, typed values, and normalized value for admin inspection.

using Explore.Domain.Enums;

namespace Explore.Application.DTOs.CustomPropertyProjection;

public class EventCustomPropertyProjectionDto
{
    public Guid Id { get; set; }
    public Guid EventCustomPropertyDefinitionId { get; set; }
    public Guid EventCustomPropertyValueId { get; set; }
    public Guid EventId { get; set; }
    public Guid TenantId { get; set; }
    public string Namespace { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public PropertyType PropertyType { get; set; }
    public ExposureLevel ExposureLevel { get; set; }
    public bool IsSearchable { get; set; }
    public bool IsFilterable { get; set; }
    public bool IsExportable { get; set; }
    public bool IsModerationRelevant { get; set; }
    public bool IsAnalyticsRelevant { get; set; }
    public int Ordinal { get; set; }
    public Guid? OptionId { get; set; }
    public string? TextValue { get; set; }
    public decimal? NumberValue { get; set; }
    public bool? BooleanValue { get; set; }
    public DateTimeOffset? DateTimeValue { get; set; }
    public string? NormalizedValue { get; set; }
    public DateTime UpdatedAt { get; set; }
}
