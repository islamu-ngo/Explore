// ABOUTME: Lightweight list DTO for event runtime custom property definitions.
// ABOUTME: Includes OptionCount and SourceTemplateId for quick provenance visibility.

using Explore.Domain.Enums;

namespace Explore.Application.DTOs.EventCustomProperty;

public sealed record EventCustomPropertyDefinitionListDto
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public string Namespace { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public PropertyType PropertyType { get; init; }
    public bool IsRequired { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
    public ExposureLevel ExposureLevel { get; init; }
    public Guid? SourceTemplateId { get; init; }
    public int OptionCount { get; init; }
}
