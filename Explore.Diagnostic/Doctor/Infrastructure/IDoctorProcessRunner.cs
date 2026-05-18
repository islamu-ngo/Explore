// ABOUTME: Process abstraction for read-only doctor command checks.
// ABOUTME: Lets tests prove command choices without invoking external tools.

namespace Explore.Diagnostic.Doctor.Infrastructure;

public interface IDoctorProcessRunner
{
    Task<DoctorProcessResult> RunAsync(string fileName, string arguments, CancellationToken cancellationToken);
}
