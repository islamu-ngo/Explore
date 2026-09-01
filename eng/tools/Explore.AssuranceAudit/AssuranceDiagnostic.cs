// ABOUTME: Defines bounded, value-free diagnostics emitted by the repository assurance audit.
// ABOUTME: Keeps category and syntax location stable without including source excerpts.

namespace Explore.AssuranceAudit;

public sealed record AssuranceDiagnostic(
    string Category,
    string Path,
    int Line,
    int Column,
    string Message)
{
    public override string ToString() => $"{Path}({Line},{Column}): {Category}: {Message}";
}
