// ABOUTME: Defines the stable PASS/WARN/FAIL states emitted by the doctor CLI.
// ABOUTME: FAIL is reserved for hard readiness blockers and controls the process exit code.

namespace Explore.Diagnostic.Doctor;

public enum DoctorCheckStatus
{
    Pass,
    Warn,
    Fail,
}
