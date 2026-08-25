// ABOUTME: Safe DTO for storage route-matrix administration and effective policy display.
// ABOUTME: Exposes route keys, provider choices, byte ceilings, and source metadata without destinations or credentials.

using Explore.Domain;

namespace Explore.Application.DTOs.Storage;

public sealed record StorageRouteSettingsDto
{
    public string RouteKey { get; init; } = StorageRouteKeys.General;
    public string Provider { get; init; } = StorageProviders.Local;
    public long MaxUploadBytes { get; init; } = 10 * 1024 * 1024;
    public string ProviderSource { get; init; } = "SystemDefault";
    public string MaxUploadSource { get; init; } = "SystemDefault";
    public bool IsReadOnly { get; init; }
}
