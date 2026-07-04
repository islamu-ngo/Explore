// ABOUTME: Unit tests for synchronizing canonical webhook event type descriptors into persistence.
// ABOUTME: Verifies startup catalog sync creates stable subscription rows and updates drifted metadata.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Webhooks;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Webhooks;

public sealed class WebhookEventTypeCatalogSyncServiceTests
{
    [Test]
    public async Task SyncAsync_WhenEventTypeMissing_CreatesPersistedCatalogRow()
    {
        var descriptor = CreateDescriptor("event.published");
        var repository = Substitute.For<IWebhookEventTypeRepository>();
        WebhookEventType? captured = null;
        repository.GetByNamesAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        repository.CreateAsync(Arg.Do<WebhookEventType>(eventType => captured = eventType), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<WebhookEventType>());
        var service = CreateService(descriptor, repository);

        var result = await service.SyncAsync(CancellationToken.None);

        await Assert.That(result.CreatedCount).IsEqualTo(1);
        await Assert.That(result.UpdatedCount).IsEqualTo(0);
        await Assert.That(result.UnchangedCount).IsEqualTo(0);
        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(captured.Name).IsEqualTo(descriptor.Name);
        await Assert.That(captured.SchemaJson).Contains("\"eventId\"");
        await repository.Received(1).CreateAsync(
            Arg.Any<WebhookEventType>(),
            Arg.Any<CancellationToken>());
        await repository.DidNotReceive().UpdateAsync(
            Arg.Any<WebhookEventType>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SyncAsync_WhenEventTypeMetadataDrifts_UpdatesPersistedCatalogRow()
    {
        var descriptor = CreateDescriptor("registration.created");
        var existing = new WebhookEventType
        {
            Id = Guid.CreateVersion7(),
            Name = descriptor.Name,
            GroupName = "legacy",
            Description = "Legacy description",
            SchemaJson = """{"type":"object","properties":{}}""",
            SchemaVersion = 0,
            IsPublic = false,
            IsEnabled = false,
            PayloadRetentionDays = 1
        };
        var repository = Substitute.For<IWebhookEventTypeRepository>();
        repository.GetByNamesAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns([existing]);
        var service = CreateService(descriptor, repository);

        var result = await service.SyncAsync(CancellationToken.None);

        await Assert.That(result.CreatedCount).IsEqualTo(0);
        await Assert.That(result.UpdatedCount).IsEqualTo(1);
        await Assert.That(result.UnchangedCount).IsEqualTo(0);
        await Assert.That(existing.GroupName).IsEqualTo(descriptor.GroupName);
        await Assert.That(existing.Description).IsEqualTo(descriptor.Description);
        await Assert.That(existing.SchemaVersion).IsEqualTo(descriptor.SchemaVersion);
        await Assert.That(existing.IsPublic).IsEqualTo(descriptor.IsPublic);
        await Assert.That(existing.IsEnabled).IsEqualTo(descriptor.IsEnabled);
        await Assert.That(existing.PayloadRetentionDays).IsEqualTo(descriptor.PayloadRetentionDays);
        await repository.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
        await repository.DidNotReceive().CreateAsync(
            Arg.Any<WebhookEventType>(),
            Arg.Any<CancellationToken>());
    }

    private static WebhookEventTypeCatalogSyncService CreateService(
        WebhookEventTypeDescriptor descriptor,
        IWebhookEventTypeRepository repository)
    {
        return new WebhookEventTypeCatalogSyncService(
            new TestWebhookEventTypeRegistry(descriptor),
            new WebhookEventSchemaProvider(),
            repository);
    }

    private static WebhookEventTypeDescriptor CreateDescriptor(string name) =>
        new(
            name,
            name.Split('.')[0],
            $"Raised for {name}.",
            1,
            true,
            true,
            14,
            [
                new WebhookEventDataFieldDescriptor(
                    "eventId",
                    WebhookJsonSchemaTypes.Text,
                    "Event identifier.",
                    "018f0000-0000-7000-8000-000000000001")
            ]);

    private sealed class TestWebhookEventTypeRegistry(WebhookEventTypeDescriptor descriptor) : IWebhookEventTypeRegistry
    {
        public IReadOnlyCollection<WebhookEventTypeDescriptor> GetAll() => [descriptor];

        public WebhookEventTypeDescriptor? FindByName(string name) =>
            string.Equals(name, descriptor.Name, StringComparison.Ordinal)
                ? descriptor
                : null;

        public bool IsKnownEventType(string name) =>
            string.Equals(name, descriptor.Name, StringComparison.Ordinal);
    }
}
