# Distributed Bundle File Writer

## Problem Statement

`BundleFileWriter` writes exported TMS bundles to `{ContentRoot}/App_Data/Localization/Bundles/{code}.json` on the **local filesystem**. This is correct for single-instance deployments and deployments with a shared persistent volume, but it is **not HA-safe** behind a load balancer without shared storage.

In multi-replica deployments:
- An export from one replica writes the bundle locally; other replicas don't see it.
- `OfflineTranslationProvider` falls back to embedded resources on replicas that didn't export.
- The admin UI health banner will show "writable" on one replica and potentially different data on another.

## Interface Contract

The existing `IBundleFileWriter` contract (`Explore.Application/Contracts/Infrastructure/IBundleFileWriter.cs`) is the seam:

```csharp
public interface IBundleFileWriter
{
    Task<string> WriteBundleAsync(string languageCode, IReadOnlyDictionary<string, string> translations, CancellationToken ct = default);
    Task<WritablePathHealth> CheckHealthAsync(CancellationToken ct = default);
}
```

**No changes to call sites required.** A new `DistributedBundleFileWriter` implementation would be registered via deployment configuration (e.g., `appsettings.Production.json` or environment variable).

## Candidate Implementations

### 1. S3/Blob Storage Writer
- Write bundles to S3 (or Azure Blob, MinIO, etc.)
- `OfflineTranslationProvider` reads from blob on cache miss
- Pros: truly distributed, no shared filesystem needed
- Cons: requires object-store dependency, latency on first read

### 2. Shared Volume Writer (current `BundleFileWriter` + ops config)
- Mount a shared PVC / NFS / EFS volume at `App_Data/Localization/Bundles/`
- Zero code change — works today
- Pros: simplest
- Cons: requires infra config, not all hosting environments support it

### 3. HybridCache-backed Writer
- Write to both local disk and `IDistributedCache` (Redis/Valkey)
- `OfflineTranslationProvider` checks distributed cache first
- Pros: fast reads, leverages existing caching infra
- Cons: cache TTL management, potential staleness

## Acceptance Criteria

- [ ] `IBundleFileWriter` contract unchanged
- [ ] New implementation registered via deployment config (DI condition or feature flag)
- [ ] All replicas see the same bundle content after export
- [ ] Health check accurately reflects the distributed store's availability
- [ ] Existing unit tests for `ExportFromTmsCommandHandler` pass without modification
- [ ] Integration test verifies cross-instance visibility

## References

- `docs/LOCALIZATION.md` → "Bundle Persistence & HA Constraint"
- `Explore.Application/Contracts/Infrastructure/IBundleFileWriter.cs`
- `Explore.Infrastructure/Localization/BundleFileWriter.cs`
- Admin UI health banner in `InstanceLocalizationSection.razor`
