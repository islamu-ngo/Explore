// ABOUTME: Unit tests for broker-neutral EmailDispatch single-row drainage.
// ABOUTME: Verifies RabbitMQ consumers can reuse SMTP state transitions without owning delivery logic.

using System.Diagnostics.Metrics;
using Explore.Application.Contracts.Infrastructure;
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
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Infrastructure;

[Category(InfrastructureTestCategories.Email)]
public sealed class EmailDispatchDrainServiceTests
{
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
        await fixture.PreferenceRepository.DidNotReceiveWithAnyArgs().GetByUserAndCategory(default, default, default!);
        await fixture.EmailService.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
    }

    [Test]
    public async Task ProcessSingleAsyncSendsAndPersistsOutcomeForPendingRows()
    {
        var fixture = new Fixture();
        var dispatch = CreateDispatch(EmailDispatchStatus.Pending);
        EmailDispatchReceipt? claimedReceipt = null;
        fixture.Repository.GetByTenantAndPublishEventId(dispatch.TenantId, dispatch.PublishEventId, Arg.Any<CancellationToken>())
            .Returns(dispatch);
        fixture.Repository.IsTenantPaused(dispatch.TenantId, Arg.Any<CancellationToken>()).Returns(false);
        fixture.Repository.TryMarkAsProcessing(dispatch.Id, Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);
        fixture.Repository.TryClaimReceipt(Arg.Do<EmailDispatchReceipt>(receipt => claimedReceipt = receipt), Arg.Any<CancellationToken>())
            .Returns(true);
        fixture.PreferenceRepository.GetByUserAndCategory(dispatch.TenantId, dispatch.UserId!.Value, Arg.Any<string>())
            .Returns((UserNotificationPreference?)null);
        fixture.EmailService.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(EmailResult.Ok("provider-message-1"));

        EmailDispatchSingleDrainResult result = await fixture.Service.ProcessSingleAsync(
            dispatch.TenantId,
            dispatch.PublishEventId,
            "rabbit-consumer-1",
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(EmailDispatchDrainOutcome.Sent);
        await Assert.That(claimedReceipt).IsNotNull();
        await Assert.That(claimedReceipt!.ConsumerId).IsEqualTo("rabbit-consumer-1");
        await fixture.Repository.Received(1).RecordAttempt(Arg.Any<EmailDispatchAttempt>(), Arg.Any<CancellationToken>());
        await fixture.Repository.Received(1).MarkAsSent(dispatch.Id, Arg.Any<DateTime>(), "provider-message-1", Arg.Any<CancellationToken>());
        await fixture.Repository.Received(1).MarkReceiptCompleted(Arg.Any<Guid>(), Arg.Any<DateTime>(), "provider-message-1", Arg.Any<CancellationToken>());
        fixture.TenantAccessor.Received(1).SetTenant(dispatch.TenantId);
        fixture.TenantAccessor.Received(1).Clear();
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
        fixture.Repository.TryClaimReceipt(Arg.Any<EmailDispatchReceipt>(), Arg.Any<CancellationToken>())
            .Returns(true);
        fixture.PreferenceRepository.GetByUserAndCategory(
                dispatch.TenantId,
                dispatch.UserId!.Value,
                NotificationPreferenceCategories.EventReminders)
            .Returns((UserNotificationPreference?)null);
        fixture.UnsubscribeTokenService.GenerateToken(
                Arg.Is<EmailUnsubscribeTokenPayload>(payload =>
                    payload.TenantId == dispatch.TenantId &&
                    payload.UserId == dispatch.UserId &&
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
        fixture.Repository.TryClaimReceipt(Arg.Any<EmailDispatchReceipt>(), Arg.Any<CancellationToken>())
            .Returns(true);
        fixture.PreferenceRepository.GetByUserAndCategory(
                dispatch.TenantId,
                dispatch.UserId!.Value,
                NotificationPreferenceCategories.EventReminders)
            .Returns(new UserNotificationPreference
            {
                TenantId = dispatch.TenantId,
                Tenant = null!,
                UserId = dispatch.UserId.Value,
                Category = NotificationPreferenceCategories.EventReminders,
                IsEnabled = false
            });

        var result = await fixture.Service.ProcessSingleAsync(
            dispatch.TenantId,
            dispatch.PublishEventId,
            "tickerq-drain",
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(EmailDispatchDrainOutcome.Skipped);
        await fixture.EmailService.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
        await fixture.Repository.Received(1).RecordAttempt(
            Arg.Is<EmailDispatchAttempt>(attempt =>
                attempt.EmailDispatchOutboxId == dispatch.Id &&
                attempt.Outcome == EmailDispatchAttemptOutcome.Skipped &&
                attempt.FailureCategory == "recipient_unsubscribed"),
            Arg.Any<CancellationToken>());
        await fixture.Repository.Received(1).MarkAsSkipped(
            dispatch.Id,
            "recipient_unsubscribed",
            Arg.Is<string>(message => message.Contains("skipped before provider handoff", StringComparison.OrdinalIgnoreCase)),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await fixture.Repository.Received(1).MarkReceiptSkipped(
            Arg.Any<Guid>(),
            "recipient_unsubscribed",
            Arg.Is<string>(message => message.Contains("skipped before provider handoff", StringComparison.OrdinalIgnoreCase)),
            Arg.Any<DateTime>(),
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
        fixture.Repository.TryClaimReceipt(Arg.Any<EmailDispatchReceipt>(), Arg.Any<CancellationToken>())
            .Returns(true);
        fixture.PreferenceRepository.GetByUserAndCategory(
                dispatch.TenantId,
                dispatch.UserId!.Value,
                NotificationPreferenceCategories.EventReminders)
            .Returns((UserNotificationPreference?)null);
        fixture.PreferenceResolver.ResolveAsync(Arg.Any<NotificationPreferenceResolveRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => DisabledDecision(call.Arg<NotificationPreferenceResolveRequest>()));

        var result = await fixture.Service.ProcessSingleAsync(
            dispatch.TenantId,
            dispatch.PublishEventId,
            "tickerq-drain",
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(EmailDispatchDrainOutcome.Skipped);
        await fixture.EmailService.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
        await fixture.Repository.Received(1).RecordAttempt(
            Arg.Is<EmailDispatchAttempt>(attempt =>
                attempt.EmailDispatchOutboxId == dispatch.Id &&
                attempt.Outcome == EmailDispatchAttemptOutcome.Skipped &&
                attempt.FailureCategory == "recipient_notification_preference_disabled"),
            Arg.Any<CancellationToken>());
        await fixture.Repository.Received(1).MarkAsSkipped(
            dispatch.Id,
            "recipient_notification_preference_disabled",
            Arg.Is<string>(message => message.Contains("disabled this notification category", StringComparison.OrdinalIgnoreCase)),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await fixture.Repository.Received(1).MarkReceiptSkipped(
            Arg.Any<Guid>(),
            "recipient_notification_preference_disabled",
            Arg.Is<string>(message => message.Contains("disabled this notification category", StringComparison.OrdinalIgnoreCase)),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await fixture.PreferenceResolver.Received(1).ResolveAsync(
            Arg.Is<NotificationPreferenceResolveRequest>(request =>
                request.TenantId == dispatch.TenantId &&
                request.UserId == dispatch.UserId &&
                request.CategoryCode == NotificationPreferenceCategoryCodes.EventUpdates &&
                request.ChannelCode == NotificationPreferenceChannelCodes.Email),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessSingleAsyncAppliesPreferencesForEveryProductLifecycleEmailKind()
    {
        var cases = new[]
        {
            (EmailDispatchKind.RegistrationConfirmation, NotificationPreferenceCategories.RegistrationConfirmations),
            (EmailDispatchKind.RegistrationApproved, NotificationPreferenceCategories.EventUpdates),
            (EmailDispatchKind.RegistrationRejected, NotificationPreferenceCategories.EventUpdates),
            (EmailDispatchKind.WaitlistPromoted, NotificationPreferenceCategories.EventUpdates),
            (EmailDispatchKind.EventReminder, NotificationPreferenceCategories.EventReminders),
            (EmailDispatchKind.EventCancelled, NotificationPreferenceCategories.EventUpdates),
            (EmailDispatchKind.OrganizerNotification, NotificationPreferenceCategories.OrganizerAnnouncements)
        };

        foreach (var (kind, category) in cases)
        {
            var fixture = new Fixture();
            var dispatch = CreateDispatch(EmailDispatchStatus.Pending);
            dispatch.Kind = kind;
            fixture.Repository.GetByTenantAndPublishEventId(dispatch.TenantId, dispatch.PublishEventId, Arg.Any<CancellationToken>())
                .Returns(dispatch);
            fixture.Repository.IsTenantPaused(dispatch.TenantId, Arg.Any<CancellationToken>()).Returns(false);
            fixture.Repository.TryMarkAsProcessing(dispatch.Id, Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                .Returns(true);
            fixture.Repository.TryClaimReceipt(Arg.Any<EmailDispatchReceipt>(), Arg.Any<CancellationToken>())
                .Returns(true);
            fixture.PreferenceRepository.GetByUserAndCategory(
                    dispatch.TenantId,
                    dispatch.UserId!.Value,
                    category)
                .Returns(new UserNotificationPreference
                {
                    TenantId = dispatch.TenantId,
                    Tenant = null!,
                    UserId = dispatch.UserId.Value,
                    Category = category,
                    IsEnabled = false
                });

            var result = await fixture.Service.ProcessSingleAsync(
                dispatch.TenantId,
                dispatch.PublishEventId,
                "tickerq-drain",
                CancellationToken.None);

            await Assert.That(result.Outcome).IsEqualTo(EmailDispatchDrainOutcome.Skipped);
            await fixture.PreferenceRepository.Received(1).GetByUserAndCategory(
                dispatch.TenantId,
                dispatch.UserId.Value,
                category);
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
        fixture.Repository.TryClaimReceipt(Arg.Any<EmailDispatchReceipt>(), Arg.Any<CancellationToken>())
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
            "Mailbox unavailable",
            true,
            Arg.Any<TimeSpan>(),
            Arg.Any<int>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await fixture.Repository.Received(1).MarkReceiptFailed(
            Arg.Any<Guid>(),
            "smtp_retry_scheduled",
            "Mailbox unavailable",
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        fixture.TenantAccessor.Received(1).Clear();
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
        fixture.Repository.TryClaimReceipt(Arg.Any<EmailDispatchReceipt>(), Arg.Any<CancellationToken>())
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
            "Mailbox unavailable",
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
        fixture.Repository.TryClaimReceipt(Arg.Any<EmailDispatchReceipt>(), Arg.Any<CancellationToken>())
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
                attempt.SanitizedErrorMessage == "SMTP timeout while waiting for provider acknowledgement."),
            Arg.Any<CancellationToken>());
        await fixture.Repository.Received(1).MarkAsUnknown(
            dispatch.Id,
            "smtp_outcome_unknown",
            "SMTP timeout while waiting for provider acknowledgement.",
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await fixture.Repository.Received(1).MarkReceiptFailed(
            Arg.Any<Guid>(),
            "smtp_outcome_unknown",
            "SMTP timeout while waiting for provider acknowledgement.",
            Arg.Any<DateTime>(),
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

    private static EmailDispatchOutbox CreateDispatch(EmailDispatchStatus status)
    {
        return new EmailDispatchOutbox
        {
            Id = Guid.CreateVersion7(),
            TenantId = Guid.CreateVersion7(),
            PublishEventId = Guid.CreateVersion7(),
            UserId = Guid.CreateVersion7(),
            Kind = EmailDispatchKind.RegistrationConfirmation,
            SourceType = "event-registration",
            SourceId = Guid.CreateVersion7(),
            RecipientEmail = "attendee@example.test",
            Subject = "Registration confirmation",
            PlainTextBody = "Registration confirmed.",
            Status = status
        };
    }

    private static NotificationPreferenceDecision EnabledDecision(NotificationPreferenceResolveRequest request) => new(
        request.CategoryCode,
        request.ChannelCode,
        IsEnabled: true,
        IsRequired: false,
        IsLocked: false,
        IsMuted: false,
        EffectiveSourceScope: "Default",
        LockReason: null);

    private static NotificationPreferenceDecision DisabledDecision(NotificationPreferenceResolveRequest request) => new(
        request.CategoryCode,
        request.ChannelCode,
        IsEnabled: false,
        IsRequired: false,
        IsLocked: false,
        IsMuted: false,
        EffectiveSourceScope: "User",
        LockReason: null);

    private sealed class Fixture
    {
        public Fixture(EmailDispatchProcessorSettings? settings = null)
        {
            Repository = Substitute.For<IEmailDispatchOutboxRepository>();
            EmailService = Substitute.For<IEmailService>();
            PreferenceRepository = Substitute.For<IUserNotificationPreferenceRepository>();
            PreferenceResolver = Substitute.For<INotificationPreferenceResolver>();
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
            PreferenceResolver.ResolveAsync(Arg.Any<NotificationPreferenceResolveRequest>(), Arg.Any<CancellationToken>())
                .Returns(call => EnabledDecision(call.Arg<NotificationPreferenceResolveRequest>()));

            var services = new ServiceCollection();
            services.AddSingleton(Repository);
            services.AddSingleton(EmailService);
            services.AddSingleton(PreferenceRepository);
            services.AddSingleton(PreferenceResolver);
            services.AddSingleton(UnsubscribeTokenService);
            services.AddSingleton(TenantAccessor);
            services.AddSingleton<IConfiguration>(Configuration);
            ServiceProvider = services.BuildServiceProvider();

            var meterFactory = Substitute.For<IMeterFactory>();
            meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter(BusinessMetrics.MeterName));

            Service = new EmailDispatchDrainService(
                ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
                Options.Create(settings ?? new EmailDispatchProcessorSettings()),
                new BusinessMetrics(meterFactory),
                NullLogger<EmailDispatchDrainService>.Instance);
        }

        public IEmailDispatchOutboxRepository Repository { get; }

        public IEmailService EmailService { get; }

        public IUserNotificationPreferenceRepository PreferenceRepository { get; }

        public INotificationPreferenceResolver PreferenceResolver { get; }

        public IEmailUnsubscribeTokenService UnsubscribeTokenService { get; }

        public ITenantContextAccessor TenantAccessor { get; }

        public IConfiguration Configuration { get; }

        public ServiceProvider ServiceProvider { get; }

        public EmailDispatchDrainService Service { get; }
    }
}
