// ABOUTME: Defines deterministic fake/replay AI usability scenario statuses.
// ABOUTME: Keeps report semantics stable without requiring live provider credentials.

namespace Explore.Diagnostic.AiReplay;

public enum AiReplayScenarioStatus
{
    Pass = 1,
    Warn = 2,
    Fail = 3
}
