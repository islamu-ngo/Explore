// ABOUTME: Provider-neutral result for quarantining a backing object.
// ABOUTME: Reports only bounded outcome metadata and never exposes local filesystem paths.

namespace Explore.Application.Models.Storage;

public sealed record FileStorageQuarantineResult(
    string Provider,
    string ObjectKey,
    bool Quarantined);
