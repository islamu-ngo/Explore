// ABOUTME: Deployment-managed local filesystem storage options.
// ABOUTME: RootPath is configured outside tenant/admin settings and is never browser-controlled.

namespace Explore.Infrastructure.Storage;

public sealed class LocalFileStorageOptions
{
    public const string SectionName = "Storage:Local";

    public string RootPath { get; set; } = "storage-data/local";

    public bool CreateRootIfMissing { get; set; } = true;
}
