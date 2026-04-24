// ABOUTME: Identifies a runtime option whose source template option no longer exists in the requested target version.
// ABOUTME: Carries concurrency metadata so the sync apply path can retire without silent overwrites.

namespace Explore.Application.DTOs.EventTemplateSync;

public sealed record RetiredOptionDto(
    string Namespace,
    string Key,
    Guid CurrentConcurrencyStamp);
