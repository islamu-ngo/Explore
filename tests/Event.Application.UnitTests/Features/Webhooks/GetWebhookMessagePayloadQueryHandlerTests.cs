// ABOUTME: Tests separately authorized, audited reads of retained outgoing webhook payload bytes.
// ABOUTME: Covers exact-byte success, tenant-safe absence, retention expiry, and audit fail-closed behavior.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Features.Webhooks.Handlers.Queries;
using Explore.Application.Features.Webhooks.Requests.Queries;
using Explore.Application.Responses;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Webhooks;

public sealed class GetWebhookMessagePayloadQueryHandlerTests
{
    private static readonly DateTime RetrievedAt =
        new(2026, 7, 14, 18, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task Handle_WhenPayloadIsRetained_AuditsBeforeReturningExactBytes()
    {
        byte[] payloadBytes = [0x7B, 0x22, 0x76, 0x22, 0x3A, 0xC3, 0xA9, 0x7D, 0x0A];
        var message = CreateMessage(payloadBytes, RetrievedAt.AddHours(1));
        var repository = Substitute.For<IWebhookMessageRepository>();
        repository.GetByIdForOwnerOperationAsync(message.Id, Arg.Any<CancellationToken>())
            .Returns(message);
        var auditWriter = Substitute.For<IWebhookAuditEventWriter>();
        var auditCompleted = false;
        auditWriter.AppendAsync(Arg.Any<WebhookAuditWriteRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                auditCompleted = true;
                return (WebhookAuditEvent)null!;
            });
        var handler = CreateHandler(repository, auditWriter);

        var result = await handler.Handle(new GetWebhookMessagePayloadQuery
        {
            MessageId = message.Id
        }, CancellationToken.None);

        await Assert.That(auditCompleted).IsTrue();
        await Assert.That(result.Status).IsEqualTo(WebhookMessagePayloadReadStatus.Available);
        await Assert.That(result.Payload).IsNotNull();
        await Assert.That(Convert.FromBase64String(result.Payload!.PayloadBase64)).IsEquivalentTo(payloadBytes);
        await Assert.That(result.Payload.PayloadHash).IsEqualTo(message.PayloadHash);
        await Assert.That(result.Payload.PayloadByteLength).IsEqualTo(payloadBytes.LongLength);
        await Assert.That(result.Payload.RetrievedAt).IsEqualTo(RetrievedAt);
        await auditWriter.Received(1).AppendAsync(
            Arg.Is<WebhookAuditWriteRequest>(audit =>
                audit.TenantId == message.TenantId &&
                audit.Action == WebhookAuditAction.PayloadViewed &&
                audit.TargetKind == WebhookAuditTargetKind.Payload &&
                audit.TargetId == message.Id &&
                audit.ReasonCode == "payload.viewed" &&
                audit.Outcome == WebhookAuditOutcome.Succeeded &&
                audit.SafeAfterJson != null &&
                !audit.SafeAfterJson.Contains(Convert.ToBase64String(payloadBytes), StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenPersistedOwnerLookupDoesNotFindMessage_ReturnsNotFoundWithoutAudit()
    {
        var messageId = Guid.CreateVersion7();
        var repository = Substitute.For<IWebhookMessageRepository>();
        var auditWriter = Substitute.For<IWebhookAuditEventWriter>();
        var handler = CreateHandler(repository, auditWriter);

        var result = await handler.Handle(new GetWebhookMessagePayloadQuery
        {
            MessageId = messageId
        }, CancellationToken.None);

        await Assert.That(result.Status).IsEqualTo(WebhookMessagePayloadReadStatus.NotFound);
        await Assert.That(result.Payload).IsNull();
        await repository.Received(1).GetByIdForOwnerOperationAsync(
            messageId,
            Arg.Any<CancellationToken>());
        await auditWriter.DidNotReceive().AppendAsync(
            Arg.Any<WebhookAuditWriteRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenRetentionEnded_ReturnsGoneWithoutReturningStoredBytes()
    {
        byte[] payloadBytes = [0x7B, 0x7D];
        var message = CreateMessage(payloadBytes, RetrievedAt.AddTicks(-1));
        var repository = Substitute.For<IWebhookMessageRepository>();
        repository.GetByIdForOwnerOperationAsync(message.Id, Arg.Any<CancellationToken>())
            .Returns(message);
        var auditWriter = Substitute.For<IWebhookAuditEventWriter>();
        var handler = CreateHandler(repository, auditWriter);

        var result = await handler.Handle(new GetWebhookMessagePayloadQuery
        {
            MessageId = message.Id
        }, CancellationToken.None);

        await Assert.That(result.Status).IsEqualTo(WebhookMessagePayloadReadStatus.Gone);
        await Assert.That(result.Payload).IsNull();
        await auditWriter.Received(1).AppendAsync(
            Arg.Is<WebhookAuditWriteRequest>(audit =>
                audit.ReasonCode == "payload.retention-expired" &&
                audit.Outcome == WebhookAuditOutcome.Rejected &&
                audit.SafeAfterJson != null &&
                !audit.SafeAfterJson.Contains(Convert.ToBase64String(payloadBytes), StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenAuditWriteFails_DoesNotProducePayloadResult()
    {
        var message = CreateMessage([0x7B, 0x7D], RetrievedAt.AddHours(1));
        var repository = Substitute.For<IWebhookMessageRepository>();
        repository.GetByIdForOwnerOperationAsync(message.Id, Arg.Any<CancellationToken>())
            .Returns(message);
        var auditWriter = Substitute.For<IWebhookAuditEventWriter>();
        auditWriter.AppendAsync(Arg.Any<WebhookAuditWriteRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<WebhookAuditEvent>(new InvalidOperationException("audit unavailable")));
        var handler = CreateHandler(repository, auditWriter);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new GetWebhookMessagePayloadQuery
            {
                MessageId = message.Id
            },
            CancellationToken.None));
    }

    [Test]
    public async Task Handle_WhenIdentifiersAreInvalid_DoesNotReadOrAudit()
    {
        var repository = Substitute.For<IWebhookMessageRepository>();
        var auditWriter = Substitute.For<IWebhookAuditEventWriter>();
        var handler = CreateHandler(repository, auditWriter);

        var result = await handler.Handle(new GetWebhookMessagePayloadQuery(), CancellationToken.None);

        await Assert.That(result.Status).IsEqualTo(WebhookMessagePayloadReadStatus.NotFound);
        await repository.DidNotReceive().GetByIdForOwnerOperationAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
        await auditWriter.DidNotReceive().AppendAsync(
            Arg.Any<WebhookAuditWriteRequest>(),
            Arg.Any<CancellationToken>());
    }

    private static GetWebhookMessagePayloadQueryHandler CreateHandler(
        IWebhookMessageRepository repository,
        IWebhookAuditEventWriter auditWriter) =>
        new(repository, auditWriter, new FixedTimeProvider(RetrievedAt));

    private static WebhookMessage CreateMessage(byte[] payloadBytes, DateTime retentionUntil) =>
        WebhookMessage.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "event.published",
            Guid.CreateVersion7().ToString("D"),
            "Event",
            Guid.CreateVersion7(),
            null,
            payloadBytes,
            "application/json",
            "utf-8",
            RetrievedAt.AddDays(-2),
            retentionUntil,
            RetrievedAt.AddDays(-2));

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
