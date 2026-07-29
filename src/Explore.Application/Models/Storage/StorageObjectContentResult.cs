// ABOUTME: Application result for streaming stored object content through API endpoints.
// ABOUTME: Carries the provider stream and response metadata without exposing object keys or paths.

namespace Explore.Application.Models.Storage;

public sealed record StorageObjectContentResult(
    Stream Content,
    string ContentType,
    long Length,
    DateTimeOffset? LastModified,
    string? Sha256Checksum,
    string SafeDisplayName = "download",
    bool ShouldDownloadAsAttachment = true);
