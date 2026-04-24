// ABOUTME: Identifies a template-backed runtime definition that should be retired because its source no longer exists in target version.
// ABOUTME: Carries the runtime concurrency stamp so apply can fail safely on concurrent edits.

namespace Explore.Blazor.Client.Models.EventTemplateSync;

public sealed record RetiredDefinitionDto(
    string Namespace,
    string Key,
    Guid CurrentConcurrencyStamp);
