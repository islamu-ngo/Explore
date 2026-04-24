// ABOUTME: Snapshot of a definition that would be added to an event session from the target session template during sync.
// ABOUTME: Includes full mutable/runtime-visible definition metadata plus its template option set.

namespace Explore.Blazor.Client.Models.EventSessionTemplateSync;

public sealed record AddedDefinitionDto(
    string Namespace,
    string Key,
    string DisplayName,
    string? Description,
    string PropertyType,
    bool IsRequired,
    bool IsMultiValue,
    string? DefaultValue,
    string ExposureLevel,
    bool IsSearchable,
    bool IsFilterable,
    bool IsExportable,
    bool IsModerationRelevant,
    bool IsAnalyticsRelevant,
    bool IsSystemOwned,
    int? MinLength,
    int? MaxLength,
    string? RegexPattern,
    decimal? MinNumber,
    decimal? MaxNumber,
    DateTimeOffset? MinDateTime,
    DateTimeOffset? MaxDateTime,
    string? AllowedUrlSchemes,
    IReadOnlyList<AddedOptionDto> Options);
