// ABOUTME: Unit-style tests for the API-hosted Quartz email dispatch jobs.
// ABOUTME: Proves jobs delegate to Application drain contracts and treat scheduler payloads as pointers only.

using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using Explore.API.Scheduling;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Scheduling;
using Explore.Application.Contracts.Services;
using Explore.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Quartz;
using TUnit.Core;

namespace ApiIntegrationTests.Features;

[Category(TestCategories.Email)]
public sealed class EmailDispatchQuartzJobsTests
{
    [Test]
    public async Task DrainJobCallsSchedulerNeutralDrainService()
    {
        var drainService = Substitute.For<IEmailDispatchDrainService>();
        drainService.ProcessBatchAsync(Arg.Any<CancellationToken>())
            .Returns(new EmailDispatchDrainResult(1, 1, 1, 0, 0, 0, 0, 0, 0));
        var job = new EmailDispatchDrainJob(drainService, NullLogger<EmailDispatchDrainJob>.Instance);

        await job.Execute(CreateContext());

        await drainService.Received(1).ProcessBatchAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DrainJobBubblesUnexpectedDrainFailuresToScheduler()
    {
        var drainService = Substitute.For<IEmailDispatchDrainService>();
        drainService.ProcessBatchAsync(Arg.Any<CancellationToken>())
            .Returns<Task<EmailDispatchDrainResult>>(_ => throw new InvalidOperationException("database unavailable"));
        var job = new EmailDispatchDrainJob(drainService, NullLogger<EmailDispatchDrainJob>.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => job.Execute(CreateContext()));

        await Assert.That(exception).IsNotNull();
        await Assert.That(exception!.Message).Contains("database unavailable");
    }

    [Test]
    public async Task RecoveryScanJobCallsSchedulerNeutralRecoveryService()
    {
        var drainService = Substitute.For<IEmailDispatchDrainService>();
        drainService.RecoverStaleProcessingAsync(Arg.Any<CancellationToken>())
            .Returns(new EmailDispatchRecoveryResult(3, DateTime.UtcNow.AddMinutes(-15)));
        var job = new EmailDispatchRecoveryScanJob(drainService, NullLogger<EmailDispatchRecoveryScanJob>.Instance);

        await job.Execute(CreateContext());

        await drainService.Received(1).RecoverStaleProcessingAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReminderJobCallsSingleOutboxDrainWithPointerIds()
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
        var job = new EventReminderDispatchJob(drainService, NullLogger<EventReminderDispatchJob>.Instance);

        await job.Execute(CreateContext(new ScheduledEmailDispatchPointer(
            tenantId,
            publishEventId,
            EventLifecycleAutomationUseCases.EventReminder,
            EventId: Guid.CreateVersion7(),
            RegistrationIntentId: Guid.CreateVersion7())));

        await drainService.Received(1).ProcessSingleAsync(
            tenantId,
            publishEventId,
            ScheduledJobNames.EventReminderDispatch,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReminderJobSkipsUnsupportedUseCase()
    {
        var drainService = Substitute.For<IEmailDispatchDrainService>();
        var job = new EventReminderDispatchJob(drainService, NullLogger<EventReminderDispatchJob>.Instance);

        await job.Execute(CreateContext(new ScheduledEmailDispatchPointer(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "unsupported",
            EventId: Guid.CreateVersion7(),
            RegistrationIntentId: Guid.CreateVersion7())));

        await AssertNoSingleDrain(drainService);
    }

    [Test]
    public async Task ReminderJobSkipsWhenPointerIsMissing()
    {
        var drainService = Substitute.For<IEmailDispatchDrainService>();
        var job = new EventReminderDispatchJob(drainService, NullLogger<EventReminderDispatchJob>.Instance);

        await job.Execute(CreateContext());

        await AssertNoSingleDrain(drainService);
    }

    [Test]
    public async Task ReminderJobDropsMalformedPointerInsteadOfThrowing()
    {
        var drainService = Substitute.For<IEmailDispatchDrainService>();
        var job = new EventReminderDispatchJob(drainService, NullLogger<EventReminderDispatchJob>.Instance);
        var dataMap = new JobDataMap
        {
            { QuartzSchedulerKeys.DispatchPointerDataKey, "{ not-valid-json" }
        };

        await job.Execute(CreateContext(dataMap));

        await AssertNoSingleDrain(drainService);
    }

    private static async Task AssertNoSingleDrain(IEmailDispatchDrainService drainService)
    {
        await drainService.DidNotReceive().ProcessSingleAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    private static IJobExecutionContext CreateContext(ScheduledEmailDispatchPointer pointer)
    {
        return CreateContext(new JobDataMap
        {
            { QuartzSchedulerKeys.DispatchPointerDataKey, JsonSerializer.Serialize(pointer) }
        });
    }

    private static IJobExecutionContext CreateContext(JobDataMap? dataMap = null)
    {
        var context = Substitute.For<IJobExecutionContext>();
        context.CancellationToken.Returns(CancellationToken.None);
        context.MergedJobDataMap.Returns(dataMap ?? []);
        return context;
    }
}
