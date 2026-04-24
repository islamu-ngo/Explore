// ABOUTME: Describes a template-backed session runtime definition whose explicit fields differ from the target template version.
// ABOUTME: Carries the runtime concurrency stamp to support precise concurrent-update conflict reporting during apply.

namespace Explore.Blazor.Client.Models.EventSessionTemplateSync;

public sealed record ModifiedDefinitionDto(
    string Namespace,
    string Key,
    Guid CurrentConcurrencyStamp,
    IReadOnlyList<FieldChangeDto> FieldChanges);
