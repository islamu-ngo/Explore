// ABOUTME: Supplements generated analytics client contracts until OpenAPI emits nested record schemas.
// ABOUTME: Keeps Studio analytics on IEventApiClient types instead of page-local transport mirrors.

using System.Text.Json.Serialization;

namespace Explore.Blazor.Client.Clients;

public partial class HalResourceOfRegistrationAnswerAnalyticsDto
{
    [JsonPropertyName("eventId")]
    public Guid EventId { get; set; }

    [JsonPropertyName("formId")]
    public Guid FormId { get; set; }

    [JsonPropertyName("formVersionId")]
    public Guid FormVersionId { get; set; }

    [JsonPropertyName("minimumCellSize")]
    public int MinimumCellSize { get; set; }

    [JsonPropertyName("fields")]
    public ICollection<RegistrationAnswerFieldAggregateDto> Fields { get; set; } = [];

    [JsonPropertyName("_links")]
    public IDictionary<string, HalLink>? _links { get; set; }
}

public sealed class RegistrationAnswerFieldAggregateDto
{
    [JsonPropertyName("fieldId")]
    public Guid FieldId { get; set; }

    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = string.Empty;

    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("fieldTypeId")]
    public int FieldTypeId { get; set; }

    [JsonPropertyName("fieldTypeCode")]
    public string FieldTypeCode { get; set; } = string.Empty;

    [JsonPropertyName("isOperationallyFilterable")]
    public bool IsOperationallyFilterable { get; set; }

    [JsonPropertyName("responseCount")]
    public long ResponseCount { get; set; }

    [JsonPropertyName("cells")]
    public ICollection<RegistrationAnswerAggregateCellDto> Cells { get; set; } = [];

    [JsonPropertyName("numeric")]
    public RegistrationAnswerNumericAggregateDto? Numeric { get; set; }
}

public sealed class RegistrationAnswerAggregateCellDto
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public long Count { get; set; }
}

public sealed class RegistrationAnswerNumericAggregateDto
{
    [JsonPropertyName("count")]
    public long Count { get; set; }

    [JsonPropertyName("min")]
    public decimal Min { get; set; }

    [JsonPropertyName("max")]
    public decimal Max { get; set; }

    [JsonPropertyName("average")]
    public decimal Average { get; set; }
}
