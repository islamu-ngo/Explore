// ABOUTME: Optional reporter or provider severity hints for moderation reports.
// ABOUTME: Keeps severity advisory and separate from final human decision outcomes.

namespace Explore.Domain.Enums;

public enum EventReportSeverityHint
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}
