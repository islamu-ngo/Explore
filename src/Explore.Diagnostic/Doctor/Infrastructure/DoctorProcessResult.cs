// ABOUTME: Captures read-only process check output for doctor evaluations.
// ABOUTME: Callers must redact output before printing it to operators.

namespace Explore.Diagnostic.Doctor.Infrastructure;

public sealed record DoctorProcessResult(int ExitCode, string StandardOutput, string StandardError);
