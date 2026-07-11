// ABOUTME: Providers that can attach automated moderation signals to reports.
// ABOUTME: Keeps external signal provenance bounded to known source categories.

namespace Explore.Domain.Enums;

public enum EventReportSignalProvider
{
    Local = 1,
    Osprey = 2,
    Coop = 3,
    Model = 4,
    System = 5
}
