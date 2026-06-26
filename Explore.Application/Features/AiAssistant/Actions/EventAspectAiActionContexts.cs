// ABOUTME: Shared mapping contexts for event aspect AI proposal mappers.
// ABOUTME: Preserves event concurrency, aspect module, HAL context, and destructive confirmation data.

namespace Explore.Application.Features.AiAssistant.Actions;

public sealed record EventAspectAiPermissionContext(
    Guid ExpectedConcurrencyStamp,
    string AspectKind,
    bool ManagementContextHasEdit);

public sealed record EventAspectAiDestructiveContext(
    Guid ExpectedConcurrencyStamp,
    string AspectKind,
    bool ManagementContextHasEdit,
    string DestructiveSummary,
    string ConfirmationPhrase,
    bool AcknowledgedConsequences);
