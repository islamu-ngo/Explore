// ABOUTME: Sources that can create report decisions before enforcement.
// ABOUTME: Distinguishes local moderators from provider, automation, and system decisions.

namespace Explore.Domain.Enums;

public enum EventReportDecisionSource
{
    LocalModerator = 1,
    OspreyAuto = 2,
    CoopReviewer = 3,
    System = 4
}
