// ABOUTME: Aggregates doctor check results and summarizes readiness status counts.
// ABOUTME: Used by CLI output and exit-code mapping without mutating application state.

namespace Explore.Diagnostic.Doctor;

public sealed record DoctorReport(IReadOnlyList<DoctorCheckResult> Results)
{
    public int PassCount => Results.Count(result => result.Status == DoctorCheckStatus.Pass);
    public int WarnCount => Results.Count(result => result.Status == DoctorCheckStatus.Warn);
    public int FailCount => Results.Count(result => result.Status == DoctorCheckStatus.Fail);
    public bool HasFailures => FailCount > 0;
}
