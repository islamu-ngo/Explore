// ABOUTME: Categorizes doctor checks so CLI output remains stable and operator-friendly.
// ABOUTME: Categories are intentionally coarse-grained to avoid leaking environment details.

namespace Explore.Diagnostic.Doctor;

public enum DoctorCheckCategory
{
    Tooling,
    Configuration,
    Topology,
    Bootstrap,
    Documentation,
}
