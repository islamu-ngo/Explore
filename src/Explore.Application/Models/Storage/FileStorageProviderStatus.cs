// ABOUTME: Provider-neutral health/status snapshot for storage providers.
// ABOUTME: Avoids exposing host paths, provider secrets, raw object keys, or presigned URLs.

namespace Explore.Application.Models.Storage;

public sealed record FileStorageProviderStatus(
    string Provider,
    bool IsAvailable,
    bool SupportsServerSideStreaming,
    bool SupportsBrowserDirectUpload,
    string? FailureCode = null,
    string? Message = null,
    S3PreflightResult? Preflight = null);
