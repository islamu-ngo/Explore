// ABOUTME: Captures a single explicit session-template sync field delta using string-rendered values for deterministic diff output.
// ABOUTME: Shared by session definition and option modified DTOs in the event-session template sync workflow.

namespace Explore.Blazor.Client.Models.EventSessionTemplateSync;

public sealed record FieldChangeDto(
    string FieldName,
    string? OldValue,
    string? NewValue,
    string ValueType);
