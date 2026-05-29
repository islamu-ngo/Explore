// ABOUTME: PostgreSQL-backed tests for EmailDispatch operator replay and parking transitions.
// ABOUTME: Verifies durable state-machine changes that future RabbitMQ consumers and admin actions reuse.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class EmailDispatchOutboxTransitionRepositoryTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task TryParkForOperatorMarksEligibleRowAsParked()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "park");
        var dispatch = await SeedDispatchAsync(context, tenant.Id, EmailDispatchStatus.DeadLettered);
        var actorId = Guid.NewGuid();
        var parkedAt = DateTime.UtcNow;
        var repository = new EmailDispatchOutboxRepository(context);

        var parked = await repository.TryParkForOperator(
            tenant.Id,
            dispatch.Id,
            "operator quarantine",
            actorId,
            parkedAt,
            CancellationToken.None);

        await Assert.That(parked).IsTrue();

        var row = await context.EmailDispatchOutbox
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(outbox => outbox.Id == dispatch.Id);
        await Assert.That(row.Status).IsEqualTo(EmailDispatchStatus.Parked);
        await Assert.That(row.ParkedAt).IsNotNull();
        await Assert.That(Math.Abs((row.ParkedAt.Value - parkedAt).TotalMilliseconds)).IsLessThan(5);
        await Assert.That(row.LastFailureCategory).IsEqualTo("operator_parked");
        await Assert.That(row.LastError).IsEqualTo("operator quarantine");
        await Assert.That(row.UpdatedBy).IsEqualTo(actorId);
        await Assert.That(row.NextAttemptAt).IsNull();
        await Assert.That(row.ProcessingLeaseToken).IsNull();
    }

    [Test]
    public async Task TryReplayForOperatorResetsDeferredRowToPending()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "replay");
        var dispatch = await SeedDispatchAsync(context, tenant.Id, EmailDispatchStatus.DeadLettered);
        var actorId = Guid.NewGuid();
        var replayAt = DateTime.UtcNow;
        var repository = new EmailDispatchOutboxRepository(context);

        var replayed = await repository.TryReplayForOperator(
            tenant.Id,
            dispatch.Id,
            actorId,
            replayAt,
            CancellationToken.None);

        await Assert.That(replayed).IsTrue();

        var row = await context.EmailDispatchOutbox
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(outbox => outbox.Id == dispatch.Id);
        await Assert.That(row.Status).IsEqualTo(EmailDispatchStatus.Pending);
        await Assert.That(row.NextAttemptAt).IsNull();
        await Assert.That(row.DeadLetteredAt).IsNull();
        await Assert.That(row.ParkedAt).IsNull();
        await Assert.That(row.UnknownAt).IsNull();
        await Assert.That(row.LastFailureCategory).IsNull();
        await Assert.That(row.LastError).IsNull();
        await Assert.That(row.UpdatedBy).IsEqualTo(actorId);
    }

    [Test]
    public async Task TryReplayForOperatorDoesNotReplaySentRow()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "sent");
        var dispatch = await SeedDispatchAsync(context, tenant.Id, EmailDispatchStatus.Sent);
        var repository = new EmailDispatchOutboxRepository(context);

        var replayed = await repository.TryReplayForOperator(
            tenant.Id,
            dispatch.Id,
            Guid.NewGuid(),
            DateTime.UtcNow,
            CancellationToken.None);

        await Assert.That(replayed).IsFalse();

        var row = await context.EmailDispatchOutbox
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(outbox => outbox.Id == dispatch.Id);
        await Assert.That(row.Status).IsEqualTo(EmailDispatchStatus.Sent);
    }

    [Test]
    public async Task GetByTenantAndPublishEventIdReturnsOnlyMatchingTenantRow()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "lookup");
        var otherTenant = await SeedTenantAsync(context, "lookup-other");
        var dispatch = await SeedDispatchAsync(context, tenant.Id, EmailDispatchStatus.Pending);
        await SeedDispatchAsync(context, otherTenant.Id, EmailDispatchStatus.Pending);
        var repository = new EmailDispatchOutboxRepository(context);

        var found = await repository.GetByTenantAndPublishEventId(
            tenant.Id,
            dispatch.PublishEventId,
            CancellationToken.None);
        var wrongTenant = await repository.GetByTenantAndPublishEventId(
            otherTenant.Id,
            dispatch.PublishEventId,
            CancellationToken.None);

        await Assert.That(found).IsNotNull();
        await Assert.That(found!.Id).IsEqualTo(dispatch.Id);
        await Assert.That(found.TenantId).IsEqualTo(tenant.Id);
        await Assert.That(wrongTenant).IsNull();
    }

    [Test]
    public async Task TryClaimReceiptRejectsDuplicatePublishEventForTenant()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "receipt");
        var dispatch = await SeedDispatchAsync(context, tenant.Id, EmailDispatchStatus.Pending);
        var repository = new EmailDispatchOutboxRepository(context);
        var firstReceipt = CreateReceipt(tenant.Id, dispatch.PublishEventId, dispatch.Id, "consumer-a");
        var duplicateReceipt = CreateReceipt(tenant.Id, dispatch.PublishEventId, dispatch.Id, "consumer-b");

        var firstClaimed = await repository.TryClaimReceipt(firstReceipt, CancellationToken.None);
        var duplicateClaimed = await repository.TryClaimReceipt(duplicateReceipt, CancellationToken.None);

        await Assert.That(firstClaimed).IsTrue();
        await Assert.That(duplicateClaimed).IsFalse();

        var receiptCount = await context.EmailDispatchReceipts
            .IgnoreQueryFilters()
            .CountAsync(receipt => receipt.TenantId == tenant.Id && receipt.PublishEventId == dispatch.PublishEventId);
        await Assert.That(receiptCount).IsEqualTo(1);
    }

    [Test]
    public async Task ConcurrentProcessingClaimsAllowOnlyOneNodeToSend()
    {
        await fixture.ResetAsync();
        await using (var seedContext = fixture.CreateDbContext())
        {
            var tenant = await SeedTenantAsync(seedContext, "multi-node");
            await SeedDispatchAsync(seedContext, tenant.Id, EmailDispatchStatus.Pending);
        }

        await using var nodeAContext = fixture.CreateDbContext();
        await using var nodeBContext = fixture.CreateDbContext();
        var nodeARepository = new EmailDispatchOutboxRepository(nodeAContext);
        var nodeBRepository = new EmailDispatchOutboxRepository(nodeBContext);
        var nodeANow = DateTime.UtcNow;
        var nodeBNow = nodeANow;
        var nodeARow = (await nodeARepository.GetPendingBatch(1, nodeANow, CancellationToken.None)).Single();
        var nodeBRow = (await nodeBRepository.GetPendingBatch(1, nodeBNow, CancellationToken.None)).Single();
        var simulatedSends = 0;

        var claimResults = await Task.WhenAll(
            ClaimAndSimulateSendAsync(nodeARepository, nodeARow.Id, "provider-node-a"),
            ClaimAndSimulateSendAsync(nodeBRepository, nodeBRow.Id, "provider-node-b"));

        await Assert.That(claimResults.Count(claimed => claimed)).IsEqualTo(1);
        await Assert.That(simulatedSends).IsEqualTo(1);

        await using var verificationContext = fixture.CreateDbContext();
        var row = await verificationContext.EmailDispatchOutbox
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(outbox => outbox.Id == nodeARow.Id);
        await Assert.That(row.Status).IsEqualTo(EmailDispatchStatus.Sent);
        await Assert.That(row.AttemptCount).IsEqualTo(1);
        await Assert.That(row.ProcessingLeaseToken).IsNull();
        await Assert.That(row.ProviderMessageId).StartsWith("provider-node-");

        async Task<bool> ClaimAndSimulateSendAsync(
            EmailDispatchOutboxRepository repository,
            Guid dispatchId,
            string providerMessageId)
        {
            var claimed = await repository.TryMarkAsProcessing(
                dispatchId,
                Guid.NewGuid(),
                DateTime.UtcNow,
                CancellationToken.None);
            if (!claimed)
            {
                return false;
            }

            Interlocked.Increment(ref simulatedSends);
            await repository.MarkAsSent(dispatchId, DateTime.UtcNow, providerMessageId, CancellationToken.None);
            return true;
        }
    }

    [Test]
    public async Task ConcurrentReceiptClaimsAllowOnlyOneNodeToOwnPublishEvent()
    {
        await fixture.ResetAsync();
        await using (var seedContext = fixture.CreateDbContext())
        {
            var tenant = await SeedTenantAsync(seedContext, "receipt-node");
            await SeedDispatchAsync(seedContext, tenant.Id, EmailDispatchStatus.Pending);
        }

        await using var lookupContext = fixture.CreateDbContext();
        var dispatch = await lookupContext.EmailDispatchOutbox
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync();
        await using var nodeAContext = fixture.CreateDbContext();
        await using var nodeBContext = fixture.CreateDbContext();
        var nodeARepository = new EmailDispatchOutboxRepository(nodeAContext);
        var nodeBRepository = new EmailDispatchOutboxRepository(nodeBContext);

        var receiptClaims = await Task.WhenAll(
            nodeARepository.TryClaimReceipt(
                CreateReceipt(dispatch.TenantId, dispatch.PublishEventId, dispatch.Id, "scheduler-node-a"),
                CancellationToken.None),
            nodeBRepository.TryClaimReceipt(
                CreateReceipt(dispatch.TenantId, dispatch.PublishEventId, dispatch.Id, "scheduler-node-b"),
                CancellationToken.None));

        await Assert.That(receiptClaims.Count(claimed => claimed)).IsEqualTo(1);

        await using var verificationContext = fixture.CreateDbContext();
        var receipts = await verificationContext.EmailDispatchReceipts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(receipt => receipt.TenantId == dispatch.TenantId && receipt.PublishEventId == dispatch.PublishEventId)
            .ToListAsync();
        await Assert.That(receipts).Count().IsEqualTo(1);
        await Assert.That(new[] { "scheduler-node-a", "scheduler-node-b" }.Contains(receipts[0].ConsumerId)).IsTrue();
    }

    [Test]
    public async Task MarkStaleProcessingAsUnknownRecoversOnlyExpiredLeases()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "stale-processing");
        var staleStartedAt = DateTime.UtcNow.AddMinutes(-30);
        var freshStartedAt = DateTime.UtcNow.AddMinutes(-2);
        var stale = await SeedProcessingDispatchWithReceiptAsync(context, tenant.Id, staleStartedAt);
        var fresh = await SeedProcessingDispatchWithReceiptAsync(context, tenant.Id, freshStartedAt);
        var recoveredAt = DateTime.UtcNow;
        var repository = new EmailDispatchOutboxRepository(context);

        var recovered = await repository.MarkStaleProcessingAsUnknown(
            DateTime.UtcNow.AddMinutes(-10),
            recoveredAt,
            "processing_lease_expired",
            "lease expired during node shutdown",
            10,
            CancellationToken.None);

        await Assert.That(recovered).IsEqualTo(1);

        var rows = await context.EmailDispatchOutbox
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(outbox => outbox.Id == stale.Id || outbox.Id == fresh.Id)
            .ToDictionaryAsync(outbox => outbox.Id);
        var receipts = await context.EmailDispatchReceipts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(receipt => receipt.EmailDispatchOutboxId == stale.Id || receipt.EmailDispatchOutboxId == fresh.Id)
            .ToDictionaryAsync(receipt => receipt.EmailDispatchOutboxId);

        await Assert.That(rows[stale.Id].Status).IsEqualTo(EmailDispatchStatus.Unknown);
        await Assert.That(rows[stale.Id].UnknownAt).IsNotNull();
        await Assert.That(Math.Abs((rows[stale.Id].UnknownAt!.Value - recoveredAt).TotalMilliseconds)).IsLessThan(10);
        await Assert.That(rows[stale.Id].ProcessingLeaseToken).IsNull();
        await Assert.That(rows[stale.Id].ProcessingStartedAt).IsNull();
        await Assert.That(rows[stale.Id].LastFailureCategory).IsEqualTo("processing_lease_expired");
        await Assert.That(rows[fresh.Id].Status).IsEqualTo(EmailDispatchStatus.Processing);
        await Assert.That(rows[fresh.Id].ProcessingStartedAt).IsNotNull();
        await Assert.That(receipts[stale.Id].Status).IsEqualTo(EmailDispatchReceiptStatus.Unknown);
        await Assert.That(receipts[stale.Id].FailureCode).IsEqualTo("processing_lease_expired");
        await Assert.That(receipts[fresh.Id].Status).IsEqualTo(EmailDispatchReceiptStatus.Processing);
    }

    private static async Task<Tenant> SeedTenantAsync(ExploreDbContext context, string slugPrefix)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            FullName = $"Email Dispatch {slugPrefix}",
            Slug = $"email-dispatch-{slugPrefix}-{Guid.NewGuid().ToString("N")[..8]}",
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!,
        };

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();
        return tenant;
    }

    private static async Task<EmailDispatchOutbox> SeedDispatchAsync(
        ExploreDbContext context,
        Guid tenantId,
        EmailDispatchStatus status)
    {
        var now = DateTime.UtcNow;
        var dispatch = new EmailDispatchOutbox
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PublishEventId = Guid.NewGuid(),
            Kind = EmailDispatchKind.RegistrationConfirmation,
            SourceType = "event_registration_intent",
            SourceId = Guid.NewGuid(),
            RecipientEmail = "recipient@example.test",
            Subject = "Registration confirmation",
            PlainTextBody = "plain body",
            HtmlBody = "<p>html body</p>",
            Status = status,
            AttemptCount = status == EmailDispatchStatus.Pending ? 0 : 3,
            MaxAttempts = 5,
            NextAttemptAt = status == EmailDispatchStatus.RetryScheduled ? now.AddHours(1) : null,
            DeadLetteredAt = status == EmailDispatchStatus.DeadLettered ? now : null,
            ParkedAt = status == EmailDispatchStatus.Parked ? now : null,
            UnknownAt = status == EmailDispatchStatus.Unknown ? now : null,
            SentAt = status == EmailDispatchStatus.Sent ? now : null,
            LastFailureCategory = status == EmailDispatchStatus.Pending ? null : "smtp_send_failed",
            LastError = status == EmailDispatchStatus.Pending ? null : "previous failure",
            LastFailureAt = status == EmailDispatchStatus.Pending ? null : now,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.EmailDispatchOutbox.Add(dispatch);
        await context.SaveChangesAsync();
        return dispatch;
    }

    private static async Task<EmailDispatchOutbox> SeedProcessingDispatchWithReceiptAsync(
        ExploreDbContext context,
        Guid tenantId,
        DateTime processingStartedAt)
    {
        var dispatch = await SeedDispatchAsync(context, tenantId, EmailDispatchStatus.Processing);
        dispatch.AttemptCount = 1;
        dispatch.ProcessingStartedAt = processingStartedAt;
        dispatch.ProcessingLeaseToken = Guid.NewGuid();
        dispatch.UpdatedAt = processingStartedAt;
        context.EmailDispatchReceipts.Add(CreateReceipt(
            tenantId,
            dispatch.PublishEventId,
            dispatch.Id,
            "scheduler-node"));
        await context.SaveChangesAsync();
        return dispatch;
    }

    private static EmailDispatchReceipt CreateReceipt(
        Guid tenantId,
        Guid publishEventId,
        Guid outboxId,
        string consumerId)
    {
        var now = DateTime.UtcNow;
        return new EmailDispatchReceipt
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PublishEventId = publishEventId,
            EmailDispatchOutboxId = outboxId,
            Status = EmailDispatchReceiptStatus.Processing,
            ConsumerId = consumerId,
            FirstSeenAt = now,
            ProcessingStartedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
