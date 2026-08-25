// ABOUTME: Provider-neutral downloadable archive for authorization policy package fallback distribution.
// ABOUTME: Carries ZIP bytes and manifest metadata while keeping provider-specific archive construction in Infrastructure.

namespace Explore.Application.Authorization;

/// <summary>
/// Downloadable authorization policy package archive for manual operator installation.
/// </summary>
public sealed record PolicyPackageArchive
{
    public PolicyPackageArchive(
        string FileName,
        string ContentType,
        ReadOnlyMemory<byte> Content,
        PolicyPackageManifest Manifest)
    {
        this.FileName = FileName;
        this.ContentType = ContentType;
        this.Content = Content.ToArray();
        this.Manifest = Manifest;
    }

    public string FileName { get; }
    public string ContentType { get; }
    public ReadOnlyMemory<byte> Content { get; }
    public PolicyPackageManifest Manifest { get; }
}
