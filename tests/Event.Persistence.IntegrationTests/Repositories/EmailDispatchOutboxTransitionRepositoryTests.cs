// ABOUTME: PostgreSQL-backed tests for EmailDispatch operator replay and parking transitions.
// ABOUTME: Verifies durable state-machine changes that future RabbitMQ consumers and admin actions reuse.

using System.Data.Common;
using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Notifications;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Persistence;
using Explore.Persistence.Repositories;
using Explore.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class EmailDispatchOutboxTransitionRepositoryTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task EligibilityRefreshesCurrentVerifiedAddressAndCreatesProviderFenceAtomically()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "eligibility-address");
        var dispatch = await SeedDispatchAsync(context, tenant.Id, EmailDispatchStatus.Pending);
        var user = await context.Users.Include(value => value.Pii).SingleAsync(value => value.Id == dispatch.RecipientUserId);
        user.Email = "current-verified@example.test";
        await context.SaveChangesAsync();
        var repository = new EmailDispatchOutboxRepository(context);
        var leaseToken = Guid.CreateVersion7();
        await Assert.That(await repository.TryMarkAsProcessing(dispatch.Id, leaseToken, DateTime.UtcNow, CancellationToken.None)).IsTrue();
        context.ChangeTracker.Clear();
        var evaluator = CreateEligibilityEvaluator(context);

        var result = await evaluator.EvaluateAndBeginProviderHandoffAsync(
            new EmailDispatchEligibilityRequest(tenant.Id, dispatch.Id, leaseToken, 1, "test-worker", DateTime.UtcNow),
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(EmailDispatchEligibilityOutcome.Eligible);
        await Assert.That(result.RecipientEmail).IsEqualTo("current-verified@example.test");
        var persisted = await context.EmailDispatchOutbox.IgnoreQueryFilters().AsNoTracking().SingleAsync(value => value.Id == dispatch.Id);
        var attempt = await context.EmailDispatchAttempts.IgnoreQueryFilters().AsNoTracking().SingleAsync(value => value.EmailDispatchOutboxId == dispatch.Id);
        var receipt = await context.EmailDispatchReceipts.IgnoreQueryFilters().AsNoTracking().SingleAsync(value => value.EmailDispatchOutboxId == dispatch.Id);
        await Assert.That(persisted.RecipientEmail).IsEqualTo("current-verified@example.test");
        await Assert.That(persisted.Status).IsEqualTo(EmailDispatchStatus.Processing);
        await Assert.That(attempt.FailureCategory).IsEqualTo("provider_handoff_started");
        await Assert.That(attempt.CompletedAt).IsNull();
        await Assert.That(receipt.Status).IsEqualTo(EmailDispatchReceiptStatus.Processing);
    }

    [Test]
    public async Task EligibilitySkipsUnverifiedRecipientAndAlignsAllDeliveryLedgersAtomically()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "eligibility-unverified");
        var dispatch = await SeedDispatchAsync(context, tenant.Id, EmailDispatchStatus.Pending);
        var user = await context.Users.SingleAsync(value => value.Id == dispatch.RecipientUserId);
        user.EmailVerified = false;
        await context.SaveChangesAsync();
        var repository = new EmailDispatchOutboxRepository(context);
        var leaseToken = Guid.CreateVersion7();
        await Assert.That(await repository.TryMarkAsProcessing(dispatch.Id, leaseToken, DateTime.UtcNow, CancellationToken.None)).IsTrue();
        context.ChangeTracker.Clear();

        var result = await CreateEligibilityEvaluator(context).EvaluateAndBeginProviderHandoffAsync(
            new EmailDispatchEligibilityRequest(tenant.Id, dispatch.Id, leaseToken, 1, "test-worker", DateTime.UtcNow),
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(EmailDispatchEligibilityOutcome.Skipped);
        await Assert.That(result.SkipReason).IsEqualTo("recipient_email_unverified");
        var persisted = await context.EmailDispatchOutbox.IgnoreQueryFilters().AsNoTracking().SingleAsync(value => value.Id == dispatch.Id);
        var delivery = await context.NotificationDeliveries.IgnoreQueryFilters().AsNoTracking().SingleAsync(value => value.EmailDispatchOutboxId == dispatch.Id);
        var attempt = await context.EmailDispatchAttempts.IgnoreQueryFilters().AsNoTracking().SingleAsync(value => value.EmailDispatchOutboxId == dispatch.Id);
        var receipt = await context.EmailDispatchReceipts.IgnoreQueryFilters().AsNoTracking().SingleAsync(value => value.EmailDispatchOutboxId == dispatch.Id);
        await Assert.That(persisted.Status).IsEqualTo(EmailDispatchStatus.Skipped);
        await Assert.That(persisted.LastFailureCategory).IsEqualTo("recipient_email_unverified");
        await Assert.That(delivery.StatusId).IsEqualTo((int)NotificationDeliveryStatusEnum.Skipped);
        await Assert.That(delivery.FailureCategory).IsEqualTo("recipient_email_unverified");
        await Assert.That(attempt.Outcome).IsEqualTo(EmailDispatchAttemptOutcome.Skipped);
        await Assert.That(receipt.Status).IsEqualTo(EmailDispatchReceiptStatus.Skipped);
    }

    [Test]
    public async Task EligibilitySkipsSupersededDeliveryBeforeProviderHandoff()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "eligibility-superseded");
        var dispatch = await SeedDispatchAsync(context, tenant.Id, EmailDispatchStatus.Pending);
        var delivery = await context.NotificationDeliveries.SingleAsync(value => value.EmailDispatchOutboxId == dispatch.Id);
        delivery.StatusId = (int)NotificationDeliveryStatusEnum.Superseded;
        await context.SaveChangesAsync();
        var repository = new EmailDispatchOutboxRepository(context);
        var leaseToken = Guid.CreateVersion7();
        await Assert.That(await repository.TryMarkAsProcessing(dispatch.Id, leaseToken, DateTime.UtcNow, CancellationToken.None)).IsTrue();
        context.ChangeTracker.Clear();

        var result = await CreateEligibilityEvaluator(context).EvaluateAndBeginProviderHandoffAsync(
            new EmailDispatchEligibilityRequest(tenant.Id, dispatch.Id, leaseToken, 1, "test-worker", DateTime.UtcNow),
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(EmailDispatchEligibilityOutcome.Skipped);
        await Assert.That(result.SkipReason).IsEqualTo("delivery_superseded");
    }

    [Test]
    public async Task EligibilityDefersTenantPauseWithoutConsumingAttemptBudget()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "eligibility-paused");
        var dispatch = await SeedDispatchAsync(context, tenant.Id, EmailDispatchStatus.Pending);
        var repository = new EmailDispatchOutboxRepository(context);
        var leaseToken = Guid.CreateVersion7();
        await Assert.That(await repository.TryMarkAsProcessing(dispatch.Id, leaseToken, DateTime.UtcNow, CancellationToken.None)).IsTrue();
        await repository.SetTenantPauseState(tenant.Id, true, "maintenance", null, DateTime.UtcNow, CancellationToken.None);
        context.ChangeTracker.Clear();

        var result = await CreateEligibilityEvaluator(context).EvaluateAndBeginProviderHandoffAsync(
            new EmailDispatchEligibilityRequest(tenant.Id, dispatch.Id, leaseToken, 1, "test-worker", DateTime.UtcNow),
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(EmailDispatchEligibilityOutcome.TenantPaused);
        var persisted = await context.EmailDispatchOutbox.IgnoreQueryFilters().AsNoTracking().SingleAsync(value => value.Id == dispatch.Id);
        await Assert.That(persisted.Status).IsEqualTo(EmailDispatchStatus.Pending);
        await Assert.That(persisted.AttemptCount).IsEqualTo(0);
        await Assert.That(await context.EmailDispatchAttempts.IgnoreQueryFilters().CountAsync(value => value.EmailDispatchOutboxId == dispatch.Id)).IsEqualTo(0);
    }

    [Test]
    public async Task EligibilitySkipsInactiveTenantMembershipBeforeProviderHandoff()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "eligibility-membership");
        var dispatch = await SeedDispatchAsync(context, tenant.Id, EmailDispatchStatus.Pending);
        var membership = await context.TenantUsers.SingleAsync(value =>
            value.TenantId == tenant.Id && value.UserId == dispatch.RecipientUserId);
        membership.StatusId = (int)TenantUserStatusEnum.Suspended;
        await context.SaveChangesAsync();
        var repository = new EmailDispatchOutboxRepository(context);
        var leaseToken = Guid.CreateVersion7();
        await Assert.That(await repository.TryMarkAsProcessing(dispatch.Id, leaseToken, DateTime.UtcNow, CancellationToken.None)).IsTrue();
        context.ChangeTracker.Clear();

        var result = await CreateEligibilityEvaluator(context).EvaluateAndBeginProviderHandoffAsync(
            new EmailDispatchEligibilityRequest(tenant.Id, dispatch.Id, leaseToken, 1, "test-worker", DateTime.UtcNow),
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(EmailDispatchEligibilityOutcome.Skipped);
        await Assert.That(result.SkipReason).IsEqualTo("recipient_membership_inactive");
        await Assert.That(await context.EmailDispatchAttempts.IgnoreQueryFilters().AnyAsync(value =>
            value.EmailDispatchOutboxId == dispatch.Id && value.FailureCategory == "provider_handoff_started")).IsFalse();
    }

    [Test]
    public async Task EligibilityFailsClosedWhenPersistedPolicyVersionDrifts()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "eligibility-policy-version");
        var dispatch = await SeedDispatchAsync(context, tenant.Id, EmailDispatchStatus.Pending);
        var delivery = await context.NotificationDeliveries.SingleAsync(value => value.EmailDispatchOutboxId == dispatch.Id);
        delivery.PolicyVersion = 2;
        await context.SaveChangesAsync();
        var repository = new EmailDispatchOutboxRepository(context);
        var leaseToken = Guid.CreateVersion7();
        await Assert.That(await repository.TryMarkAsProcessing(dispatch.Id, leaseToken, DateTime.UtcNow, CancellationToken.None)).IsTrue();
        context.ChangeTracker.Clear();

        var result = await CreateEligibilityEvaluator(context).EvaluateAndBeginProviderHandoffAsync(
            new EmailDispatchEligibilityRequest(tenant.Id, dispatch.Id, leaseToken, 1, "test-worker", DateTime.UtcNow),
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(EmailDispatchEligibilityOutcome.Skipped);
        await Assert.That(result.SkipReason).IsEqualTo("delivery_policy_version_unsupported");
    }

    [Test]
    public async Task EligibilitySkipsOptionalDeliveryWhenRecipientUnsubscribedAfterQueueing()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "eligibility-preference");
        var dispatch = await SeedDispatchAsync(context, tenant.Id, EmailDispatchStatus.Pending);
        context.UserNotificationPreferences.Add(new UserNotificationPreference
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            Tenant = tenant,
            UserId = dispatch.RecipientUserId,
            Category = NotificationPreferenceCategories.RegistrationConfirmations,
            IsEnabled = false,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        var repository = new EmailDispatchOutboxRepository(context);
        var leaseToken = Guid.CreateVersion7();
        await Assert.That(await repository.TryMarkAsProcessing(dispatch.Id, leaseToken, DateTime.UtcNow, CancellationToken.None)).IsTrue();
        context.ChangeTracker.Clear();

        var result = await CreateEligibilityEvaluator(context).EvaluateAndBeginProviderHandoffAsync(
            new EmailDispatchEligibilityRequest(tenant.Id, dispatch.Id, leaseToken, 1, "test-worker", DateTime.UtcNow),
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(EmailDispatchEligibilityOutcome.Skipped);
        await Assert.That(result.SkipReason).IsEqualTo("recipient_unsubscribed");
    }

    [Test]
    public async Task EligibilityPreservesAuthorizationBoundManagedInvitationDestination()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "eligibility-invitation");
        var invitedAddress = "authorized-invitation@example.test";
        var dispatch = await SeedDispatchAsync(
            context,
            tenant.Id,
            EmailDispatchStatus.Pending,
            managedInvitation: true,
            invitationEmail: invitedAddress);
        var repository = new EmailDispatchOutboxRepository(context);
        var leaseToken = Guid.CreateVersion7();
        await Assert.That(await repository.TryMarkAsProcessing(dispatch.Id, leaseToken, DateTime.UtcNow, CancellationToken.None)).IsTrue();
        context.ChangeTracker.Clear();

        var result = await CreateEligibilityEvaluator(context).EvaluateAndBeginProviderHandoffAsync(
            new EmailDispatchEligibilityRequest(tenant.Id, dispatch.Id, leaseToken, 1, "test-worker", DateTime.UtcNow),
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(EmailDispatchEligibilityOutcome.Eligible);
        await Assert.That(result.RecipientEmail).IsEqualTo(invitedAddress);
    }

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
        dispatch.RabbitMqLastPublishedAt = DateTime.UtcNow.AddMinutes(-10);
        dispatch.RabbitMqLastPublishAttemptAt = DateTime.UtcNow.AddMinutes(-10);
        dispatch.RabbitMqPublishAttemptCount = 3;
        dispatch.RabbitMqLastPublishFailureCategory = "publisher_nack";
        await context.SaveChangesAsync();
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
        await Assert.That(row.RabbitMqLastPublishedAt).IsNull();
        await Assert.That(row.RabbitMqLastPublishAttemptAt).IsNull();
        await Assert.That(row.RabbitMqPublishAttemptCount).IsEqualTo(0);
        await Assert.That(row.RabbitMqLastPublishFailureCategory).IsNull();
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
    public async Task TryParkForOperatorDoesNotParkSkippedRow()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "skipped-park");
        var dispatch = await SeedDispatchAsync(context, tenant.Id, EmailDispatchStatus.Skipped);
        var repository = new EmailDispatchOutboxRepository(context);

        var parked = await repository.TryParkForOperator(
            tenant.Id,
            dispatch.Id,
            "manual review",
            Guid.NewGuid(),
            DateTime.UtcNow,
            CancellationToken.None);

        await Assert.That(parked).IsFalse();

        var row = await context.EmailDispatchOutbox
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(outbox => outbox.Id == dispatch.Id);
        await Assert.That(row.Status).IsEqualTo(EmailDispatchStatus.Skipped);
        await Assert.That(row.ParkedAt).IsNull();
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
    public async Task MarkAsSkippedSettlesOutboxAndReceiptWithoutRetry()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "skip");
        var dispatch = await SeedProcessingDispatchWithReceiptAsync(context, tenant.Id, DateTime.UtcNow.AddSeconds(-30));
        var receipt = await context.EmailDispatchReceipts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(row => row.EmailDispatchOutboxId == dispatch.Id);
        var skippedAt = DateTime.UtcNow;
        var repository = new EmailDispatchOutboxRepository(context);

        await repository.MarkAsSkipped(
            dispatch.Id,
            "recipient_unsubscribed",
            "Recipient opted out before SMTP send.",
            skippedAt,
            CancellationToken.None);
        await repository.MarkReceiptSkipped(
            receipt.Id,
            "recipient_unsubscribed",
            "Recipient opted out before SMTP send.",
            skippedAt,
            CancellationToken.None);

        var row = await context.EmailDispatchOutbox
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(outbox => outbox.Id == dispatch.Id);
        var receiptRow = await context.EmailDispatchReceipts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(row => row.Id == receipt.Id);

        await Assert.That(row.Status).IsEqualTo(EmailDispatchStatus.Skipped);
        await Assert.That(row.NextAttemptAt).IsNull();
        await Assert.That(row.ProcessingStartedAt).IsNull();
        await Assert.That(row.ProcessingLeaseToken).IsNull();
        await Assert.That(row.LastFailureCategory).IsEqualTo("recipient_unsubscribed");
        await Assert.That(row.LastError).IsEqualTo("Recipient opted out before SMTP send.");
        await Assert.That(Math.Abs((row.LastFailureAt!.Value - skippedAt).TotalMilliseconds)).IsLessThan(10);
        await Assert.That(receiptRow.Status).IsEqualTo(EmailDispatchReceiptStatus.Skipped);
        await Assert.That(receiptRow.FailureCode).IsEqualTo("recipient_unsubscribed");
        await Assert.That(receiptRow.FailureMessage).IsEqualTo("Recipient opted out before SMTP send.");
        await Assert.That(Math.Abs((receiptRow.FailedAt!.Value - skippedAt).TotalMilliseconds)).IsLessThan(10);
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
    public async Task GetRabbitMqPublishBatchReturnsDueUnpausedRowsOnly()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var activeTenant = await SeedTenantAsync(context, "rabbitmq-active");
        var pausedTenant = await SeedTenantAsync(context, "rabbitmq-paused");
        var now = DateTime.UtcNow;
        var eligible = await SeedDispatchAsync(context, activeTenant.Id, EmailDispatchStatus.Pending);
        var throttled = await SeedDispatchAsync(context, activeTenant.Id, EmailDispatchStatus.Pending);
        var deferred = await SeedDispatchAsync(context, activeTenant.Id, EmailDispatchStatus.RetryScheduled);
        var sent = await SeedDispatchAsync(context, activeTenant.Id, EmailDispatchStatus.Sent);
        var paused = await SeedDispatchAsync(context, pausedTenant.Id, EmailDispatchStatus.Pending);
        throttled.RabbitMqLastPublishAttemptAt = now.AddSeconds(-5);
        deferred.NextAttemptAt = now.AddMinutes(10);
        context.EmailDispatchTenantControls.Add(new EmailDispatchTenantControl
        {
            Id = Guid.NewGuid(),
            TenantId = pausedTenant.Id,
            IsPaused = true,
            CreatedAt = now,
            UpdatedAt = now
        });
        await context.SaveChangesAsync();
        var repository = new EmailDispatchOutboxRepository(context);

        IReadOnlyList<EmailDispatchOutbox> rows = await repository.GetRabbitMqPublishBatch(
            10,
            now,
            now.AddSeconds(-30),
            CancellationToken.None);

        await Assert.That(rows.Select(row => row.Id)).IsEquivalentTo([eligible.Id]);
        await Assert.That(rows.Select(row => row.Id)).DoesNotContain(throttled.Id);
        await Assert.That(rows.Select(row => row.Id)).DoesNotContain(deferred.Id);
        await Assert.That(rows.Select(row => row.Id)).DoesNotContain(sent.Id);
        await Assert.That(rows.Select(row => row.Id)).DoesNotContain(paused.Id);
    }

    [Test]
    public async Task RabbitMqPublishMarkersUpdateProducerMetadata()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "rabbitmq-markers");
        var success = await SeedDispatchAsync(context, tenant.Id, EmailDispatchStatus.Pending);
        var failure = await SeedDispatchAsync(context, tenant.Id, EmailDispatchStatus.Pending);
        var repository = new EmailDispatchOutboxRepository(context);
        var publishedAt = DateTime.UtcNow.AddMinutes(-2);
        var failedAt = DateTime.UtcNow;

        await repository.MarkRabbitMqPublishSucceeded(success.Id, publishedAt, CancellationToken.None);
        await repository.MarkRabbitMqPublishFailed(failure.Id, "mandatory_return", failedAt, CancellationToken.None);

        var rows = await context.EmailDispatchOutbox
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(outbox => outbox.Id == success.Id || outbox.Id == failure.Id)
            .ToDictionaryAsync(outbox => outbox.Id);
        await Assert.That(rows[success.Id].RabbitMqLastPublishedAt).IsNotNull();
        await Assert.That(Math.Abs((rows[success.Id].RabbitMqLastPublishedAt!.Value - publishedAt).TotalMilliseconds)).IsLessThan(10);
        await Assert.That(rows[success.Id].RabbitMqLastPublishAttemptAt).IsNotNull();
        await Assert.That(rows[success.Id].RabbitMqPublishAttemptCount).IsEqualTo(1);
        await Assert.That(rows[success.Id].RabbitMqLastPublishFailureCategory).IsNull();
        await Assert.That(rows[failure.Id].RabbitMqLastPublishedAt).IsNull();
        await Assert.That(rows[failure.Id].RabbitMqLastPublishAttemptAt).IsNotNull();
        await Assert.That(rows[failure.Id].RabbitMqPublishAttemptCount).IsEqualTo(1);
        await Assert.That(rows[failure.Id].RabbitMqLastPublishFailureCategory).IsEqualTo("mandatory_return");
    }

    [Test]
    public async Task HealthCountMethodsCountDueRetryStaleProcessingAndDeadLetterRowsAcrossTenants()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "health");
        var otherTenant = await SeedTenantAsync(context, "health-other");
        var now = DateTime.UtcNow;
        await SeedDispatchAsync(context, tenant.Id, EmailDispatchStatus.Pending);
        var dueRetry = await SeedDispatchAsync(context, tenant.Id, EmailDispatchStatus.RetryScheduled);
        var futureRetry = await SeedDispatchAsync(context, otherTenant.Id, EmailDispatchStatus.RetryScheduled);
        await SeedProcessingDispatchWithReceiptAsync(context, tenant.Id, now.AddMinutes(-30));
        await SeedProcessingDispatchWithReceiptAsync(context, otherTenant.Id, now.AddMinutes(-2));
        await SeedDispatchAsync(context, otherTenant.Id, EmailDispatchStatus.DeadLettered);
        await SeedDispatchAsync(context, tenant.Id, EmailDispatchStatus.Sent);
        dueRetry.NextAttemptAt = now.AddMinutes(-5);
        futureRetry.NextAttemptAt = now.AddMinutes(30);
        await context.SaveChangesAsync();
        var repository = new EmailDispatchOutboxRepository(context);

        var dueDispatchCount = await repository.CountDueDispatchAsync(now, CancellationToken.None);
        var retryScheduledCount = await repository.CountRetryScheduledAsync(CancellationToken.None);
        var staleProcessingCount = await repository.CountStaleProcessingAsync(
            now.AddMinutes(-10),
            CancellationToken.None);
        var deadLetteredCount = await repository.CountDeadLetteredAsync(CancellationToken.None);

        await Assert.That(dueDispatchCount).IsEqualTo(2);
        await Assert.That(retryScheduledCount).IsEqualTo(2);
        await Assert.That(staleProcessingCount).IsEqualTo(1);
        await Assert.That(deadLetteredCount).IsEqualTo(1);
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

    [Test]
    public async Task SettleProviderAcceptedAtomicallyAlignsAttemptReceiptOutboxAndEmailDelivery()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var graph = await SeedAcceptedSettlementGraphAsync(context, "accepted-settlement", DateTime.UtcNow);
        var repository = new EmailDispatchOutboxRepository(context);
        var settledAt = DateTime.UtcNow;
        var settlement = new EmailDispatchAcceptedSettlement(
            graph.Dispatch.TenantId,
            graph.Dispatch.Id,
            graph.Attempt.AttemptNumber,
            settledAt,
            "provider-message-accepted");

        await repository.SettleProviderAccepted(settlement, CancellationToken.None);

        context.ChangeTracker.Clear();
        var attempt = await context.EmailDispatchAttempts.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(row => row.Id == graph.Attempt.Id);
        var receipt = await context.EmailDispatchReceipts.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(row => row.Id == graph.Receipt.Id);
        var dispatch = await context.EmailDispatchOutbox.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(row => row.Id == graph.Dispatch.Id);
        var delivery = await context.NotificationDeliveries.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(row => row.Id == graph.Delivery.Id);
        var due = await repository.GetPendingBatch(10, DateTime.UtcNow.AddHours(1), CancellationToken.None);

        await Assert.That(attempt.Outcome).IsEqualTo(EmailDispatchAttemptOutcome.Succeeded);
        await Assert.That(attempt.ProviderMessageId).IsEqualTo("provider-message-accepted");
        await Assert.That(receipt.Status).IsEqualTo(EmailDispatchReceiptStatus.Completed);
        await Assert.That(dispatch.Status).IsEqualTo(EmailDispatchStatus.Sent);
        await Assert.That(dispatch.NextAttemptAt).IsNull();
        await Assert.That(delivery.StatusId).IsEqualTo((int)NotificationDeliveryStatusEnum.Delivered);
        await Assert.That(delivery.ProviderStatus).IsEqualTo("accepted");
        await Assert.That(due.Select(row => row.Id)).DoesNotContain(graph.Dispatch.Id);
    }

    [Test]
    public async Task ReconcileProviderAcceptedConvertsPartialSettlementToSanitizedUnknownWithoutRetry()
    {
        const string canary = "provider-canary attendee@example.test body-canary";
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var graph = await SeedAcceptedSettlementGraphAsync(context, "partial-settlement", DateTime.UtcNow);
        graph.Attempt.Outcome = EmailDispatchAttemptOutcome.Succeeded;
        graph.Attempt.CompletedAt = DateTime.UtcNow;
        graph.Attempt.FailureCategory = null;
        graph.Attempt.SanitizedErrorMessage = null;
        graph.Attempt.ProviderMessageId = canary;
        graph.Receipt.Status = EmailDispatchReceiptStatus.Completed;
        graph.Receipt.CompletedAt = DateTime.UtcNow;
        graph.Receipt.ProviderMessageId = canary;
        graph.Dispatch.Status = EmailDispatchStatus.Sent;
        graph.Dispatch.SentAt = DateTime.UtcNow;
        graph.Dispatch.ProviderMessageId = canary;
        graph.Dispatch.ProcessingStartedAt = null;
        graph.Dispatch.ProcessingLeaseToken = null;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new EmailDispatchOutboxRepository(context);
        var settlement = new EmailDispatchAcceptedSettlement(
            graph.Dispatch.TenantId,
            graph.Dispatch.Id,
            graph.Attempt.AttemptNumber,
            DateTime.UtcNow,
            canary);

        EmailDispatchAcceptedReconciliationOutcome outcome = await repository.ReconcileProviderAccepted(
            settlement,
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var attempt = await context.EmailDispatchAttempts.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(row => row.Id == graph.Attempt.Id);
        var receipt = await context.EmailDispatchReceipts.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(row => row.Id == graph.Receipt.Id);
        var dispatch = await context.EmailDispatchOutbox.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(row => row.Id == graph.Dispatch.Id);
        var delivery = await context.NotificationDeliveries.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(row => row.Id == graph.Delivery.Id);
        var due = await repository.GetPendingBatch(10, DateTime.UtcNow.AddHours(1), CancellationToken.None);

        await Assert.That(outcome).IsEqualTo(EmailDispatchAcceptedReconciliationOutcome.Unknown);
        await Assert.That(attempt.Outcome).IsEqualTo(EmailDispatchAttemptOutcome.Unknown);
        await Assert.That(attempt.SanitizedErrorMessage).DoesNotContain(canary);
        await Assert.That(attempt.ProviderMessageId).IsNull();
        await Assert.That(receipt.Status).IsEqualTo(EmailDispatchReceiptStatus.Unknown);
        await Assert.That(receipt.FailureMessage).DoesNotContain(canary);
        await Assert.That(receipt.ProviderMessageId).IsNull();
        await Assert.That(dispatch.Status).IsEqualTo(EmailDispatchStatus.Unknown);
        await Assert.That(dispatch.LastError).DoesNotContain(canary);
        await Assert.That(dispatch.ProviderMessageId).IsNull();
        await Assert.That(dispatch.NextAttemptAt).IsNull();
        await Assert.That(delivery.StatusId).IsEqualTo((int)NotificationDeliveryStatusEnum.Unknown);
        await Assert.That(delivery.ProviderMessageId).IsNull();
        await Assert.That(due.Select(row => row.Id)).DoesNotContain(graph.Dispatch.Id);
    }

    [Test]
    public async Task ReconcileProviderAcceptedRecognizesAlreadyCommittedAlignedSentGraph()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var graph = await SeedAcceptedSettlementGraphAsync(context, "committed-settlement", DateTime.UtcNow);
        var repository = new EmailDispatchOutboxRepository(context);
        var settlement = new EmailDispatchAcceptedSettlement(
            graph.Dispatch.TenantId,
            graph.Dispatch.Id,
            graph.Attempt.AttemptNumber,
            DateTime.UtcNow,
            "provider-message-committed");
        await repository.SettleProviderAccepted(settlement, CancellationToken.None);
        context.ChangeTracker.Clear();

        EmailDispatchAcceptedReconciliationOutcome outcome = await repository.ReconcileProviderAccepted(
            settlement,
            CancellationToken.None);

        var dispatch = await context.EmailDispatchOutbox.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(row => row.Id == graph.Dispatch.Id);
        var delivery = await context.NotificationDeliveries.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(row => row.Id == graph.Delivery.Id);
        await Assert.That(outcome).IsEqualTo(EmailDispatchAcceptedReconciliationOutcome.Sent);
        await Assert.That(dispatch.Status).IsEqualTo(EmailDispatchStatus.Sent);
        await Assert.That(dispatch.UnknownAt).IsNull();
        await Assert.That(delivery.StatusId).IsEqualTo((int)NotificationDeliveryStatusEnum.Delivered);
    }

    [Test]
    public async Task MarkStaleProcessingAsUnknownAlignsFencedRecipientDeliveryGraph()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var processingStartedAt = DateTime.UtcNow.AddMinutes(-30);
        var graph = await SeedAcceptedSettlementGraphAsync(context, "stale-fenced", processingStartedAt);
        graph.Attempt.Outcome = EmailDispatchAttemptOutcome.Failed;
        graph.Attempt.CompletedAt = processingStartedAt.AddMinutes(-5);
        graph.Attempt.FailureCategory = "previous_attempt_failed";
        graph.Attempt.SanitizedErrorMessage = "Previous SMTP attempt failed before provider acceptance.";
        graph.Dispatch.AttemptCount = 2;
        var currentAttempt = new EmailDispatchAttempt
        {
            Id = Guid.CreateVersion7(),
            TenantId = graph.Dispatch.TenantId,
            EmailDispatchOutboxId = graph.Dispatch.Id,
            AttemptNumber = 2,
            Outcome = EmailDispatchAttemptOutcome.Unknown,
            StartedAt = processingStartedAt,
            FailureCategory = "provider_handoff_started",
            SanitizedErrorMessage = "SMTP provider handoff started; automatic resend is suppressed until settlement.",
            CreatedAt = processingStartedAt
        };
        context.EmailDispatchAttempts.Add(currentAttempt);
        await context.SaveChangesAsync();
        var repository = new EmailDispatchOutboxRepository(context);
        var recoveredAt = DateTime.UtcNow;

        var recovered = await repository.MarkStaleProcessingAsUnknown(
            DateTime.UtcNow.AddMinutes(-10),
            recoveredAt,
            "processing_lease_expired",
            "Provider handoff lease expired; automatic resend is disabled.",
            10,
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var attempts = await context.EmailDispatchAttempts.IgnoreQueryFilters().AsNoTracking()
            .Where(row => row.EmailDispatchOutboxId == graph.Dispatch.Id)
            .ToDictionaryAsync(row => row.AttemptNumber);
        var receipt = await context.EmailDispatchReceipts.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(row => row.Id == graph.Receipt.Id);
        var dispatch = await context.EmailDispatchOutbox.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(row => row.Id == graph.Dispatch.Id);
        var delivery = await context.NotificationDeliveries.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(row => row.Id == graph.Delivery.Id);
        var due = await repository.GetPendingBatch(10, DateTime.UtcNow.AddHours(1), CancellationToken.None);

        await Assert.That(recovered).IsEqualTo(1);
        await Assert.That(attempts[1].Outcome).IsEqualTo(EmailDispatchAttemptOutcome.Failed);
        await Assert.That(attempts[1].FailureCategory).IsEqualTo("previous_attempt_failed");
        await Assert.That(attempts[2].Outcome).IsEqualTo(EmailDispatchAttemptOutcome.Unknown);
        await Assert.That(attempts[2].FailureCategory).IsEqualTo("processing_lease_expired");
        await Assert.That(receipt.Status).IsEqualTo(EmailDispatchReceiptStatus.Unknown);
        await Assert.That(dispatch.Status).IsEqualTo(EmailDispatchStatus.Unknown);
        await Assert.That(dispatch.NextAttemptAt).IsNull();
        await Assert.That(delivery.StatusId).IsEqualTo((int)NotificationDeliveryStatusEnum.Unknown);
        await Assert.That(due.Select(row => row.Id)).DoesNotContain(graph.Dispatch.Id);
    }

    [Test]
    [Arguments("email_dispatch_attempts")]
    [Arguments("email_dispatch_receipts")]
    [Arguments("email_dispatch_outbox")]
    [Arguments("notification_deliveries")]
    public async Task AcceptedSettlementStageFailureRollsBackThenFreshContextAlignsUnknown(string failingTable)
    {
        await fixture.ResetAsync();
        AcceptedSettlementGraph graph;
        await using (var seedContext = fixture.CreateDbContext())
        {
            graph = await SeedAcceptedSettlementGraphAsync(seedContext, $"fault-{failingTable}", DateTime.UtcNow);
        }

        var settlement = new EmailDispatchAcceptedSettlement(
            graph.Dispatch.TenantId,
            graph.Dispatch.Id,
            graph.Attempt.AttemptNumber,
            DateTime.UtcNow,
            "provider-message-accepted");
        await using (var failingContext = CreateDbContext(new SettlementStageFailureInterceptor(failingTable)))
        {
            var failingRepository = new EmailDispatchOutboxRepository(failingContext);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                failingRepository.SettleProviderAccepted(settlement, CancellationToken.None));
        }

        await using var reconciliationContext = fixture.CreateDbContext();
        var reconciliationRepository = new EmailDispatchOutboxRepository(reconciliationContext);
        var outcome = await reconciliationRepository.ReconcileProviderAccepted(settlement, CancellationToken.None);

        var attempt = await reconciliationContext.EmailDispatchAttempts.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(row => row.Id == graph.Attempt.Id);
        var receipt = await reconciliationContext.EmailDispatchReceipts.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(row => row.Id == graph.Receipt.Id);
        var dispatch = await reconciliationContext.EmailDispatchOutbox.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(row => row.Id == graph.Dispatch.Id);
        var delivery = await reconciliationContext.NotificationDeliveries.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(row => row.Id == graph.Delivery.Id);

        await Assert.That(outcome).IsEqualTo(EmailDispatchAcceptedReconciliationOutcome.Unknown);
        await Assert.That(attempt.Outcome).IsEqualTo(EmailDispatchAttemptOutcome.Unknown);
        await Assert.That(receipt.Status).IsEqualTo(EmailDispatchReceiptStatus.Unknown);
        await Assert.That(dispatch.Status).IsEqualTo(EmailDispatchStatus.Unknown);
        await Assert.That(dispatch.NextAttemptAt).IsNull();
        await Assert.That(delivery.StatusId).IsEqualTo((int)NotificationDeliveryStatusEnum.Unknown);
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

    private ExploreDbContext CreateDbContext(DbCommandInterceptor interceptor)
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .AddInterceptors(interceptor)
            .Options;
        var context = new ExploreDbContext(options);
        context.EnableTenantFilterBypass("Email settlement fault-injection test.");
        return context;
    }

    private static async Task<AcceptedSettlementGraph> SeedAcceptedSettlementGraphAsync(
        ExploreDbContext context,
        string slugPrefix,
        DateTime processingStartedAt)
    {
        var tenant = await SeedTenantAsync(context, slugPrefix);
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Pii = new UserPii
            {
                Email = $"{slugPrefix}@example.test",
                FirstName = "Email",
                LastName = "Recipient"
            },
            EmailVerified = true,
            CreatedAt = processingStartedAt
        };
        var tenantUser = new TenantUser
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            Tenant = tenant,
            UserId = user.Id,
            User = user,
            StatusId = (int)TenantUserStatusEnum.Active,
            JoinedAt = processingStartedAt,
            CreatedAt = processingStartedAt
        };
        var intent = new NotificationIntent
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            CategoryId = (int)NotificationCategoryEnum.RegistrationLifecycle,
            OwnershipTypeId = (int)NotificationOwnershipTypeEnum.IslamuEvent,
            RecipientKindId = (int)NotificationRecipientKindEnum.User,
            StatusId = (int)NotificationIntentStatusEnum.DispatchQueued,
            TemplateKey = "registration.confirmed",
            DeduplicationKey = $"accepted-settlement:{slugPrefix}:{Guid.CreateVersion7()}",
            RecipientUserId = user.Id,
            RecipientTenantUser = tenantUser,
            CreatedAt = processingStartedAt
        };
        var dispatch = new EmailDispatchOutbox
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            PublishEventId = Guid.CreateVersion7(),
            Kind = EmailDispatchKind.RegistrationConfirmation,
            SourceType = "notification_intent",
            SourceId = intent.Id,
            NotificationIntentId = intent.Id,
            NotificationIntent = intent,
            RecipientUserId = user.Id,
            RecipientTenantUser = tenantUser,
            RecipientAddressSource = RecipientAddressSource.TenantUserVerifiedEmail,
            RecipientEmail = user.Email,
            Subject = "Registration confirmation",
            PlainTextBody = "Registration confirmed.",
            Status = EmailDispatchStatus.Processing,
            AttemptCount = 1,
            MaxAttempts = 5,
            ProcessingStartedAt = processingStartedAt,
            ProcessingLeaseToken = Guid.CreateVersion7(),
            CreatedAt = processingStartedAt,
            UpdatedAt = processingStartedAt
        };
        var delivery = new NotificationDelivery
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            NotificationIntentId = intent.Id,
            NotificationIntent = intent,
            ChannelId = (int)NotificationPreferenceChannelEnum.Email,
            DeliveryPolicyId = (int)NotificationDeliveryPolicyEnum.RegistrationStatusOptional,
            IsRequired = false,
            PolicyVersion = 1,
            PreferenceCategoryCode = NotificationPreferenceCategoryCodes.RegistrationStatus,
            RecipientAddressSource = RecipientAddressSource.TenantUserVerifiedEmail,
            DisclosureLevel = "standard",
            TemplateKey = intent.TemplateKey,
            TemplateVersion = 1,
            LinkAllowed = true,
            EmailDispatchOutboxId = dispatch.Id,
            EmailDispatchOutbox = dispatch,
            StatusId = (int)NotificationDeliveryStatusEnum.Queued,
            ProviderStatus = "queued",
            QueuedAt = processingStartedAt,
            CreatedAt = processingStartedAt
        };
        var attempt = new EmailDispatchAttempt
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            EmailDispatchOutboxId = dispatch.Id,
            EmailDispatchOutbox = dispatch,
            AttemptNumber = 1,
            Outcome = EmailDispatchAttemptOutcome.Unknown,
            StartedAt = processingStartedAt,
            FailureCategory = "provider_handoff_started",
            SanitizedErrorMessage = "SMTP provider handoff started; automatic resend is suppressed until settlement.",
            CreatedAt = processingStartedAt
        };
        var receipt = CreateReceipt(
            tenant.Id,
            dispatch.PublishEventId,
            dispatch.Id,
            "scheduler-node");
        receipt.Id = Guid.CreateVersion7();
        receipt.EmailDispatchOutbox = dispatch;
        receipt.FirstSeenAt = processingStartedAt;
        receipt.ProcessingStartedAt = processingStartedAt;
        receipt.CreatedAt = processingStartedAt;

        context.Users.Add(user);
        context.TenantUsers.Add(tenantUser);
        context.NotificationIntents.Add(intent);
        context.EmailDispatchOutbox.Add(dispatch);
        context.NotificationDeliveries.Add(delivery);
        context.EmailDispatchAttempts.Add(attempt);
        context.EmailDispatchReceipts.Add(receipt);
        await context.SaveChangesAsync();
        return new AcceptedSettlementGraph(dispatch, attempt, receipt, delivery);
    }

    private static async Task<EmailDispatchOutbox> SeedDispatchAsync(
        ExploreDbContext context,
        Guid tenantId,
        EmailDispatchStatus status,
        bool managedInvitation = false,
        string? invitationEmail = null)
    {
        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Pii = new UserPii
            {
                Email = $"recipient-{Guid.CreateVersion7():N}@example.test",
                FirstName = "Email",
                LastName = "Recipient",
            },
            EmailVerified = true,
            CreatedAt = now,
        };
        var tenantUser = new TenantUser
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Tenant = null!,
            UserId = user.Id,
            User = user,
            StatusId = (int)TenantUserStatusEnum.Active,
            JoinedAt = now,
            CreatedAt = now,
        };
        var intent = new NotificationIntent
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            CategoryId = (int)NotificationCategoryEnum.RegistrationLifecycle,
            OwnershipTypeId = (int)NotificationOwnershipTypeEnum.IslamuEvent,
            RecipientKindId = (int)NotificationRecipientKindEnum.User,
            StatusId = (int)NotificationIntentStatusEnum.DispatchQueued,
            TemplateKey = "registration.confirmed",
            DeduplicationKey = $"transition-seed:{Guid.CreateVersion7():N}",
            RecipientUserId = user.Id,
            RecipientTenantUser = tenantUser,
            CreatedAt = now,
        };
        ManagedTenantProvisioningOperation? operation = null;
        if (managedInvitation)
        {
            operation = new ManagedTenantProvisioningOperation
            {
                Id = Guid.CreateVersion7(),
                ManagedInstanceId = Guid.CreateVersion7(),
                ExternalRequestId = $"request-{Guid.CreateVersion7():N}",
                ExternalCustomerReference = $"customer-{Guid.CreateVersion7():N}",
                RequestHash = new string('a', 64),
                RequestJson = null,
                TenantSlug = "managed-invitation",
                CurrentOutboxMessageId = Guid.CreateVersion7(),
                Status = ManagedTenantProvisioningStatus.Succeeded,
                TenantId = tenantId,
                TenantAdministratorUserId = user.Id,
                CompletedAt = now,
                CreatedAt = now
            };
        }

        var recipientAddressSource = managedInvitation
            ? RecipientAddressSource.ManagedTenantAdministratorInvitation
            : RecipientAddressSource.TenantUserVerifiedEmail;
        var dispatch = new EmailDispatchOutbox
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            PublishEventId = Guid.CreateVersion7(),
            Kind = managedInvitation
                ? EmailDispatchKind.TenantAdministratorInvitation
                : EmailDispatchKind.RegistrationConfirmation,
            SourceType = managedInvitation ? "managed_tenant_provisioning" : "notification_intent",
            SourceId = operation?.Id ?? intent.Id,
            NotificationIntentId = intent.Id,
            NotificationIntent = intent,
            RecipientUserId = user.Id,
            RecipientTenantUser = tenantUser,
            RecipientAddressSource = recipientAddressSource,
            ManagedTenantProvisioningOperationId = operation?.Id,
            RecipientEmail = managedInvitation ? invitationEmail! : user.Email,
            Subject = "Registration confirmation",
            PlainTextBody = "plain body",
            HtmlBody = "<p>html body</p>",
            Status = status,
            AttemptCount = status == EmailDispatchStatus.Pending ? 0 : 3,
            MaxAttempts = 5,
            NextAttemptAt = status == EmailDispatchStatus.RetryScheduled ? now.AddHours(1) : null,
            ProcessingStartedAt = status == EmailDispatchStatus.Processing ? now : null,
            ProcessingLeaseToken = status == EmailDispatchStatus.Processing ? Guid.CreateVersion7() : null,
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
        var delivery = new NotificationDelivery
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            NotificationIntentId = intent.Id,
            NotificationIntent = intent,
            ChannelId = (int)NotificationPreferenceChannelEnum.Email,
            DeliveryPolicyId = managedInvitation
                ? (int)NotificationDeliveryPolicyEnum.TenantAdministrationRequired
                : (int)NotificationDeliveryPolicyEnum.RegistrationStatusOptional,
            IsRequired = managedInvitation,
            PolicyVersion = 1,
            PreferenceCategoryCode = managedInvitation ? null : NotificationPreferenceCategoryCodes.RegistrationStatus,
            RecipientAddressSource = recipientAddressSource,
            DisclosureLevel = "standard",
            TemplateKey = intent.TemplateKey,
            TemplateVersion = 1,
            LinkAllowed = false,
            EmailDispatchOutboxId = dispatch.Id,
            EmailDispatchOutbox = dispatch,
            StatusId = status switch
            {
                EmailDispatchStatus.Sent => (int)NotificationDeliveryStatusEnum.Delivered,
                EmailDispatchStatus.Skipped => (int)NotificationDeliveryStatusEnum.Skipped,
                EmailDispatchStatus.DeadLettered => (int)NotificationDeliveryStatusEnum.DeadLettered,
                EmailDispatchStatus.Parked => (int)NotificationDeliveryStatusEnum.Parked,
                EmailDispatchStatus.Unknown => (int)NotificationDeliveryStatusEnum.Unknown,
                _ => (int)NotificationDeliveryStatusEnum.Queued,
            },
            QueuedAt = now,
            CompletedAt = status is EmailDispatchStatus.Sent or EmailDispatchStatus.Skipped ? now : null,
            CreatedAt = now,
        };
        intent.Deliveries.Add(delivery);

        context.Users.Add(user);
        context.TenantUsers.Add(tenantUser);
        if (operation is not null)
        {
            context.ManagedTenantProvisioningOperations.Add(operation);
        }
        context.NotificationIntents.Add(intent);
        await context.SaveChangesAsync();
        return dispatch;
    }

    private static EmailDispatchEligibilityEvaluator CreateEligibilityEvaluator(ExploreDbContext context) =>
        new(
            context,
            new NotificationDeliveryPolicyResolver(),
            new NotificationPreferenceResolver(context));

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

    private sealed record AcceptedSettlementGraph(
        EmailDispatchOutbox Dispatch,
        EmailDispatchAttempt Attempt,
        EmailDispatchReceipt Receipt,
        NotificationDelivery Delivery);

    private sealed class SettlementStageFailureInterceptor(string failingTable) : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains($"UPDATE {failingTable}", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Injected accepted-settlement failure at {failingTable}.");
            }

            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
