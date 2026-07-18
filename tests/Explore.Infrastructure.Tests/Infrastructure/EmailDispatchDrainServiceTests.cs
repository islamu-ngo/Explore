// ABOUTME: Unit tests for broker-neutral EmailDispatch single-row drainage.
// ABOUTME: Verifies RabbitMQ consumers can reuse SMTP state transitions without owning delivery logic.

using System.Diagnostics.Metrics;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Models;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Infrastructure;
using Explore.Infrastructure.Tests.Fixtures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Infrastructure;

[Category(InfrastructureTestCategories.Email)]
public sealed class EmailDispatchDrainServiceTests
{
    [Test]
    public async Task ProcessBatchAsyncUsesHysteresisToDeferAndResumeOptionalReminders()
    {
        var settings = new EmailDispatchProcessorSettings
        {
            BatchSize = 10,
            MaxRowsPerTenantPerBatch = 2,
            OptionalBacklogHighWatermark = 5,
            OptionalBacklogLowWatermark = 2
        };
        var fixture = new Fixture(settings);
        fixture.Repository.CountDueDispatchAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(5, 2);
        fixture.Repository.GetPendingBatch(
                10,
                2,
                Arg.Any<bool>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns([]);

        await fixture.Service.ProcessBatchAsync(CancellationToken.None);
        await fixture.Service.ProcessBatchAsync(CancellationToken.None);

        await fixture.Repository.Received(1).GetPendingBatch(
            10,
            2,
            false,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await fixture.Repository.Received(1).GetPendingBatch(
            10,
            2,
            true,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessSingleAsyncReturnsMissingWithoutSendingWhenOutboxRowDoesNotExist()
    {
        var fixture = new Fixture();
        var tenantId = Guid.CreateVersion7();
        var publishEventId = Guid.CreateVersion7();
        fixture.Repository.GetByTenantAndPublishEventId(tenantId, publishEventId, Arg.Any<CancellationToken>())
            .Returns((EmailDispatchOutbox?)null);

        EmailDispatchSingleDrainResult result = await fixture.Service.ProcessSingleAsync(
            tenantId,
            publishEventId,
            "rabbit-consumer-1",
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(EmailDispatchDrainOutcome.Missing);
        await fixture.EmailService.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
    }

    [Test]
    public async Task ProcessSingleAsyncReturnsAlreadySettledWithoutSendingForSentRows()
    {
        var fixture = new Fixture();
        var dispatch = CreateDispatch(EmailDispatchStatus.Sent);
        fixture.Repository.GetByTenantAndPublishEventId(dispatch.TenantId, dispatch.PublishEventId, Arg.Any<CancellationToken>())
            .Returns(dispatch);

        EmailDispatchSingleDrainResult result = await fixture.Service.ProcessSingleAsync(
            dispatch.TenantId,
            dispatch.PublishEventId,
            "rabbit-consumer-1",
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(EmailDispatchDrainOutcome.AlreadySettled);
        await Assert.That(result.OutboxId).IsEqualTo(dispatch.Id);
        await fixture.EmailService.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
    }

    [Test]
    public async Task ProcessSingleAsyncReturnsDeferredWithoutSendingForFutureRetryRows()
    {
        var fixture = new Fixture();
        var dispatch = CreateDispatch(EmailDispatchStatus.RetryScheduled);
        dispatch.NextAttemptAt = DateTime.UtcNow.AddMinutes(15);
        fixture.Repository.GetByTenantAndPublishEventId(dispatch.TenantId, dispatch.PublishEventId, Arg.Any<CancellationToken>())
            .Returns(dispatch);

        EmailDispatchSingleDrainResult result = await fixture.Service.ProcessSingleAsync(
            dispatch.TenantId,
            dispatch.PublishEventId,
            "rabbit-consumer-1",
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(EmailDispatchDrainOutcome.Deferred);
        await fixture.EmailService.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
    }

    [Test]
    public async Task ProcessSingleAsyncReturnsTenantPausedBeforePreferenceLookupOrSend()
    {
        var fixture = new Fixture();
        var dispatch = CreateDispatch(EmailDispatchStatus.Pending);
        fixture.Repository.GetByTenantAndPublishEventId(dispatch.TenantId, dispatch.PublishEventId, Arg.Any<CancellationToken>())
            .Returns(dispatch);
        fixture.Repository.IsTenantPaused(dispatch.TenantId, Arg.Any<CancellationToken>()).Returns(true);

        EmailDispatchSingleDrainResult result = await fixture.Service.ProcessSingleAsync(
            dispatch.TenantId,
            dispatch.PublishEventId,
            "rabbit-consumer-1",
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(EmailDispatchDrainOutcome.TenantPaused);
        await fixture.Repository.DidNotReceiveWithAnyArgs().TryMarkAsProcessing(default, default, default, default);
        await fixture.EligibilityEvaluator.DidNotReceiveWithAnyArgs()
            .EvaluateAndBeginProviderHandoffAsync(default!, default);
        await fixture.EmailService.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
    }

    [Test]
    public async Task ProcessSingleAsyncUsesEligibilityRefreshedAddressAtProviderHandoff()
    {
        var fixture = new Fixture();
        var dispatch = CreateDispatch(EmailDispatchStatus.Pending);
        EmailMessage? sentMessage = null;
        fixture.Repository.GetByTenantAndPublishEventId(dispatch.TenantId, dispatch.PublishEventId, Arg.Any<CancellationToken>())
            .Returns(dispatch);
        fixture.Repository.IsTenantPaused(dispatch.TenantId, Arg.Any<CancellationToken>()).Returns(false);
        fixture.Repository.TryMarkAsProcessing(dispatch.Id, Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);
        fixture.EligibilityEvaluator.EvaluateAndBeginProviderHandoffAsync(
                Arg.Any<EmailDispatchEligibilityRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new EmailDispatchEligibilityResult(
                EmailDispatchEligibilityOutcome.Eligible,
                "current-verified@example.test",
                null));
        fixture.EmailService.SendAsync(Arg.Do<EmailMessage>(message => sentMessage = message), Arg.Any<CancellationToken>())
            .Returns(EmailResult.Ok("provider-message-1"));

        var result = await fixture.Service.ProcessSingleAsync(
            dispatch.TenantId,
            dispatch.PublishEventId,
            "tickerq-drain",
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(EmailDispatchDrainOutcome.Sent);
        await Assert.That(sentMessage).IsNotNull();
        await Assert.That(sentMessage!.To).IsEqualTo("current-verified@example.test");
        await fixture.EligibilityEvaluator.Received(1).EvaluateAndBeginProviderHandoffAsync(
            Arg.Is<EmailDispatchEligibilityRequest>(request =>
                request.TenantId == dispatch.TenantId &&
                request.OutboxId == dispatch.Id &&
                request.AttemptNumber == dispatch.AttemptCount + 1),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessSingleAsyncDoesNotSendWhenEligibilityAtomicallySkipsDelivery()
    {
        var fixture = new Fixture();
        var dispatch = CreateDispatch(EmailDispatchStatus.Pending);
        fixture.Repository.GetByTenantAndPublishEventId(dispatch.TenantId, dispatch.PublishEventId, Arg.Any<CancellationToken>())
            .Returns(dispatch);
        fixture.Repository.IsTenantPaused(dispatch.TenantId, Arg.Any<CancellationToken>()).Returns(false);
        fixture.Repository.TryMarkAsProcessing(dispatch.Id, Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);
        fixture.EligibilityEvaluator.EvaluateAndBeginProviderHandoffAsync(
                Arg.Any<EmailDispatchEligibilityRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new EmailDispatchEligibilityResult(
                EmailDispatchEligibilityOutcome.Skipped,
                null,
                "recipient_email_unverified"));

        var result = await fixture.Service.ProcessSingleAsync(
            dispatch.TenantId,
            dispatch.PublishEventId,
            "tickerq-drain",
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(EmailDispatchDrainOutcome.Skipped);
        await fixture.EmailService.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
        await fixture.Repository.DidNotReceiveWithAnyArgs().RecordAttempt(default!, default);
        await fixture.Repository.DidNotReceiveWithAnyArgs().MarkAsSkipped(default, default!, default!, default, default);
    }

    [Test]
    public async Task ProcessSingleAsyncSendsAndPersistsOutcomeForPendingRows()
    {
        const string providerResponseCanary = "provider-response-canary attendee@example.test body-canary";
        var fixture = new Fixture();
        var dispatch = CreateDispatch(EmailDispatchStatus.Pending);
        fixture.Repository.GetByTenantAndPublishEventId(dispatch.TenantId, dispatch.PublishEventId, Arg.Any<CancellationToken>())
            .Returns(dispatch);
        fixture.Repository.IsTenantPaused(dispatch.TenantId, Arg.Any<CancellationToken>()).Returns(false);
        fixture.Repository.TryMarkAsProcessing(dispatch.Id, Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);
        fixture.EmailService.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(EmailResult.Ok(providerResponseCanary));

        EmailDispatchSingleDrainResult result = await fixture.Service.ProcessSingleAsync(
            dispatch.TenantId,
            dispatch.PublishEventId,
            "rabbit-consumer-1",
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(EmailDispatchDrainOutcome.Sent);
        await fixture.EligibilityEvaluator.Received(1).EvaluateAndBeginProviderHandoffAsync(
            Arg.Is<EmailDispatchEligibilityRequest>(request =>
                request.TenantId == dispatch.TenantId &&
                request.OutboxId == dispatch.Id &&
                request.ConsumerId == "rabbit-consumer-1"),
            Arg.Any<CancellationToken>());
        await fixture.Repository.Received(1).SettleProviderAccepted(
            Arg.Is<EmailDispatchAcceptedSettlement>(settlement =>
                settlement.TenantId == dispatch.TenantId &&
                settlement.OutboxId == dispatch.Id &&
                settlement.AttemptNumber == dispatch.AttemptCount + 1 &&
                settlement.ProviderMessageId == null),
            Arg.Any<CancellationToken>());
        await Assert.That(fixture.Logger.Entries.All(entry =>
            !entry.Message.Contains(providerResponseCanary, StringComparison.Ordinal) &&
            entry.Exception?.Message.Contains(providerResponseCanary, StringComparison.Ordinal) != true)).IsTrue();
        fixture.TenantAccessor.Received(1).SetTenant(dispatch.TenantId);
        fixture.TenantAccessor.Received(1).Clear();
    }

    [Test]
    [Arguments("attempt")]
    [Arguments("receipt")]
    [Arguments("outbox")]
    [Arguments("channel-delivery")]
    public async Task ProcessSingleAsyncReconcilesUnknownWithoutResendWhenAcceptedSettlementFails(string failureStage)
    {
        const string canary = "provider-error-canary attendee@example.test body-canary";
        var fixture = new Fixture();
        var dispatch = CreateDispatch(EmailDispatchStatus.Pending);
        ConfigureAcceptedSend(fixture, dispatch);
        ConfigureAcceptedSettlementFailure(fixture, failureStage, canary);
        fixture.Repository.ReconcileProviderAccepted(
                Arg.Any<EmailDispatchAcceptedSettlement>(),
                Arg.Any<CancellationToken>())
            .Returns(EmailDispatchAcceptedReconciliationOutcome.Unknown);

        EmailDispatchSingleDrainResult result = await fixture.Service.ProcessSingleAsync(
            dispatch.TenantId,
            dispatch.PublishEventId,
            "tickerq-drain",
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(EmailDispatchDrainOutcome.Unknown);
        await fixture.EmailService.Received(1).SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        await fixture.Repository.Received(1).ReconcileProviderAccepted(
            Arg.Any<EmailDispatchAcceptedSettlement>(),
            Arg.Any<CancellationToken>());
        await fixture.Repository.DidNotReceiveWithAnyArgs().MarkAsFailed(
            default, default!, default!, default, default, default, default, default);
        await Assert.That(fixture.Logger.Entries.All(entry =>
            !entry.Message.Contains(canary, StringComparison.Ordinal) &&
            entry.Exception?.Message.Contains(canary, StringComparison.Ordinal) != true)).IsTrue();
    }

    [Test]
    public async Task ProcessSingleAsyncReconcilesUnknownAfterCrashImmediatelyFollowingProviderAcceptance()
    {
        var fixture = new Fixture();
        var dispatch = CreateDispatch(EmailDispatchStatus.Pending);
        ConfigureAcceptedSend(fixture, dispatch);
        ConfigureAcceptedSettlementFailure(fixture, "before-first-settlement-write", "simulated worker crash");
        fixture.Repository.ReconcileProviderAccepted(
                Arg.Any<EmailDispatchAcceptedSettlement>(),
                Arg.Any<CancellationToken>())
            .Returns(EmailDispatchAcceptedReconciliationOutcome.Unknown);

        EmailDispatchSingleDrainResult result = await fixture.Service.ProcessSingleAsync(
            dispatch.TenantId,
            dispatch.PublishEventId,
            "tickerq-drain",
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(EmailDispatchDrainOutcome.Unknown);
        await fixture.EmailService.Received(1).SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        await fixture.EligibilityEvaluator.Received(1).EvaluateAndBeginProviderHandoffAsync(
            Arg.Is<EmailDispatchEligibilityRequest>(request => request.OutboxId == dispatch.Id),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessSingleAsyncTreatsUnknownCommitAsSentWhenFreshReconciliationFindsAlignedLedgers()
    {
        var fixture = new Fixture();
        var dispatch = CreateDispatch(EmailDispatchStatus.Pending);
        ConfigureAcceptedSend(fixture, dispatch);
        ConfigureAcceptedSettlementFailure(fixture, "commit", "commit outcome canary");
        fixture.Repository.ReconcileProviderAccepted(
                Arg.Any<EmailDispatchAcceptedSettlement>(),
                Arg.Any<CancellationToken>())
            .Returns(EmailDispatchAcceptedReconciliationOutcome.Sent);

        EmailDispatchSingleDrainResult result = await fixture.Service.ProcessSingleAsync(
            dispatch.TenantId,
            dispatch.PublishEventId,
            "tickerq-drain",
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(EmailDispatchDrainOutcome.Sent);
        await fixture.EmailService.Received(1).SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        await fixture.Repository.DidNotReceiveWithAnyArgs().MarkAsFailed(
            default, default!, default!, default, default, default, default, default);
    }

    [Test]
    public async Task ProcessSingleAsyncAddsUnsubscribeHeadersAndFooterWhenPublicBaseUrlIsConfigured()
    {
        var fixture = new Fixture();
        var dispatch = CreateDispatch(EmailDispatchStatus.Pending);
        dispatch.Kind = EmailDispatchKind.EventReminder;
        EmailMessage? sentMessage = null;
        fixture.Repository.GetByTenantAndPublishEventId(dispatch.TenantId, dispatch.PublishEventId, Arg.Any<CancellationToken>())
            .Returns(dispatch);
        fixture.Repository.IsTenantPaused(dispatch.TenantId, Arg.Any<CancellationToken>()).Returns(false);
        fixture.Repository.TryMarkAsProcessing(dispatch.Id, Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);
        fixture.UnsubscribeTokenService.GenerateToken(
                Arg.Is<EmailUnsubscribeTokenPayload>(payload =>
                    payload.TenantId == dispatch.TenantId &&
                    payload.UserId == dispatch.RecipientUserId &&
                    payload.Category == NotificationPreferenceCategories.EventReminders),
                Arg.Any<TimeSpan?>())
            .Returns("token+value");
        fixture.EmailService.SendAsync(Arg.Do<EmailMessage>(message => sentMessage = message), Arg.Any<CancellationToken>())
            .Returns(EmailResult.Ok("provider-message-1"));

        var result = await fixture.Service.ProcessSingleAsync(
            dispatch.TenantId,
            dispatch.PublishEventId,
            "tickerq-drain",
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(EmailDispatchDrainOutcome.Sent);
        await Assert.That(sentMessage).IsNotNull();
        var unsubscribeUrl = "https://events.example.test/api/email/unsubscribe?token=token%2Bvalue";
        await Assert.That(sentMessage!.CustomHeaders["List-Unsubscribe"]).IsEqualTo($"<{unsubscribeUrl}>");
        await Assert.That(sentMessage.CustomHeaders["List-Unsubscribe-Post"]).IsEqualTo("List-Unsubscribe=One-Click");
        await Assert.That(sentMessage.PlainTextBody).Contains(unsubscribeUrl);
        await Assert.That(sentMessage.HtmlBody).Contains("unsubscribe");
        await Assert.That(sentMessage.HtmlBody).Contains(unsubscribeUrl);
    }

    [Test]
    public async Task ProcessSingleAsyncUsesEventUpdatesUnsubscribeCategoryForRegistrationCancellationKinds()
    {
        foreach (var kind in new[] { EmailDispatchKind.RegistrationCancelled, EmailDispatchKind.RegistrationRevoked })
        {
            var fixture = new Fixture();
            var dispatch = CreateDispatch(EmailDispatchStatus.Pending);
            dispatch.Kind = kind;
            fixture.Repository.GetByTenantAndPublishEventId(dispatch.TenantId, dispatch.PublishEventId, Arg.Any<CancellationToken>())
                .Returns(dispatch);
            fixture.Repository.IsTenantPaused(dispatch.TenantId, Arg.Any<CancellationToken>()).Returns(false);
            fixture.Repository.TryMarkAsProcessing(dispatch.Id, Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                .Returns(true);
            fixture.UnsubscribeTokenService.GenerateToken(
                    Arg.Is<EmailUnsubscribeTokenPayload>(payload =>
                        payload.TenantId == dispatch.TenantId &&
                        payload.UserId == dispatch.RecipientUserId &&
                        payload.Category == NotificationPreferenceCategories.EventUpdates),
                    Arg.Any<TimeSpan?>())
                .Returns("event-updates-token");
            fixture.EmailService.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
                .Returns(EmailResult.Ok($"provider-{kind}"));

            var result = await fixture.Service.ProcessSingleAsync(
                dispatch.TenantId,
                dispatch.PublishEventId,
                "tickerq-drain",
                CancellationToken.None);

            await Assert.That(result.Outcome).IsEqualTo(EmailDispatchDrainOutcome.Sent);
            fixture.UnsubscribeTokenService.Received(1).GenerateToken(
                Arg.Is<EmailUnsubscribeTokenPayload>(payload => payload.Category == NotificationPreferenceCategories.EventUpdates),
                Arg.Any<TimeSpan?>());
        }
    }

    [Test]
    public async Task ProcessSingleAsyncSkipsWithoutSendingWhenRecipientOptedOut()
    {
        var fixture = new Fixture();
        var dispatch = CreateDispatch(EmailDispatchStatus.Pending);
        dispatch.Kind = EmailDispatchKind.EventReminder;
        fixture.Repository.GetByTenantAndPublishEventId(dispatch.TenantId, dispatch.PublishEventId, Arg.Any<CancellationToken>())
            .Returns(dispatch);
        fixture.Repository.IsTenantPaused(dispatch.TenantId, Arg.Any<CancellationToken>()).Returns(false);
        fixture.Repository.TryMarkAsProcessing(dispatch.Id, Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);
        fixture.EligibilityEvaluator.EvaluateAndBeginProviderHandoffAsync(
                Arg.Any<EmailDispatchEligibilityRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new EmailDispatchEligibilityResult(
                EmailDispatchEligibilityOutcome.Skipped,
                null,
                "recipient_unsubscribed"));

        var result = await fixture.Service.ProcessSingleAsync(
            dispatch.TenantId,
            dispatch.PublishEventId,
            "tickerq-drain",
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(EmailDispatchDrainOutcome.Skipped);
        await fixture.EmailService.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
        await fixture.EligibilityEvaluator.Received(1).EvaluateAndBeginProviderHandoffAsync(
            Arg.Is<EmailDispatchEligibilityRequest>(request => request.OutboxId == dispatch.Id),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessSingleAsyncSkipsWithoutSendingWhenMatrixDisablesEmailChannel()
    {
        var fixture = new Fixture();
        var dispatch = CreateDispatch(EmailDispatchStatus.Pending);
        dispatch.Kind = EmailDispatchKind.EventReminder;
        fixture.Repository.GetByTenantAndPublishEventId(dispatch.TenantId, dispatch.PublishEventId, Arg.Any<CancellationToken>())
            .Returns(dispatch);
        fixture.Repository.IsTenantPaused(dispatch.TenantId, Arg.Any<CancellationToken>()).Returns(false);
        fixture.Repository.TryMarkAsProcessing(dispatch.Id, Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);
        fixture.EligibilityEvaluator.EvaluateAndBeginProviderHandoffAsync(
                Arg.Any<EmailDispatchEligibilityRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new EmailDispatchEligibilityResult(
                EmailDispatchEligibilityOutcome.Skipped,
                null,
                "recipient_notification_preference_disabled"));

        var result = await fixture.Service.ProcessSingleAsync(
            dispatch.TenantId,
            dispatch.PublishEventId,
            "tickerq-drain",
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(EmailDispatchDrainOutcome.Skipped);
        await fixture.EmailService.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
        await fixture.EligibilityEvaluator.Received(1).EvaluateAndBeginProviderHandoffAsync(
            Arg.Is<EmailDispatchEligibilityRequest>(request => request.OutboxId == dispatch.Id),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessSingleAsyncDelegatesEveryProductLifecycleEmailKindToEligibilityEvaluator()
    {
        var cases = new[]
        {
            EmailDispatchKind.RegistrationConfirmation,
            EmailDispatchKind.RegistrationApproved,
            EmailDispatchKind.RegistrationRejected,
            EmailDispatchKind.WaitlistPromoted,
            EmailDispatchKind.RegistrationCancelled,
            EmailDispatchKind.RegistrationRevoked,
            EmailDispatchKind.EventReminder,
            EmailDispatchKind.EventCancelled,
            EmailDispatchKind.OrganizerNotification
        };

        foreach (var kind in cases)
        {
            var fixture = new Fixture();
            var dispatch = CreateDispatch(EmailDispatchStatus.Pending);
            dispatch.Kind = kind;
            fixture.Repository.GetByTenantAndPublishEventId(dispatch.TenantId, dispatch.PublishEventId, Arg.Any<CancellationToken>())
                .Returns(dispatch);
            fixture.Repository.IsTenantPaused(dispatch.TenantId, Arg.Any<CancellationToken>()).Returns(false);
            fixture.Repository.TryMarkAsProcessing(dispatch.Id, Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                .Returns(true);
            fixture.EligibilityEvaluator.EvaluateAndBeginProviderHandoffAsync(
                    Arg.Any<EmailDispatchEligibilityRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(new EmailDispatchEligibilityResult(
                    EmailDispatchEligibilityOutcome.Skipped,
                    null,
                    "recipient_notification_preference_disabled"));

            var result = await fixture.Service.ProcessSingleAsync(
                dispatch.TenantId,
                dispatch.PublishEventId,
                "tickerq-drain",
                CancellationToken.None);

            await Assert.That(result.Outcome).IsEqualTo(EmailDispatchDrainOutcome.Skipped);
            await fixture.EligibilityEvaluator.Received(1).EvaluateAndBeginProviderHandoffAsync(
                Arg.Is<EmailDispatchEligibilityRequest>(request => request.OutboxId == dispatch.Id),
                Arg.Any<CancellationToken>());
            await fixture.EmailService.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
        }
    }

    [Test]
    public async Task ProcessSingleAsyncPersistsExpectedSmtpFailureWithoutThrowing()
    {
        var fixture = new Fixture();
        var dispatch = CreateDispatch(EmailDispatchStatus.Pending);
        fixture.Repository.GetByTenantAndPublishEventId(dispatch.TenantId, dispatch.PublishEventId, Arg.Any<CancellationToken>())
            .Returns(dispatch);
        fixture.Repository.IsTenantPaused(dispatch.TenantId, Arg.Any<CancellationToken>()).Returns(false);
        fixture.Repository.TryMarkAsProcessing(dispatch.Id, Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);
        fixture.EmailService.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(EmailResult.Fail("Mailbox unavailable"));

        EmailDispatchSingleDrainResult result = await fixture.Service.ProcessSingleAsync(
            dispatch.TenantId,
            dispatch.PublishEventId,
            "tickerq-drain",
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(EmailDispatchDrainOutcome.RetryScheduled);
        await fixture.Repository.Received(1).RecordAttempt(
            Arg.Is<EmailDispatchAttempt>(attempt =>
                attempt.EmailDispatchOutboxId == dispatch.Id &&
                attempt.Outcome == EmailDispatchAttemptOutcome.Failed &&
                attempt.FailureCategory == "smtp_send_failed"),
            Arg.Any<CancellationToken>());
        await fixture.Repository.Received(1).MarkAsFailed(
            dispatch.Id,
            "smtp_send_failed",
            "SMTP send failed before provider acceptance was confirmed.",
            true,
            Arg.Any<TimeSpan>(),
            Arg.Any<int>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await fixture.Repository.Received(1).MarkReceiptFailed(
            Arg.Any<Guid>(),
            "smtp_retry_scheduled",
            "SMTP send failed before provider acceptance was confirmed.",
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        fixture.TenantAccessor.Received(1).Clear();
    }

    [Test]
    public async Task ProcessSingleAsyncDoesNotPersistOrLogProviderErrorCanaries()
    {
        const string canary = "provider-error-canary attendee@example.test body-canary";
        var fixture = new Fixture();
        var dispatch = CreateDispatch(EmailDispatchStatus.Pending);
        fixture.Repository.GetByTenantAndPublishEventId(
                dispatch.TenantId,
                dispatch.PublishEventId,
                Arg.Any<CancellationToken>())
            .Returns(dispatch);
        fixture.Repository.IsTenantPaused(dispatch.TenantId, Arg.Any<CancellationToken>()).Returns(false);
        fixture.Repository.TryMarkAsProcessing(
                dispatch.Id,
                Arg.Any<Guid>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        fixture.EmailService.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(EmailResult.Fail(canary));

        EmailDispatchSingleDrainResult result = await fixture.Service.ProcessSingleAsync(
            dispatch.TenantId,
            dispatch.PublishEventId,
            "tickerq-drain",
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(EmailDispatchDrainOutcome.RetryScheduled);
        await fixture.Repository.Received(1).RecordAttempt(
            Arg.Is<EmailDispatchAttempt>(attempt =>
                attempt.Outcome == EmailDispatchAttemptOutcome.Failed &&
                attempt.SanitizedErrorMessage == "SMTP send failed before provider acceptance was confirmed." &&
                !attempt.SanitizedErrorMessage.Contains(canary, StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
        await fixture.Repository.Received(1).MarkAsFailed(
            dispatch.Id,
            "smtp_send_failed",
            Arg.Is<string>(message => !message.Contains(canary, StringComparison.Ordinal)),
            true,
            Arg.Any<TimeSpan>(),
            Arg.Any<int>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await Assert.That(fixture.Logger.Entries.All(entry =>
            !entry.Message.Contains(canary, StringComparison.Ordinal) &&
            entry.Exception?.Message.Contains(canary, StringComparison.Ordinal) != true)).IsTrue();
    }

    [Test]
    public async Task ProcessSingleAsyncDeadLettersWhenRetryBudgetIsExhausted()
    {
        var fixture = new Fixture(new EmailDispatchProcessorSettings
        {
            MaxAttemptCount = 3
        });
        var dispatch = CreateDispatch(EmailDispatchStatus.Pending);
        dispatch.AttemptCount = 2;
        dispatch.MaxAttempts = 3;
        fixture.Repository.GetByTenantAndPublishEventId(dispatch.TenantId, dispatch.PublishEventId, Arg.Any<CancellationToken>())
            .Returns(dispatch);
        fixture.Repository.IsTenantPaused(dispatch.TenantId, Arg.Any<CancellationToken>()).Returns(false);
        fixture.Repository.TryMarkAsProcessing(dispatch.Id, Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);
        fixture.EmailService.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(EmailResult.Fail("Mailbox unavailable"));

        EmailDispatchSingleDrainResult result = await fixture.Service.ProcessSingleAsync(
            dispatch.TenantId,
            dispatch.PublishEventId,
            "tickerq-drain",
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(EmailDispatchDrainOutcome.DeadLettered);
        await fixture.Repository.Received(1).RecordAttempt(
            Arg.Is<EmailDispatchAttempt>(attempt =>
                attempt.EmailDispatchOutboxId == dispatch.Id &&
                attempt.AttemptNumber == 3 &&
                attempt.Outcome == EmailDispatchAttemptOutcome.Failed &&
                attempt.FailureCategory == "smtp_send_failed"),
            Arg.Any<CancellationToken>());
        await fixture.Repository.Received(1).MarkAsFailed(
            dispatch.Id,
            "smtp_send_failed",
            "SMTP send failed before provider acceptance was confirmed.",
            true,
            Arg.Any<TimeSpan>(),
            3,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessSingleAsyncMarksTimeoutLikeFailureUnknown()
    {
        var fixture = new Fixture();
        var dispatch = CreateDispatch(EmailDispatchStatus.Pending);
        fixture.Repository.GetByTenantAndPublishEventId(dispatch.TenantId, dispatch.PublishEventId, Arg.Any<CancellationToken>())
            .Returns(dispatch);
        fixture.Repository.IsTenantPaused(dispatch.TenantId, Arg.Any<CancellationToken>()).Returns(false);
        fixture.Repository.TryMarkAsProcessing(dispatch.Id, Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);
        fixture.EmailService.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(EmailResult.Fail("SMTP timeout while waiting for provider acknowledgement."));

        EmailDispatchSingleDrainResult result = await fixture.Service.ProcessSingleAsync(
            dispatch.TenantId,
            dispatch.PublishEventId,
            "tickerq-drain",
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(EmailDispatchDrainOutcome.Unknown);
        await fixture.Repository.Received(1).RecordAttempt(
            Arg.Is<EmailDispatchAttempt>(attempt =>
                attempt.EmailDispatchOutboxId == dispatch.Id &&
                attempt.Outcome == EmailDispatchAttemptOutcome.Unknown &&
                attempt.FailureCategory == "smtp_outcome_unknown" &&
                attempt.SanitizedErrorMessage == "SMTP provider acceptance is uncertain. Automatic resend is disabled pending reconciliation."),
            Arg.Any<CancellationToken>());
        await fixture.Repository.Received(1).ReconcileProviderAccepted(
            Arg.Is<EmailDispatchAcceptedSettlement>(settlement =>
                settlement.TenantId == dispatch.TenantId &&
                settlement.OutboxId == dispatch.Id &&
                settlement.AttemptNumber == dispatch.AttemptCount + 1 &&
                settlement.ProviderMessageId == null),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessSingleAsyncBubblesUnexpectedRepositoryFailuresToScheduler()
    {
        var fixture = new Fixture();
        var dispatch = CreateDispatch(EmailDispatchStatus.Pending);
        fixture.Repository.GetByTenantAndPublishEventId(dispatch.TenantId, dispatch.PublishEventId, Arg.Any<CancellationToken>())
            .Returns(dispatch);
        fixture.Repository.IsTenantPaused(dispatch.TenantId, Arg.Any<CancellationToken>()).Returns(false);
        fixture.Repository.TryMarkAsProcessing(dispatch.Id, Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns<Task<bool>>(_ => throw new InvalidOperationException("database unavailable"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Service.ProcessSingleAsync(
                dispatch.TenantId,
                dispatch.PublishEventId,
                "tickerq-drain",
                CancellationToken.None));

        await Assert.That(exception.Message).Contains("database unavailable");
        await fixture.EmailService.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
    }

    [Test]
    public async Task RecoverStaleProcessingAsyncMarksExpiredLeasesUnknown()
    {
        var fixture = new Fixture(new EmailDispatchProcessorSettings
        {
            BatchSize = 25,
            ProcessingLeaseTimeoutSeconds = 120
        });
        DateTime? cutoff = null;
        fixture.Repository.MarkStaleProcessingAsUnknown(
                Arg.Do<DateTime>(value => cutoff = value),
                Arg.Any<DateTime>(),
                "processing_lease_expired",
                Arg.Any<string>(),
                25,
                Arg.Any<CancellationToken>())
            .Returns(2);

        var result = await fixture.Service.RecoverStaleProcessingAsync(CancellationToken.None);

        await Assert.That(result.RecoveredCount).IsEqualTo(2);
        await Assert.That(cutoff).IsNotNull();
        await Assert.That(Math.Abs((result.ProcessingStartedBefore - cutoff!.Value).TotalMilliseconds)).IsLessThan(5);
        await fixture.Repository.Received(1).MarkStaleProcessingAsUnknown(
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            "processing_lease_expired",
            Arg.Is<string>(message => message.Contains("requires operator review", StringComparison.OrdinalIgnoreCase)),
            25,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RecoverStaleProcessingAsyncAlignsFencedAttemptReceiptAndOutboxWithoutSending()
    {
        var startedAt = DateTime.UtcNow.AddMinutes(-10);
        var dispatch = CreateDispatch(EmailDispatchStatus.Processing);
        dispatch.AttemptCount = 2;
        dispatch.ProcessingStartedAt = startedAt;
        dispatch.ProcessingLeaseToken = Guid.CreateVersion7();
        var repository = new InMemoryEmailDispatchOutboxRepository(dispatch);
        repository.Attempts.Add(new EmailDispatchAttempt
        {
            Id = Guid.CreateVersion7(),
            TenantId = dispatch.TenantId,
            EmailDispatchOutboxId = dispatch.Id,
            AttemptNumber = 1,
            Outcome = EmailDispatchAttemptOutcome.Failed,
            CompletedAt = startedAt.AddMinutes(-5),
            StartedAt = startedAt,
            FailureCategory = "previous_attempt_failed",
            SanitizedErrorMessage = "Previous attempt failed before provider handoff."
        });
        repository.Attempts.Add(new EmailDispatchAttempt
        {
            Id = Guid.CreateVersion7(),
            TenantId = dispatch.TenantId,
            EmailDispatchOutboxId = dispatch.Id,
            AttemptNumber = 2,
            Outcome = EmailDispatchAttemptOutcome.Unknown,
            StartedAt = startedAt,
            FailureCategory = "provider_handoff_started",
            SanitizedErrorMessage = "Provider handoff started."
        });
        repository.Receipts.Add(new EmailDispatchReceipt
        {
            Id = Guid.CreateVersion7(),
            TenantId = dispatch.TenantId,
            PublishEventId = dispatch.PublishEventId,
            EmailDispatchOutboxId = dispatch.Id,
            Status = EmailDispatchReceiptStatus.Processing,
            FirstSeenAt = startedAt,
            ProcessingStartedAt = startedAt
        });
        var fixture = new Fixture(new EmailDispatchProcessorSettings
        {
            ProcessingLeaseTimeoutSeconds = 60
        }, repository);

        EmailDispatchRecoveryResult result = await fixture.Service.RecoverStaleProcessingAsync(CancellationToken.None);

        await Assert.That(result.RecoveredCount).IsEqualTo(1);
        await Assert.That(repository.Dispatch.Status).IsEqualTo(EmailDispatchStatus.Unknown);
        await Assert.That(repository.Dispatch.NextAttemptAt).IsNull();
        await Assert.That(repository.Attempts).Count().IsEqualTo(2);
        await Assert.That(repository.Attempts[0].Outcome).IsEqualTo(EmailDispatchAttemptOutcome.Failed);
        await Assert.That(repository.Attempts[0].FailureCategory).IsEqualTo("previous_attempt_failed");
        await Assert.That(repository.Attempts[1].Outcome).IsEqualTo(EmailDispatchAttemptOutcome.Unknown);
        await Assert.That(repository.Attempts[1].CompletedAt).IsNotNull();
        await Assert.That(repository.Receipts).Count().IsEqualTo(1);
        await Assert.That(repository.Receipts[0].Status).IsEqualTo(EmailDispatchReceiptStatus.Unknown);
        await fixture.EmailService.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
    }

    private static EmailDispatchOutbox CreateDispatch(EmailDispatchStatus status)
    {
        return new EmailDispatchOutbox
        {
            Id = Guid.CreateVersion7(),
            TenantId = Guid.CreateVersion7(),
            PublishEventId = Guid.CreateVersion7(),
            RecipientUserId = Guid.CreateVersion7(),
            Kind = EmailDispatchKind.RegistrationConfirmation,
            SourceType = "event-registration",
            SourceId = Guid.CreateVersion7(),
            RecipientEmail = "attendee@example.test",
            Subject = "Registration confirmation",
            PlainTextBody = "Registration confirmed.",
            Status = status
        };
    }

    private static void ConfigureAcceptedSend(Fixture fixture, EmailDispatchOutbox dispatch)
    {
        fixture.Repository.GetByTenantAndPublishEventId(
                dispatch.TenantId,
                dispatch.PublishEventId,
                Arg.Any<CancellationToken>())
            .Returns(dispatch);
        fixture.Repository.IsTenantPaused(dispatch.TenantId, Arg.Any<CancellationToken>()).Returns(false);
        fixture.Repository.TryMarkAsProcessing(
                dispatch.Id,
                Arg.Any<Guid>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        fixture.EmailService.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(EmailResult.Ok("provider-message-1"));
    }

    private static void ConfigureAcceptedSettlementFailure(Fixture fixture, string failureStage, string canary)
    {
        fixture.Repository.SettleProviderAccepted(
                Arg.Any<EmailDispatchAcceptedSettlement>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException($"{failureStage}: {canary}"));
    }

    private sealed class Fixture
    {
        public Fixture(
            EmailDispatchProcessorSettings? settings = null,
            IEmailDispatchOutboxRepository? repository = null)
        {
            Repository = repository ?? Substitute.For<IEmailDispatchOutboxRepository>();
            EmailService = Substitute.For<IEmailService>();
            EligibilityEvaluator = Substitute.For<IEmailDispatchEligibilityEvaluator>();
            UnsubscribeTokenService = Substitute.For<IEmailUnsubscribeTokenService>();
            TenantAccessor = Substitute.For<ITenantContextAccessor>();
            Configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PublicBaseUrl"] = "https://events.example.test/"
                })
                .Build();
            UnsubscribeTokenService.GenerateToken(
                    Arg.Any<EmailUnsubscribeTokenPayload>(),
                    Arg.Any<TimeSpan?>())
                .Returns("unsubscribe-token");
            EligibilityEvaluator.EvaluateAndBeginProviderHandoffAsync(
                    Arg.Any<EmailDispatchEligibilityRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    var request = call.Arg<EmailDispatchEligibilityRequest>();
                    return new EmailDispatchEligibilityResult(
                        EmailDispatchEligibilityOutcome.Eligible,
                        repository is InMemoryEmailDispatchOutboxRepository inMemory
                            ? inMemory.Dispatch.RecipientEmail
                            : "attendee@example.test",
                        null,
                        Guid.CreateVersion7());
                });

            var services = new ServiceCollection();
            services.AddSingleton(Repository);
            services.AddSingleton(EmailService);
            services.AddSingleton(EligibilityEvaluator);
            services.AddSingleton(UnsubscribeTokenService);
            services.AddSingleton(TenantAccessor);
            services.AddSingleton<IConfiguration>(Configuration);
            ServiceProvider = services.BuildServiceProvider();

            var meterFactory = Substitute.For<IMeterFactory>();
            meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter(BusinessMetrics.MeterName));
            Logger = new TestListLogger<EmailDispatchDrainService>();

            Service = new EmailDispatchDrainService(
                ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
                Options.Create(settings ?? new EmailDispatchProcessorSettings()),
                new BusinessMetrics(meterFactory),
                Logger);
        }

        public IEmailDispatchOutboxRepository Repository { get; }

        public IEmailService EmailService { get; }

        public IEmailDispatchEligibilityEvaluator EligibilityEvaluator { get; }

        public IEmailUnsubscribeTokenService UnsubscribeTokenService { get; }

        public ITenantContextAccessor TenantAccessor { get; }

        public IConfiguration Configuration { get; }

        public TestListLogger<EmailDispatchDrainService> Logger { get; }

        public ServiceProvider ServiceProvider { get; }

        public EmailDispatchDrainService Service { get; }
    }
}
