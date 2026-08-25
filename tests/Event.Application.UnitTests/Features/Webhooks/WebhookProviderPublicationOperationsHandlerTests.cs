// ABOUTME: Tests provider publication list/detail, manual reconciliation, and abandonment handlers.
// ABOUTME: Covers normalized mapping, optimistic conflicts, legal states, append-only evidence, and audit.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Features.Webhooks.Handlers.Commands;
using Explore.Application.Features.Webhooks.Handlers.Queries;
using Explore.Application.Features.Webhooks.Requests.Commands;
using Explore.Application.Features.Webhooks.Requests.Queries;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Webhooks;

public sealed class WebhookProviderPublicationOperationsHandlerTests
{
    private static readonly DateTime PreparedAt =
        new(2026, 7, 14, 8, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task Reconcile_WhenManualEvidenceMatchesVersion_SettlesAndAudits()
    {
        var publication = CreateManualReconciliationPublication();
        var expectedVersion = publication.ConcurrencyVersion;
        var repository = Substitute.For<IWebhookProviderPublicationRepository>();
        repository.GetByTenantAndIdAsync(
                publication.TenantId,
                publication.Id,
                Arg.Any<CancellationToken>())
            .Returns(publication);
        repository.UpdateAsync(publication, Arg.Any<CancellationToken>()).Returns(publication);
        var auditWriter = Substitute.For<IWebhookAuditEventWriter>();
        var handler = new ReconcileWebhookProviderPublicationCommandHandler(
            repository,
            auditWriter,
            new InlineUnitOfWork(),
            new FixedTimeProvider(PreparedAt.AddMinutes(8)));
        var actorUserId = Guid.CreateVersion7();

        var result = await handler.Handle(
            new ReconcileWebhookProviderPublicationCommand
            {
                TenantId = publication.TenantId,
                PublicationId = publication.Id,
                ActorUserId = actorUserId,
                ExpectedConcurrencyVersion = expectedVersion,
                ExternalProviderMessageId = "provider-message-123",
                ReasonCode = "operator.provider-evidence"
            },
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(publication.Status).IsEqualTo(WebhookProviderPublicationStatus.ProviderQueued);
        await Assert.That(publication.ExternalProviderMessageId).IsEqualTo("provider-message-123");
        await Assert.That(publication.Attempts.Last().Outcome)
            .IsEqualTo(WebhookProviderPublicationAttemptOutcome.ReconciledProviderQueued);
        await auditWriter.Received(1).AppendAsync(
            Arg.Is<WebhookAuditWriteRequest>(audit =>
                audit.Action == WebhookAuditAction.ProviderPublicationReconciled &&
                audit.TargetId == publication.Id &&
                audit.ReasonCode == "operator.provider-evidence" &&
                audit.PrincipalReference == $"user:{actorUserId:D}"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Reconcile_WhenExpectedVersionIsStale_RejectsWithoutMutation()
    {
        var publication = CreateManualReconciliationPublication();
        var repository = Substitute.For<IWebhookProviderPublicationRepository>();
        repository.GetByTenantAndIdAsync(
                publication.TenantId,
                publication.Id,
                Arg.Any<CancellationToken>())
            .Returns(publication);
        var auditWriter = Substitute.For<IWebhookAuditEventWriter>();
        var handler = new ReconcileWebhookProviderPublicationCommandHandler(
            repository,
            auditWriter,
            new InlineUnitOfWork(),
            new FixedTimeProvider(PreparedAt.AddMinutes(8)));

        var result = await handler.Handle(
            new ReconcileWebhookProviderPublicationCommand
            {
                TenantId = publication.TenantId,
                PublicationId = publication.Id,
                ActorUserId = Guid.CreateVersion7(),
                ExpectedConcurrencyVersion = publication.ConcurrencyVersion - 1,
                ExternalProviderMessageId = "provider-message-123",
                ReasonCode = "operator.provider-evidence"
            },
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode)
            .IsEqualTo("webhook_provider_publication_concurrency_conflict");
        await Assert.That(publication.Status)
            .IsEqualTo(WebhookProviderPublicationStatus.ManualReconciliation);
        await repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
        await auditWriter.DidNotReceiveWithAnyArgs().AppendAsync(default!, default);
    }

    [Test]
    public async Task Abandon_WhenManualReconciliation_SettlesAndAudits()
    {
        var publication = CreateManualReconciliationPublication();
        var repository = Substitute.For<IWebhookProviderPublicationRepository>();
        repository.GetByTenantAndIdAsync(
                publication.TenantId,
                publication.Id,
                Arg.Any<CancellationToken>())
            .Returns(publication);
        repository.UpdateAsync(publication, Arg.Any<CancellationToken>()).Returns(publication);
        var auditWriter = Substitute.For<IWebhookAuditEventWriter>();
        var handler = new AbandonWebhookProviderPublicationCommandHandler(
            repository,
            auditWriter,
            new InlineUnitOfWork(),
            new FixedTimeProvider(PreparedAt.AddMinutes(8)));

        var result = await handler.Handle(
            new AbandonWebhookProviderPublicationCommand
            {
                TenantId = publication.TenantId,
                PublicationId = publication.Id,
                ActorUserId = Guid.CreateVersion7(),
                ExpectedConcurrencyVersion = publication.ConcurrencyVersion,
                ReasonCode = "operator.no-provider-record"
            },
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(publication.Status).IsEqualTo(WebhookProviderPublicationStatus.Abandoned);
        await Assert.That(publication.FailureCategory).IsEqualTo("operator_abandoned");
        await Assert.That(publication.Attempts.Last().Outcome)
            .IsEqualTo(WebhookProviderPublicationAttemptOutcome.Abandoned);
        await auditWriter.Received(1).AppendAsync(
            Arg.Is<WebhookAuditWriteRequest>(audit =>
                audit.Action == WebhookAuditAction.ProviderPublicationAbandoned &&
                audit.TargetId == publication.Id),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Abandon_WhenPublicationIsPrepared_RejectsActiveWorkerOwnedState()
    {
        var publication = CreatePublication();
        var repository = Substitute.For<IWebhookProviderPublicationRepository>();
        repository.GetByTenantAndIdAsync(
                publication.TenantId,
                publication.Id,
                Arg.Any<CancellationToken>())
            .Returns(publication);
        var handler = new AbandonWebhookProviderPublicationCommandHandler(
            repository,
            Substitute.For<IWebhookAuditEventWriter>(),
            new InlineUnitOfWork(),
            new FixedTimeProvider(PreparedAt.AddMinutes(1)));

        var result = await handler.Handle(
            new AbandonWebhookProviderPublicationCommand
            {
                TenantId = publication.TenantId,
                PublicationId = publication.Id,
                ActorUserId = Guid.CreateVersion7(),
                ExpectedConcurrencyVersion = publication.ConcurrencyVersion,
                ReasonCode = "operator.abort"
            },
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode)
            .IsEqualTo("webhook_provider_publication_not_abandonable");
        await repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Test]
    public async Task ListQuery_MapsNormalizedPublicationAndAttemptCodes()
    {
        var publication = CreateManualReconciliationPublication();
        var repository = Substitute.For<IWebhookProviderPublicationRepository>();
        repository.ListByTenantAsync(
                publication.TenantId,
                null,
                null,
                (int)WebhookProviderPublicationStatus.ManualReconciliation,
                25,
                Arg.Any<CancellationToken>())
            .Returns([publication]);
        var handler = new GetWebhookProviderPublicationsQueryHandler(repository);

        var result = await handler.Handle(
            new GetWebhookProviderPublicationsQuery
            {
                TenantId = publication.TenantId,
                StatusId = (int)WebhookProviderPublicationStatus.ManualReconciliation,
                Limit = 25
            },
            CancellationToken.None);

        await Assert.That(result).HasSingleItem();
        await Assert.That(result[0].StatusCode).IsEqualTo("MANUAL_RECONCILIATION");
        await Assert.That(result[0].ProviderKindCode).IsEqualTo("SVIX");
        await Assert.That(result[0].Attempts.Last().OutcomeCode)
            .IsEqualTo("MANUAL_RECONCILIATION_REQUIRED");
        await Assert.That(result[0].GetType().GetProperty("CredentialReference")).IsNull();
    }

    private static WebhookProviderPublication CreateManualReconciliationPublication()
    {
        var publication = CreatePublication();
        var leaseToken = Guid.CreateVersion7();
        publication.ClaimForPublishing(
            "publisher",
            leaseToken,
            PreparedAt.AddMinutes(4),
            PreparedAt.AddMinutes(1),
            3);
        publication.MarkPublicationUnknown(
            leaseToken,
            publication.PublicationFence,
            "acceptance_timeout",
            null,
            PreparedAt.AddMinutes(4),
            PreparedAt.AddMinutes(2));
        publication.RequireManualReconciliation(
            "operator_review_required",
            null,
            PreparedAt.AddMinutes(5));
        return publication;
    }

    private static WebhookProviderPublication CreatePublication() =>
        WebhookProviderPublication.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            WebhookProviderKind.Svix,
            Guid.CreateVersion7(),
            "svix-2026.07",
            "event-123",
            "idempotency-123",
            $"sha256:{new string('a', 64)}",
            "consumer-application-uid",
            "provider-application-id",
            "self-hosted",
            "secret:webhook-provider",
            "credential-v3",
            WebhookProviderMode.Svix,
            "provider-config-v5",
            4,
            "retention-v2",
            PreparedAt.AddDays(7),
            PreparedAt.AddDays(30),
            PreparedAt.AddHours(12),
            PreparedAt);

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

        public Task<T> ExecuteSerializableAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default) => ExecuteInTransactionAsync(operation, ct);
    }
}
