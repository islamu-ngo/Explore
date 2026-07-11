// ABOUTME: Provider-neutral existence check input for storage objects.
// ABOUTME: Keeps reconciliation and health probes from opening full content streams.

namespace Explore.Application.Models.Storage;

public sealed record FileStorageExistsInput(string ObjectKey);
