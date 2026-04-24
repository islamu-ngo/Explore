// ABOUTME: Describes a session-template option that exists in the target template but not in the current runtime state.
// ABOUTME: Used by session diff output and embedded in added-definition snapshots.

namespace Explore.Blazor.Client.Models.EventSessionTemplateSync;

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
