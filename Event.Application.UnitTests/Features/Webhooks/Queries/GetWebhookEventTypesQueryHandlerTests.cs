// ABOUTME: Unit tests for the webhook event catalog query handler.
// ABOUTME: Verifies provider-neutral catalog mapping includes schema, examples, retention, and fields.

using System.Text.Json;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Features.Webhooks.Handlers.Queries;
using Explore.Application.Features.Webhooks.Requests.Queries;
using Explore.Application.Webhooks;

namespace Event.Application.UnitTests.Features.Webhooks.Queries;

public sealed class GetWebhookEventTypesQueryHandlerTests
{
    [Test]
    public async Task Handle_ReturnsCanonicalCatalogWithSchemaExamplesAndFields()
    {
        var handler = new GetWebhookEventTypesQueryHandler(
            new WebhookEventTypeRegistry(),
            new WebhookEventSchemaProvider());

        var result = await handler.Handle(new GetWebhookEventTypesQuery(), CancellationToken.None);

        await Assert.That(result).Count().IsEqualTo(13);
        await Assert.That(result.Select(eventType => eventType.Name)).Contains(WebhookEventNames.EventPublished);
        await Assert.That(result.Select(eventType => eventType.Name)).Contains(WebhookEventNames.EventHeavyRedacted);
        await Assert.That(result.Select(eventType => eventType.Name)).Contains(WebhookEventNames.WebhookTest);

        var published = result.Single(eventType => eventType.Name == WebhookEventNames.EventPublished);
        await Assert.That(published.GroupName).IsEqualTo("event");
        await Assert.That(published.SchemaVersion).IsEqualTo(1);
        await Assert.That(published.IsPublic).IsTrue();
        await Assert.That(published.IsEnabled).IsTrue();
        await Assert.That(published.PayloadRetentionDays).IsEqualTo(14);
        await Assert.That(published.DataFields.Select(field => field.Name)).Contains("eventId");
        await Assert.That(published.DataFields.Single(field => field.Name == "publicUrl").Required).IsFalse();

        using var schema = JsonDocument.Parse(published.SchemaJson);
        using var example = JsonDocument.Parse(published.ExamplePayloadJson);
        await Assert.That(schema.RootElement.GetProperty("type").GetString()).IsEqualTo("object");
        await Assert.That(example.RootElement.GetProperty("type").GetString()).IsEqualTo(WebhookEventNames.EventPublished);
    }
}
