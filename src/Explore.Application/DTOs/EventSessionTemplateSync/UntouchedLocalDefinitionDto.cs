// ABOUTME: Surfaces session runtime-only local definitions that are intentionally excluded from operator-driven template sync diffs.
// ABOUTME: Warns the caller when local additions or local retirements exist alongside template-managed definitions.

namespace Explore.Application.DTOs.EventSessionTemplateSync;

public sealed record UntouchedLocalDefinitionDto(
    string Namespace,
    string Key,
    string Reason);
