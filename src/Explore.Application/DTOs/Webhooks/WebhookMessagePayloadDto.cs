// ABOUTME: Dedicated sensitive response contract for retained outgoing webhook payload bytes.
// ABOUTME: Encodes exact bytes as base64 and remains separate from default message list/detail DTOs.

namespace Explore.Application.DTOs.Webhooks;

public sealed record WebhookMessagePayloadDto
{
    public Guid MessageId { get; init; }

    public required string ContentType { get; init; }

    public required string ContentEncoding { get; init; }

    public required string PayloadBase64 { get; init; }

    public required string PayloadHash { get; init; }

    public long PayloadByteLength { get; init; }

    public DateTime PayloadRetentionUntil { get; init; }

    public DateTime RetrievedAt { get; init; }
}
