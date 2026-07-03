// ABOUTME: Unit tests for webhook payload envelope creation and minimization.
// ABOUTME: Guards stable hashes, retention calculation, strict data allow-lists, and heavy-redaction privacy.

using System.Text.Json;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Webhooks;

namespace Event.Application.UnitTests.Webhooks;

public sealed class DefaultWebhookPayloadBuilderTests
{
    private static readonly Guid MessageId = Guid.Parse("018f0000-0000-7000-8000-000000000999");
    private static readonly Guid TenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");
    private static readonly Guid AggregateId = Guid.Parse("018f0000-0000-7000-8000-000000000001");
    private static readonly DateTimeOffset OccurredAt = new(2026, 7, 2, 10, 0, 0, TimeSpan.Zero);

    private readonly DefaultWebhookPayloadBuilder _builder = new(new WebhookEventTypeRegistry());

    [Test]
    public async Task BuildAsync_ForPublishedEvent_CreatesStableEnvelopeHashAndRetention()
    {
        var context = CreateContext(
            WebhookEventNames.EventPublished,
            new Dictionary<string, object?>
            {
                ["eventId"] = AggregateId.ToString(),
                ["status"] = "Published",
                ["publicUrl"] = "https://example.org/events/example-event",
                ["internalNote"] = "must not be included"
            });

        var result = await _builder.BuildAsync(context, CancellationToken.None);

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.Envelope!.Id).IsEqualTo(MessageId);
        await Assert.That(result.Envelope.Type).IsEqualTo(WebhookEventNames.EventPublished);
        await Assert.That(result.Envelope.Version).IsEqualTo(1);
        await Assert.That(result.PayloadHash).IsNotNull();
        await Assert.That(result.PayloadHash!.Length).IsEqualTo(64);
        await Assert.That(result.PayloadRetentionUntil).IsEqualTo(OccurredAt.AddDays(14));
        await Assert.That(result.RawPayloadJson!).Contains("publicUrl");
        await Assert.That(result.RawPayloadJson!).DoesNotContain("internalNote");

        using var parsed = JsonDocument.Parse(result.RawPayloadJson!);
        await Assert.That(parsed.RootElement.GetProperty("tenantId").GetGuid()).IsEqualTo(TenantId);
        await Assert.That(parsed.RootElement.GetProperty("data").GetProperty("eventId").GetString()).IsEqualTo(AggregateId.ToString());
    }

    [Test]
    public async Task BuildAsync_ForHeavyRedaction_DropsUnsafeFields()
    {
        var moderationRecordId = Guid.Parse("018f0000-0000-7000-8000-000000000010");
        var context = CreateContext(
            WebhookEventNames.EventHeavyRedacted,
            new Dictionary<string, object?>
            {
                ["moderationRecordId"] = moderationRecordId.ToString(),
                ["status"] = "HeavyRedacted",
                ["reportId"] = "018f0000-0000-7000-8000-000000000020",
                ["caseId"] = "018f0000-0000-7000-8000-000000000030",
                ["eventId"] = AggregateId.ToString(),
                ["title"] = "Unsafe Event Title",
                ["slug"] = "unsafe-event-title",
                ["publicUrl"] = "https://example.org/events/unsafe-event-title",
                ["imageUri"] = "s3://unsafe-bucket/object-key",
                ["sourceActorId"] = "018f0000-0000-7000-8000-000000000050",
                ["rawProviderError"] = "provider leaked unsafe content",
                ["moderatorFreeText"] = "unsafe moderator text"
            });

        var result = await _builder.BuildAsync(context, CancellationToken.None);

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.RawPayloadJson!).Contains(moderationRecordId.ToString());
        await Assert.That(result.RawPayloadJson!).Contains("HeavyRedacted");
        await Assert.That(result.RawPayloadJson!).DoesNotContain(AggregateId.ToString());
        await Assert.That(result.RawPayloadJson!).DoesNotContain("Unsafe Event Title");
        await Assert.That(result.RawPayloadJson!).DoesNotContain("unsafe-event-title");
        await Assert.That(result.RawPayloadJson!).DoesNotContain("/events/");
        await Assert.That(result.RawPayloadJson!).DoesNotContain("s3://");
        await Assert.That(result.RawPayloadJson!).DoesNotContain("provider leaked unsafe content");
        await Assert.That(result.RawPayloadJson!).DoesNotContain("unsafe moderator text");
        await Assert.That(result.PayloadRetentionUntil).IsEqualTo(OccurredAt.AddDays(1));
    }

    [Test]
    public async Task BuildAsync_WhenRequiredFieldMissing_FailsClosed()
    {
        var context = CreateContext(
            WebhookEventNames.EventPublished,
            new Dictionary<string, object?>
            {
                ["eventId"] = AggregateId.ToString()
            });

        var result = await _builder.BuildAsync(context, CancellationToken.None);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCategory).IsEqualTo("missing_required_payload_field");
        await Assert.That(result.RawPayloadJson).IsNull();
    }

    [Test]
    public async Task BuildAsync_WhenEventTypeUnknown_FailsClosed()
    {
        var context = CreateContext(
            "unknown.event",
            new Dictionary<string, object?>
            {
                ["eventId"] = AggregateId.ToString()
            });

        var result = await _builder.BuildAsync(context, CancellationToken.None);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCategory).IsEqualTo("unknown_event_type");
        await Assert.That(result.Envelope).IsNull();
    }

    private static WebhookEventBuildContext CreateContext(
        string eventType,
        IReadOnlyDictionary<string, object?> data) =>
        new(
            MessageId,
            TenantId,
            eventType,
            MessageId.ToString(),
            "Event",
            AggregateId,
            OccurredAt,
            data);
}
