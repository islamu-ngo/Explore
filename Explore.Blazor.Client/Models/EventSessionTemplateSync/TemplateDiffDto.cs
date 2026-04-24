// ABOUTME: Aggregate session-template sync diff describing all add/modify/retire buckets for a requested target version.
// ABOUTME: Returned by the read-only diff query before an operator chooses a subset to apply.

using System.Text.Json.Serialization;

namespace Explore.Blazor.Client.Models.EventSessionTemplateSync;

public sealed record TemplateDiffDto(
    int TargetTemplateVersion,
    int BaseProvenanceVersion,
    IReadOnlyList<AddedDefinitionDto> AddedDefinitions,
    IReadOnlyList<ModifiedDefinitionDto> ModifiedDefinitions,
    IReadOnlyList<RetiredDefinitionDto> RetiredDefinitions,
    IReadOnlyList<AddedOptionDto> AddedOptions,
    IReadOnlyList<ModifiedOptionDto> ModifiedOptions,
    IReadOnlyList<RetiredOptionDto> RetiredOptions,
    IReadOnlyList<UntouchedLocalDefinitionDto> UntouchedLocalDefinitions,
    [property: JsonPropertyName("_links")] IReadOnlyDictionary<string, HalLinkDto>? Links = null);
