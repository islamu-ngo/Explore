// ABOUTME: Unit-style tests for API-hosted TickerQ email dispatch jobs.
// ABOUTME: Proves scheduler functions delegate to Application drain contracts and preserve retry boundaries.

using Event.Api.IntegrationTests.Fixtures;
using Explore.API.Scheduling;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Scheduling;
using Explore.Application.Contracts.Services;
using Explore.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TickerQ.Utilities.Base;
using TUnit.Core;

namespace ApiIntegrationTests.Features;

[Category(TestCategories.Email)]
public sealed class EmailDispatchTickerQJobsTests
{
    [Test]
    public async Task DrainEmailDispatchOutboxAsyncCallsSchedulerNeutralDrainService()
    {
        var drainService = Substitute.For<IEmailDispatchDrainService>();
        drainService.ProcessBatchAsync(Arg.Any<CancellationToken>())
            .Returns(new EmailDispatchDrainResult(1, 1, 1, 0, 0, 0, 0, 0, 0));
        var jobs = new EmailDispatchTickerQJobs(
            drainService,
            NullLogger<EmailDispatchTickerQJobs>.Instance);

        await jobs.DrainEmailDispatchOutboxAsync(context: null, CancellationToken.None);

        await drainService.Received(1).ProcessBatchAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DrainEmailDispatchOutboxAsyncBubblesUnexpectedDrainFailuresToTickerQ()
    {
        var drainService = Substitute.For<IEmailDispatchDrainService>();
        drainService.ProcessBatchAsync(Arg.Any<CancellationToken>())
            .Returns<Task<EmailDispatchDrainResult>>(_ => throw new InvalidOperationException("database unavailable"));
        var jobs = new EmailDispatchTickerQJobs(
            drainService,
            NullLogger<EmailDispatchTickerQJobs>.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            jobs.DrainEmailDispatchOutboxAsync(context: null, CancellationToken.None));

        await Assert.That(exception).IsNotNull();
        await Assert.That(exception!.Message).Contains("database unavailable");
    }

    [Test]
    public async Task RecoverStaleEmailDispatchProcessingAsyncCallsSchedulerNeutralRecoveryService()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-15);
        var drainService = Substitute.For<IEmailDispatchDrainService>();
        drainService.RecoverStaleProcessingAsync(Arg.Any<CancellationToken>())
            .Returns(new EmailDispatchRecoveryResult(3, cutoff));
        var jobs = new EmailDispatchTickerQJobs(
            drainService,
            NullLogger<EmailDispatchTickerQJobs>.Instance);

        await jobs.RecoverStaleEmailDispatchProcessingAsync(context: null, CancellationToken.None);

        await drainService.Received(1).RecoverStaleProcessingAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchEventReminderAsyncCallsSingleOutboxDrainWithPointerIds()
    {
        var tenantId = Guid.CreateVersion7();
        var publishEventId = Guid.CreateVersion7();
        var drainService = Substitute.For<IEmailDispatchDrainService>();
        drainService.ProcessSingleAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new EmailDispatchSingleDrainResult(EmailDispatchDrainOutcome.Sent, Guid.CreateVersion7()));
        var jobs = new EventLifecycleTickerQJobs(
            drainService,
            NullLogger<EventLifecycleTickerQJobs>.Instance);
        var context = new TickerFunctionContext<ScheduledEmailDispatchPointer>(
            new TickerFunctionContext(),
            new ScheduledEmailDispatchPointer(
                tenantId,
                publishEventId,
                EventLifecycleAutomationUseCases.EventReminder,
                EventId: Guid.CreateVersion7(),
                RegistrationIntentId: Guid.CreateVersion7(),
                UserId: Guid.CreateVersion7()));

        await jobs.DispatchEventReminderAsync(context, CancellationToken.None);

        await drainService.Received(1).ProcessSingleAsync(
            tenantId,
            publishEventId,
            ScheduledJobNames.EventReminderDispatch,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchEventReminderAsyncSkipsUnsupportedUseCase()
    {
        var drainService = Substitute.For<IEmailDispatchDrainService>();
        var jobs = new EventLifecycleTickerQJobs(
            drainService,
            NullLogger<EventLifecycleTickerQJobs>.Instance);
        var context = new TickerFunctionContext<ScheduledEmailDispatchPointer>(
            new TickerFunctionContext(),
            new ScheduledEmailDispatchPointer(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                "unsupported",
                EventId: Guid.CreateVersion7(),
                RegistrationIntentId: Guid.CreateVersion7(),
                UserId: Guid.CreateVersion7()));

        await jobs.DispatchEventReminderAsync(context, CancellationToken.None);

        await drainService.DidNotReceive().ProcessSingleAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchEventReminderAsyncSkipsWhenPointerContextIsMissing()
    {
        var drainService = Substitute.For<IEmailDispatchDrainService>();
        var jobs = new EventLifecycleTickerQJobs(
            drainService,
            NullLogger<EventLifecycleTickerQJobs>.Instance);

        await jobs.DispatchEventReminderAsync(context: null, CancellationToken.None);

        await drainService.DidNotReceive().ProcessSingleAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
