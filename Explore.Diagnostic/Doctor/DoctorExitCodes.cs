// ABOUTME: Centralizes doctor process exit-code semantics.
// ABOUTME: Keeps warnings non-blocking while hard FAIL checks return a non-zero code.

namespace Explore.Diagnostic.Doctor;

public static class DoctorExitCodes
{
    public const int Success = 0;
    public const int HardFailure = 1;

    public static int FromReport(DoctorReport report) => report.HasFailures ? HardFailure : Success;
}
