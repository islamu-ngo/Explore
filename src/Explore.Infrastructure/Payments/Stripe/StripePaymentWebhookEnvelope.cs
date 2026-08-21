// ABOUTME: Minimal Stripe payment evidence retained after exact raw-body signature verification.
// ABOUTME: Excludes Checkout customer, card, billing, shipping, and other buyer payload fields.

using System.Text.Json;

namespace Explore.Infrastructure.Payments.Stripe;

internal sealed record StripePaymentWebhookEnvelope(
    string EventId,
    string EventType,
    string ObjectId,
    string AccountId,
    bool LiveMode,
    string ApiRevision,
    DateTime CreatedAt)
{
    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this);

    public static StripePaymentWebhookEnvelope? Deserialize(ReadOnlySpan<byte> payload) =>
        JsonSerializer.Deserialize<StripePaymentWebhookEnvelope>(payload);
}
