// ABOUTME: Marks idempotent endpoints whose persisted replay body and selected headers require Data Protection.
// ABOUTME: Keeps short-lived capabilities replayable without storing them as plaintext in IdempotencyRecord.

namespace Explore.API.Attributes;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method,
    Inherited = true,
    AllowMultiple = false)]
public sealed class ProtectIdempotencyReplayAttribute(params string[] responseHeaders) : Attribute
{
    public IReadOnlyList<string> ResponseHeaders { get; } = responseHeaders;
}
