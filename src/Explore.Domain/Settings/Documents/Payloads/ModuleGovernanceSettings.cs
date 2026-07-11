// ABOUTME: Tenant module-governance settings payload for typed document storage.
// ABOUTME: Stores non-secret module enablement defaults.

namespace Explore.Domain.Settings.Documents.Payloads;

public sealed record ModuleGovernanceSettings
{
    public bool IslamicModuleEnabled { get; init; } = true;

    public bool TechModuleEnabled { get; init; } = true;
}
