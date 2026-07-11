// ABOUTME: Queue priority levels for report review and provider signals.
// ABOUTME: Keeps moderation triage urgency bounded to stable values.

namespace Explore.Domain.Enums;

public enum EventReportPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Urgent = 4
}
