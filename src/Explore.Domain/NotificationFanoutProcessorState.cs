// ABOUTME: Persists cross-replica notification fanout processor coordination state.
// ABOUTME: Keeps optional-reminder backlog hysteresis durable across hosts and restarts.

namespace Explore.Domain;

public sealed class NotificationFanoutProcessorState
{
    public Guid Id { get; set; }
    public required string ProcessorCode { get; set; }
    public bool OptionalRemindersDeferred { get; set; }
    public DateTime UpdatedAt { get; set; }
}
