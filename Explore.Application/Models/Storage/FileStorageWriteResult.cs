// ABOUTME: Provider-neutral result returned after storage accepts file bytes.
// ABOUTME: Carries internal object key and integrity metadata for StorageObject persistence.

namespace Explore.Application.Models.Storage;

public sealed record FileStorageWriteResult(
    string Provider,
    string ObjectKey,
    long SizeBytes,
    string ContentType,
    string? Sha256Checksum);
