// ABOUTME: Defines the safe AI-proposed payload shape for event publish proposals.
// ABOUTME: Carries only event identity, optimistic concurrency, and bounded readiness context.

namespace Explore.Application.Features.AiAssistant.Actions;

public sealed class PublishEventAiActionPayload
{
    public Guid? EventId { get; init; }

    public Guid? ExpectedConcurrencyStamp { get; init; }

    public bool? ReadinessIsReady { get; init; }

    public int? ReadinessErrorCount { get; init; }

    public DateTimeOffset? ReadinessCheckedAtUtc { get; init; }

    public string? ReadinessSummary { get; init; }
}
