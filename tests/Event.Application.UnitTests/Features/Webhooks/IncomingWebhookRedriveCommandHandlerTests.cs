// ABOUTME: Tests authorized incoming webhook redrive validation, concurrency, audit, and tenant boundaries.
// ABOUTME: Verifies one new generation is scheduled while stale, unauthenticated, and wrong-tenant requests fail closed.

using System.Reflection;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Features.Webhooks.Handlers.Commands;
using Explore.Application.Features.Webhooks.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Constants;
using NSubstitute;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Webhooks;

public sealed class IncomingWebhookRedriveCommandHandlerTests
{
    [Test]
    public async Task AuthorizedDeadLetterRedrive_IncrementsGenerationAndWritesSafeAudit()
    {
        var now = new DateTime(2026, 7, 13, 21, 30, 0, DateTimeKind.Utc);
        var message = CreateDeadLetteredMessage(now);
        var actorUserId = Guid.CreateVersion7();
        var repository = Substitute.For<IIncomingWebhookMessageRepository>();
        repository.GetByTenantAndIdForUpdateAsync(
                message.TenantId,
                message.Id,
                Arg.Any<CancellationToken>())
            .Returns(message);
        repository.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var auditWriter = Substitute.For<IWebhookAuditEventWriter>();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(actorUserId);
        currentUser.IsAuthenticated.Returns(true);
        var machinePrincipal = Substitute.For<IMachinePrincipalAccessor>();
        var handler = CreateHandler(
            repository,
            auditWriter,
            currentUser,
            machinePrincipal,
            now.AddMinutes(1));
        var reason = "operator-confirmed-transient-provider-recovery";

        var response = await handler.Handle(new RedriveIncomingWebhookCommand
        {
            TenantId = message.TenantId,
            IncomingWebhookMessageId = message.Id,
            ExpectedProcessingGeneration = 1,
            Reason = reason
        }, CancellationToken.None);

        await Assert.That(response.Success).IsTrue();
        await Assert.That(message.Status).IsEqualTo(IncomingWebhookMessageStatus.RetryDue);
        await Assert.That(message.ProcessingGeneration).IsEqualTo(2);
        await Assert.That(message.RedriveRecords).HasSingleItem();
        var record = message.RedriveRecords.Single();
        await Assert.That(record.ActorId).IsEqualTo($"user:{actorUserId:D}");
        await Assert.That(record.Reason).IsEqualTo(reason);
        await Assert.That(record.SourceProcessingGeneration).IsEqualTo(1);
        await Assert.That(record.TargetProcessingGeneration).IsEqualTo(2);
        repository.Received(1).TrackAppendedEvidence(message);
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await auditWriter.Received(1).AppendAsync(
            Arg.Is<WebhookAuditWriteRequest>(audit =>
                audit.TenantId == message.TenantId &&
                audit.TargetId == message.Id &&
                audit.Action == WebhookAuditAction.IncomingRedriveScheduled &&
                audit.SafeAfterJson != null &&
                !audit.SafeAfterJson.Contains(reason, StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StaleGeneration_DoesNotMutateOrWriteAudit()
    {
        var now = new DateTime(2026, 7, 13, 21, 30, 0, DateTimeKind.Utc);
        var message = CreateDeadLetteredMessage(now);
        var repository = Substitute.For<IIncomingWebhookMessageRepository>();
        repository.GetByTenantAndIdForUpdateAsync(
                message.TenantId,
                message.Id,
                Arg.Any<CancellationToken>())
            .Returns(message);
        var auditWriter = Substitute.For<IWebhookAuditEventWriter>();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.CreateVersion7());
        var handler = CreateHandler(
            repository,
            auditWriter,
            currentUser,
            Substitute.For<IMachinePrincipalAccessor>(),
            now.AddMinutes(1));

        var response = await handler.Handle(new RedriveIncomingWebhookCommand
        {
            TenantId = message.TenantId,
            IncomingWebhookMessageId = message.Id,
            ExpectedProcessingGeneration = 2,
            Reason = "stale-request"
        }, CancellationToken.None);

        await Assert.That(response.Success).IsFalse();
        await Assert.That(response.FailureCode).IsEqualTo("incoming_webhook_redrive_generation_conflict");
        await Assert.That(message.Status).IsEqualTo(IncomingWebhookMessageStatus.DeadLettered);
        await Assert.That(message.ProcessingGeneration).IsEqualTo(1);
        await repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await auditWriter.DidNotReceive().AppendAsync(
            Arg.Any<WebhookAuditWriteRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExpiredReplayWindow_DoesNotMutateOrWriteAudit()
    {
        var receivedAt = new DateTime(2026, 7, 13, 21, 30, 0, DateTimeKind.Utc);
        var message = CreateDeadLetteredMessage(receivedAt);
        var repository = Substitute.For<IIncomingWebhookMessageRepository>();
        repository.GetByTenantAndIdForUpdateAsync(
                message.TenantId,
                message.Id,
                Arg.Any<CancellationToken>())
            .Returns(message);
        var auditWriter = Substitute.For<IWebhookAuditEventWriter>();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.CreateVersion7());
        var handler = CreateHandler(
            repository,
            auditWriter,
            currentUser,
            Substitute.For<IMachinePrincipalAccessor>(),
            receivedAt.AddDays(14));

        var response = await handler.Handle(new RedriveIncomingWebhookCommand
        {
            TenantId = message.TenantId,
            IncomingWebhookMessageId = message.Id,
            ExpectedProcessingGeneration = 1,
            Reason = "expired-replay-window"
        }, CancellationToken.None);

        await Assert.That(response.Success).IsFalse();
        await Assert.That(response.FailureCode).IsEqualTo("incoming_webhook_redrive_payload_unavailable");
        await Assert.That(message.Status).IsEqualTo(IncomingWebhookMessageStatus.DeadLettered);
        await repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await auditWriter.DidNotReceive().AppendAsync(
            Arg.Any<WebhookAuditWriteRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task WrongTenantOrMissingActor_FailsClosedWithoutPersistence()
    {
        var now = new DateTime(2026, 7, 13, 21, 30, 0, DateTimeKind.Utc);
        var message = CreateDeadLetteredMessage(now);
        var repository = Substitute.For<IIncomingWebhookMessageRepository>();
        var auditWriter = Substitute.For<IWebhookAuditEventWriter>();
        var currentUser = Substitute.For<ICurrentUserService>();
        var handler = CreateHandler(
            repository,
            auditWriter,
            currentUser,
            Substitute.For<IMachinePrincipalAccessor>(),
            now.AddMinutes(1));
        var request = new RedriveIncomingWebhookCommand
        {
            TenantId = Guid.CreateVersion7(),
            IncomingWebhookMessageId = message.Id,
            ExpectedProcessingGeneration = 1,
            Reason = "cross-tenant-request"
        };

        var missingActorResponse = await handler.Handle(request, CancellationToken.None);

        await Assert.That(missingActorResponse.Success).IsFalse();
        await Assert.That(missingActorResponse.FailureCode).IsEqualTo("incoming_webhook_redrive_actor_required");
        await repository.DidNotReceive().GetByTenantAndIdForUpdateAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
        await auditWriter.DidNotReceive().AppendAsync(
            Arg.Any<WebhookAuditWriteRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CommandAuthorization_UsesDedicatedActionAndRejectsInternalProcessorScope()
    {
        var attribute = typeof(RedriveIncomingWebhookCommand)
            .GetCustomAttribute<AuthorizeResourceAttribute>();
        var tenantId = Guid.CreateVersion7();
        var messageId = Guid.CreateVersion7();
        ISecureRequest request = new RedriveIncomingWebhookCommand
        {
            TenantId = tenantId,
            IncomingWebhookMessageId = messageId,
            ExpectedProcessingGeneration = 3,
            Reason = "authorized-recovery"
        };

        await Assert.That(attribute).IsNotNull();
        await Assert.That(attribute!.Resource).IsEqualTo(ResourceKinds.Webhook);
        await Assert.That(attribute.Action).IsEqualTo(AuthorizationActions.Webhooks.RedriveIncoming);
        await Assert.That(request.ResourceId).IsEqualTo(messageId.ToString("D"));
        await Assert.That(request.ResourceAttributes!["tenantId"]).IsEqualTo(tenantId.ToString("D"));
        await Assert.That(MachineScopeMapping.ScopesPermit(
            [ExternalApiKeyScopes.AdminTenant],
            ResourceKinds.Webhook,
            AuthorizationActions.Webhooks.RedriveIncoming)).IsTrue();
        await Assert.That(MachineScopeMapping.ScopesPermit(
            [InternalMachineScopes.ProcessIncomingWebhook],
            ResourceKinds.Webhook,
            AuthorizationActions.Webhooks.RedriveIncoming)).IsFalse();
    }

    private static RedriveIncomingWebhookCommandHandler CreateHandler(
        IIncomingWebhookMessageRepository repository,
        IWebhookAuditEventWriter auditWriter,
        ICurrentUserService currentUserService,
        IMachinePrincipalAccessor machinePrincipalAccessor,
        DateTime utcNow) =>
        new(
            repository,
            auditWriter,
            new InlineUnitOfWork(),
            currentUserService,
            machinePrincipalAccessor,
            new FixedTimeProvider(utcNow));

    private static IncomingWebhookMessage CreateDeadLetteredMessage(DateTime now)
    {
        var payload = System.Text.Encoding.UTF8.GetBytes("{\"redrive\":true}");
        var payloadHash = "sha256:" + Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(payload)).ToLowerInvariant();
        var message = IncomingWebhookMessage.CreateVerified(
            Guid.CreateVersion7(),
            "test-provider",
            Guid.CreateVersion7().ToString("N"),
            null,
            "test.redrive",
            payload,
            payloadHash,
            "application/json",
            "utf-8",
            null,
            now,
            now,
            now.AddDays(14),
            "webhook-retention-test-v1",
            now.AddDays(30),
            now.AddDays(90),
            now.AddDays(14),
            now.AddDays(30));
        var leaseToken = Guid.CreateVersion7();
        message.Claim("redrive-test-worker", leaseToken, now.AddMinutes(5), now.AddSeconds(1));
        message.DeadLetter(
            leaseToken,
            message.ProcessingFence,
            message.ProcessingGeneration,
            "attempts_exhausted",
            null,
            now.AddSeconds(2));
        return message;
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    private sealed class InlineUnitOfWork : IUnitOfWork
    {
        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken ct = default) =>
            await operation(ct);

        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default) =>
            operation(ct);
    }
}
