// ABOUTME: Carries an explicit evidence-based decision for one ambiguous IntegrationSync provider outcome.
// ABOUTME: Contains only a stable decision and opaque evidence reference, never subscriber data.

using Explore.Application.Contracts.Persistence;

namespace Explore.Application.DTOs.Integrations;

public sealed record ResolveIntegrationSyncAmbiguityDto
{
    public IntegrationSyncRecoveryDecision Decision { get; init; }
    public string EvidenceReference { get; init; } = string.Empty;
}
