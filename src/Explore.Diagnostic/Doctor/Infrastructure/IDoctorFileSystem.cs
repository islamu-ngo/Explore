// ABOUTME: File-system abstraction for doctor checks and tests.
// ABOUTME: Keeps check logic deterministic without touching real files in unit tests.

namespace Explore.Diagnostic.Doctor.Infrastructure;

public interface IDoctorFileSystem
{
    bool FileExists(string path);
    string ReadAllText(string path);
}
