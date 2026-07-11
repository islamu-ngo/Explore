// ABOUTME: Safe DTO for storage route-matrix administration and effective policy display.
// ABOUTME: Exposes route keys, provider choices, byte ceilings, and source metadata without destinations or credentials.

using Explore.Domain;

namespace Explore.Application.DTOs.Storage;

public class StorageRouteSettingsDto
{
    public string RouteKey { get; set; } = StorageRouteKeys.General;
    public string Provider { get; set; } = StorageProviders.Local;
    public long MaxUploadBytes { get; set; } = 10 * 1024 * 1024;
    public string ProviderSource { get; set; } = "SystemDefault";
    public string MaxUploadSource { get; set; } = "SystemDefault";
    public bool IsReadOnly { get; set; }
}
