// ABOUTME: Provider-neutral request model for writing file bytes to configured storage.
// ABOUTME: Keeps upload policy metadata separate from provider-specific object paths and URLs.

namespace Explore.Application.Models.Storage;

public sealed record FileStorageWriteInput(
    Guid TenantId,
    Stream Content,
    string ContentType,
    string SafeDisplayName,
    string? Extension,
    long? ExpectedSizeBytes,
    long? MaxSizeBytes);
