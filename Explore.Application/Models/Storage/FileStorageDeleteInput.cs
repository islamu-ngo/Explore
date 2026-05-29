// ABOUTME: Provider-neutral request for idempotently deleting stored bytes by internal object key.
// ABOUTME: Delete orchestration and metadata lifecycle decisions remain in application handlers.

namespace Explore.Application.Models.Storage;

public sealed record FileStorageDeleteInput(string ObjectKey);
