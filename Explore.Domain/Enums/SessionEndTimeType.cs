// ABOUTME: Defines the session end-time strategy enum for flexible and contextual event schedule resolution.
// ABOUTME: Supports fixed standard times, open-ended durations, and prayer-relative contextual endings.

namespace Explore.Domain.Enums;

public enum SessionEndTimeType
{
    Fixed = 0,
    OpenEnded = 1,
    RelativeToPrayer = 2
}
