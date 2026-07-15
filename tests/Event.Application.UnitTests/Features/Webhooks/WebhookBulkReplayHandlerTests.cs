// ABOUTME: Tests bounded webhook bulk replay preview, scheduling, cancellation, and mandatory audit.
// ABOUTME: Covers policy ceilings, stable operation persistence, optimistic state changes, and rejection evidence.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Features.Webhooks.Handlers.Commands;
using Explore.Application.Features.Webhooks.Handlers.Queries;
using Explore.Application.Features.Webhooks.Requests.Commands;
using Explore.Application.Features.Webhooks.Requests.Queries;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Webhooks;

public sealed class WebhookBulkReplayHandlerTests
{
    private static readonly DateTime UtcNow =
        new(2026, 7, 14, 15, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task Preview_MapsDisjointCountsAndConfiguredLimits()
    {
        var repository = Substitute.For<IWebhookBulkReplayRepository>();
        repository.PreviewAsync(
                Arg.Any<Guid>(),
                Arg.Any<WebhookBulkReplayFilter>(),
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(new WebhookBulkReplayPreviewSnapshot(12, 1, 2, 3, 4, 5, 6, 7, 8));
        var handler = new PreviewWebhookBulkReplayQueryHandler(
            repository,
            new FixedPolicyResolver(),
            new FixedTimeProvider(UtcNow));

        var result = await handler.Handle(new PreviewWebhookBulkReplayQuery
        {
            TenantId = Guid.CreateVersion7(),
            FromUtc = UtcNow.AddDays(-7),
            ToUtc = UtcNow,
            EventType = " event.published ",
            MaxItems = 10
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Preview!.EligibleCount).IsEqualTo(12);
        await Assert.That(result.Preview.EstimatedSelectedCount).IsEqualTo(10);
        await Assert.That(result.Preview.ExcludedCount).IsEqualTo(36);
        await Assert.That(result.Preview.Filter.EventType).IsEqualTo("event.published");
        await Assert.That(result.Preview.MaximumItemsPerOperation).IsEqualTo(100);
    }

    [Test]
    public async Task Schedule_QueuesStableOperationAndWritesSafeAudit()
    {
        var repository = Substitute.For<IWebhookBulkReplayRepository>();
        repository.PreviewAsync(
                Arg.Any<Guid>(),
                Arg.Any<WebhookBulkReplayFilter>(),
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(new WebhookBulkReplayPreviewSnapshot(7, 0, 0, 0, 0, 0, 0, 0, 0));
        repository.CountReservedItemsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(0);
        repository.CreateAsync(Arg.Any<WebhookBulkReplayOperation>(), Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<WebhookBulkReplayOperation>(0));
        var auditWriter = Substitute.For<IWebhookAuditEventWriter>();
        var handler = new ScheduleWebhookBulkReplayCommandHandler(
            repository,
            new FixedPolicyResolver(),
            auditWriter,
            new InlineUnitOfWork(),
            new FixedTimeProvider(UtcNow));
        var command = CreateScheduleCommand();

        var result = await handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await repository.Received(1).AcquireTenantScheduleLockAsync(
            command.TenantId,
            Arg.Any<CancellationToken>());
        await repository.Received(1).CreateAsync(
            Arg.Is<WebhookBulkReplayOperation>(operation =>
                operation.TenantId == command.TenantId &&
                operation.OperationKey == command.OperationKey &&
                operation.RequestHash.StartsWith("sha256:", StringComparison.Ordinal) &&
                operation.RequestedMaxItems == command.MaxItems &&
                operation.CreatedBy == command.ActorUserId),
            Arg.Any<CancellationToken>());
        await auditWriter.Received(1).AppendAsync(
            Arg.Is<WebhookAuditWriteRequest>(audit =>
                audit.Action == WebhookAuditAction.BulkReplayScheduled &&
                audit.Outcome == WebhookAuditOutcome.Succeeded &&
                audit.PrincipalReference == $"user:{command.ActorUserId:D}" &&
                audit.SafeAfterJson != null &&
                !audit.SafeAfterJson.Contains("payload", StringComparison.OrdinalIgnoreCase)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Cancel_WhenVersionIsStale_RejectsAndAuditsWithoutMutation()
    {
        var command = CreateScheduleCommand();
        var operation = WebhookBulkReplayOperation.Create(
            command.TenantId,
            command.OperationKey,
            $"sha256:{new string('b', 64)}",
            command.FromUtc,
            command.ToUtc,
            command.WebhookConsumerId,
            command.WebhookEndpointId,
            command.EventType,
            command.MaxItems,
            command.ReasonCode,
            new WebhookBulkReplayPreviewSnapshot(1, 0, 0, 0, 0, 0, 0, 0, 0),
            UtcNow);
        var repository = Substitute.For<IWebhookBulkReplayRepository>();
        repository.GetByTenantAndIdAsync(
                command.TenantId,
                operation.Id,
                Arg.Any<CancellationToken>())
            .Returns(operation);
        var auditWriter = Substitute.For<IWebhookAuditEventWriter>();
        var handler = new CancelWebhookBulkReplayCommandHandler(
            repository,
            new FixedPolicyResolver(),
            auditWriter,
            new InlineUnitOfWork(),
            new FixedTimeProvider(UtcNow));

        var result = await handler.Handle(new CancelWebhookBulkReplayCommand
        {
            TenantId = command.TenantId,
            ActorUserId = command.ActorUserId,
            OperationId = operation.Id,
            ExpectedConcurrencyVersion = 99,
            ReasonCode = "operator.cancel"
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("webhook_bulk_replay_concurrency_conflict");
        await Assert.That(operation.Status).IsEqualTo(WebhookBulkReplayStatus.Queued);
        await repository.DidNotReceive().UpdateAsync(
            Arg.Any<WebhookBulkReplayOperation>(),
            Arg.Any<CancellationToken>());
        await auditWriter.Received(1).AppendAsync(
            Arg.Is<WebhookAuditWriteRequest>(audit =>
                audit.Action == WebhookAuditAction.BulkReplayCancelled &&
                audit.Outcome == WebhookAuditOutcome.Rejected),
            Arg.Any<CancellationToken>());
    }

    private static ScheduleWebhookBulkReplayCommand CreateScheduleCommand() =>
        new()
        {
            TenantId = Guid.CreateVersion7(),
            ActorUserId = Guid.CreateVersion7(),
            OperationKey = Guid.CreateVersion7(),
            FromUtc = UtcNow.AddDays(-7),
            ToUtc = UtcNow,
            WebhookConsumerId = Guid.CreateVersion7(),
            EventType = "event.published",
            MaxItems = 25,
            ReasonCode = "operator.incident-recovery"
        };

    private sealed class FixedPolicyResolver : IWebhookBulkReplayPolicyResolver
    {
        public WebhookBulkReplayLimits Resolve() => new(100, 500, 30, 10, "webhook-bulk-replay-test-v1");
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    private sealed class InlineUnitOfWork : IUnitOfWork
    {
        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken ct = default) => operation(ct);

        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default) => operation(ct);
    }
}
