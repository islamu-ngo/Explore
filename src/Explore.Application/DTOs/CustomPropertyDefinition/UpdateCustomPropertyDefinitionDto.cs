// ABOUTME: Grouped PATCH DTO for shared Layer 3 custom-property definition updates.
// ABOUTME: Route identity and tenant remain server-owned while relation and field groups preserve omitted state.

using Explore.Application.Models.Common;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.CustomPropertyDefinition;

public sealed class UpdateCustomPropertyDefinitionDto
{
    public UpdateCustomPropertyDefinitionRelationsDto? Relations { get; set; }
    public UpdateCustomPropertyDefinitionMetadataDto? Metadata { get; set; }
    public UpdateCustomPropertyDefinitionValidationDto? Validation { get; set; }
    public UpdateCustomPropertyDefinitionOptionsDto? Options { get; set; }
}

public sealed class UpdateCustomPropertyDefinitionRelationsDto
{
    public EntityTypeName? EntityTypeName { get; set; }
}

public sealed class UpdateCustomPropertyDefinitionMetadataDto
{
    public string? Namespace { get; set; }
    public string? Key { get; set; }
    public string? DisplayName { get; set; }
    public OptionalUpdate<string?> Description { get; set; } = OptionalUpdate<string?>.Unspecified();
    public bool? IsActive { get; set; }
    public int? SortOrder { get; set; }
    public ExposureLevel? ExposureLevel { get; set; }
    public bool? IsSearchable { get; set; }
    public bool? IsFilterable { get; set; }
    public bool? IsExportable { get; set; }
    public bool? IsModerationRelevant { get; set; }
    public bool? IsAnalyticsRelevant { get; set; }
    public bool? IsSystemOwned { get; set; }
}

public sealed class UpdateCustomPropertyDefinitionValidationDto
{
    public PropertyType? PropertyType { get; set; }
    public bool? IsRequired { get; set; }
    public bool? IsMulti { get; set; }
    public OptionalUpdate<string?> DefaultTextValue { get; set; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<decimal?> DefaultNumberValue { get; set; } = OptionalUpdate<decimal?>.Unspecified();
    public OptionalUpdate<bool?> DefaultBooleanValue { get; set; } = OptionalUpdate<bool?>.Unspecified();
    public OptionalUpdate<DateTimeOffset?> DefaultDateTimeValue { get; set; } = OptionalUpdate<DateTimeOffset?>.Unspecified();
    public OptionalUpdate<int?> MinLength { get; set; } = OptionalUpdate<int?>.Unspecified();
    public OptionalUpdate<int?> MaxLength { get; set; } = OptionalUpdate<int?>.Unspecified();
    public OptionalUpdate<string?> RegexPattern { get; set; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<decimal?> MinNumber { get; set; } = OptionalUpdate<decimal?>.Unspecified();
    public OptionalUpdate<decimal?> MaxNumber { get; set; } = OptionalUpdate<decimal?>.Unspecified();
    public OptionalUpdate<DateTimeOffset?> MinDateTime { get; set; } = OptionalUpdate<DateTimeOffset?>.Unspecified();
    public OptionalUpdate<DateTimeOffset?> MaxDateTime { get; set; } = OptionalUpdate<DateTimeOffset?>.Unspecified();
    public OptionalUpdate<string?> AllowedUrlSchemes { get; set; } = OptionalUpdate<string?>.Unspecified();
}

public sealed class UpdateCustomPropertyDefinitionOptionsDto
{
    public List<CreateCustomPropertyOptionDto>? Items { get; set; }
}
