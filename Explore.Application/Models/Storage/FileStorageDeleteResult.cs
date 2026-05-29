// ABOUTME: Provider-neutral delete result for idempotent storage byte removal.
// ABOUTME: Missing files are represented without throwing so cleanup/retry flows remain stable.

namespace Explore.Application.Models.Storage;

public sealed record FileStorageDeleteResult(
    string Provider,
    string ObjectKey,
    bool Deleted);
