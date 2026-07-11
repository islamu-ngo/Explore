// ABOUTME: Temporal view enum for event list filtering (Upcoming, Ongoing, Past, etc.).
// ABOUTME: Mirrors the Application-layer enum for use in the Blazor client filter bar.

namespace Explore.Blazor.Client.Models;

/// <summary>
/// Temporal view filter for event listing queries.
/// </summary>
public enum TemporalView
{
    Upcoming,
    Ongoing,
    Past,
    UpcomingAndOngoing,
    All
}
