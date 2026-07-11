// ABOUTME: Describes a template option that exists in the target template but not in the current event runtime state.
// ABOUTME: Used by diff output and embedded in added-definition snapshots for event template sync.

namespace Explore.Application.DTOs.EventTemplateSync;

public sealed record AddedOptionDto(
    string Namespace,
    string Key,
    string DisplayName,
    string? Description,
    string Value,
    bool IsDefault,
    bool IsActive,
    int SortOrder,
    string? ParentOptionKey);
