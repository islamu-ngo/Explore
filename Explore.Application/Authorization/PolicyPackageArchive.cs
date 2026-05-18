// ABOUTME: Provider-neutral downloadable archive for authorization policy package fallback distribution.
// ABOUTME: Carries ZIP bytes and manifest metadata while keeping provider-specific archive construction in Infrastructure.

namespace Explore.Application.Authorization;

/// <summary>
/// Downloadable authorization policy package archive for manual operator installation.
/// </summary>
public sealed record PolicyPackageArchive(
    string FileName,
    string ContentType,
    byte[] Content,
    PolicyPackageManifest Manifest);
