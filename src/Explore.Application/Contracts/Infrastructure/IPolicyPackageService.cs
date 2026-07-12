// ABOUTME: Application-layer seam for describing and publishing authorization policy packages.
// ABOUTME: Keeps provider-specific file discovery, transport, ZIP, and Admin API details in Infrastructure.

using Explore.Application.Authorization;

namespace Explore.Application.Contracts.Infrastructure;

/// <summary>
/// Builds provider-neutral policy package manifests and publishes the current package through Infrastructure.
/// </summary>
public interface IPolicyPackageService
{
    /// <summary>
    /// Builds a provider-neutral manifest for the current authorization policy package without publishing it.
    /// </summary>
    Task<PolicyPackageManifest> BuildManifestAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes the current authorization policy package to the configured provider.
    /// </summary>
    Task<PolicyPackagePublishResult> PublishAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes the current authorization policy package to the instance-managed provider target.
    /// </summary>
    Task<PolicyPackagePublishResult> PublishInstanceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an operator-safe status summary for the current authorization policy package target.
    /// </summary>
    Task<PolicyPackageStatusResult> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports the current authorization policy package as a downloadable archive for manual installation.
    /// </summary>
    Task<PolicyPackageArchive> ExportArchiveAsync(CancellationToken cancellationToken = default);
}
