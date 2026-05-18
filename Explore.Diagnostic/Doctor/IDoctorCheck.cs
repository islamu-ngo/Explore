// ABOUTME: Contract for non-mutating platform doctor checks.
// ABOUTME: Implementations may inspect files/process output but must not repair or change state.

namespace Explore.Diagnostic.Doctor;

public interface IDoctorCheck
{
    string Code { get; }
    DoctorCheckCategory Category { get; }
    Task<DoctorCheckResult> RunAsync(CancellationToken cancellationToken);
}
