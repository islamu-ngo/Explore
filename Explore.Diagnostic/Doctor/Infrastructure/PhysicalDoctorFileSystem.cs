// ABOUTME: Production file-system adapter for read-only doctor checks.
// ABOUTME: Exposes only read operations so doctor checks cannot write through this abstraction.

namespace Explore.Diagnostic.Doctor.Infrastructure;

public sealed class PhysicalDoctorFileSystem : IDoctorFileSystem
{
    public bool FileExists(string path) => File.Exists(path);

    public string ReadAllText(string path) => File.ReadAllText(path);
}
