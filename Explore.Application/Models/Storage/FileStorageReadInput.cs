// ABOUTME: Provider-neutral request for opening stored file bytes by internal object key.
// ABOUTME: The object key remains server-side metadata and is not a public browser addressing contract.

namespace Explore.Application.Models.Storage;

public sealed record FileStorageReadInput(
    string ObjectKey,
    string? ContentType);
