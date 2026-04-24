// ABOUTME: Captures a single explicit template-sync field delta using string-rendered values for deterministic diff output.
// ABOUTME: Shared by definition and option modified DTOs in the event template sync workflow.

namespace Explore.Blazor.Client.Models.EventTemplateSync;

public sealed record FieldChangeDto(
    string FieldName,
    string? OldValue,
    string? NewValue,
    string ValueType);
