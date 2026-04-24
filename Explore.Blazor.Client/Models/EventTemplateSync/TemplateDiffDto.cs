// ABOUTME: Aggregate event-template sync diff describing all add/modify/retire buckets for a requested target version.
// ABOUTME: Returned by the read-only diff query before an operator chooses a subset to apply.

namespace Explore.Blazor.Client.Models.EventTemplateSync;

public sealed record TemplateDiffDto(
    int TargetTemplateVersion,
    int BaseProvenanceVersion,
    IReadOnlyList<AddedDefinitionDto> AddedDefinitions,
    IReadOnlyList<ModifiedDefinitionDto> ModifiedDefinitions,
    IReadOnlyList<RetiredDefinitionDto> RetiredDefinitions,
    IReadOnlyList<AddedOptionDto> AddedOptions,
    IReadOnlyList<ModifiedOptionDto> ModifiedOptions,
    IReadOnlyList<RetiredOptionDto> RetiredOptions,
    IReadOnlyList<UntouchedLocalDefinitionDto> UntouchedLocalDefinitions);
