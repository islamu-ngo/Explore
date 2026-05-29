// ABOUTME: Unit tests for broker-neutral EmailDispatch single-row drainage.
// ABOUTME: Verifies RabbitMQ consumers can reuse SMTP state transitions without owning delivery logic.

using System.Diagnostics.Metrics;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Models;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Infrastructure;

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
            Kind = EmailDispatchKind.RegistrationConfirmation,
            SourceType = "event-registration",
            SourceId = Guid.CreateVersion7(),
            RecipientEmail = "attendee@example.test",
            Subject = "Registration confirmation",
            PlainTextBody = "Registration confirmed.",
            Status = status
        };
    }

    private sealed class Fixture
    {
        public Fixture(EmailDispatchProcessorSettings? settings = null)
        {
            Repository = Substitute.For<IEmailDispatchOutboxRepository>();
            EmailService = Substitute.For<IEmailService>();
            TenantAccessor = Substitute.For<ITenantContextAccessor>();

            var services = new ServiceCollection();
            services.AddSingleton(Repository);
            services.AddSingleton(EmailService);
            services.AddSingleton(TenantAccessor);
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

        public ITenantContextAccessor TenantAccessor { get; }

        public ServiceProvider ServiceProvider { get; }

        public EmailDispatchDrainService Service { get; }
    }
}
