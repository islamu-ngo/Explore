// ABOUTME: Identifies a template-backed session runtime definition that should be retired because its source no longer exists in target version.
// ABOUTME: Carries the runtime concurrency stamp so apply can fail safely on concurrent edits.

namespace Explore.Blazor.Client.Models.EventSessionTemplateSync;

public sealed record RetiredDefinitionDto(
    string Namespace,
    string Key,
    Guid CurrentConcurrencyStamp);
