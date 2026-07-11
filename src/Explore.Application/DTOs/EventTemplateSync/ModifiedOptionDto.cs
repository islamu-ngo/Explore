// ABOUTME: Describes a template-backed runtime option whose fields differ from the target template option.
// ABOUTME: Carries the current concurrency stamp so apply can reject stale per-option edits deterministically.

namespace Explore.Application.DTOs.EventTemplateSync;

public sealed record ModifiedOptionDto(
    string Namespace,
    string Key,
    Guid CurrentConcurrencyStamp,
    IReadOnlyList<FieldChangeDto> FieldChanges);
