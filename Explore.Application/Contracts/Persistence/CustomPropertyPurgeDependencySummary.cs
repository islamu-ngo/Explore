// ABOUTME: Dependency-count contract used before irreversible custom-property hard purges.
// ABOUTME: Keeps purge eligibility decisions in Application without leaking DbContext details.

namespace Explore.Application.Contracts.Persistence;

public sealed record CustomPropertyPurgeDependencySummary(
    Guid DefinitionId,
    Guid TenantId,
    string Scope,
    int OptionCount,
    int ValueCount,
    int ProjectionCount,
    int AuditLogCount,
    int SyncProvenanceCount)
{
    public bool HasBlockingDependencies =>
        ValueCount > 0
        || ProjectionCount > 0
        || AuditLogCount > 0
        || SyncProvenanceCount > 0;
}
