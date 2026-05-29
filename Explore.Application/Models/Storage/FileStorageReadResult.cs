// ABOUTME: Provider-neutral read result containing an open stream and response metadata.
// ABOUTME: Callers own stream disposal and may expose range processing at the API boundary.

namespace Explore.Application.Models.Storage;

public sealed record FileStorageReadResult(
    Stream Content,
    string ContentType,
    long Length,
    DateTimeOffset? LastModified);
