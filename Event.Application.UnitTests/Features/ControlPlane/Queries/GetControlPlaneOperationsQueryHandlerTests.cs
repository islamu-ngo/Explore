// ABOUTME: Unit tests for the control-plane operations status read model.
// ABOUTME: Verifies bounded outbox, email dispatch, and storage warnings stay sanitized and count-based.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.ControlPlane.Handlers.Queries;
using Explore.Application.Features.ControlPlane.Requests.Queries;
using Explore.Domain;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.ControlPlane.Queries;

public sealed class GetControlPlaneOperationsQueryHandlerTests
{
    [Test]
    public async Task Handle_WhenOperationalWarningsExist_ReturnsBoundedStatusCards()
    {
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var emailRepository = Substitute.For<IEmailDispatchOutboxRepository>();
        var storageService = Substitute.For<IInstanceStorageSettingService>();
        var smtpService = Substitute.For<IInstanceSmtpSettingService>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmailDispatchProcessor:HealthDueDispatchWarningThreshold"] = "5",
                ["EmailDispatchProcessor:HealthStaleProcessingWarningThreshold"] = "1",
                ["EmailDispatchProcessor:HealthDeadLetterWarningThreshold"] = "1",
                ["EmailDispatchProcessor:ProcessingLeaseTimeoutSeconds"] = "60"
            })
            .Build();
        outboxRepository.GetPendingBatch(100, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Range(0, 100).Select(_ => CreateOutbox(OutboxMessageStatus.Pending)).ToList());
        outboxRepository.GetFailedEntries(100, Arg.Any<CancellationToken>())
            .Returns(
            [
                CreateOutbox(OutboxMessageStatus.Failed),
                CreateOutbox(OutboxMessageStatus.DeadLettered)
            ]);
        emailRepository.CountDueDispatchAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(5);
        emailRepository.CountRetryScheduledAsync(Arg.Any<CancellationToken>()).Returns(2);
        emailRepository.CountStaleProcessingAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(1);
        emailRepository.CountDeadLetteredAsync(Arg.Any<CancellationToken>()).Returns(1);
        storageService.ReadSettingsAsync(Arg.Any<CancellationToken>()).Returns(new InstanceStorageSettingsDto
        {
            Provider = StorageProviders.S3Compatible,
            ProviderStatus = new InstanceStorageProviderStatusDto
            {
                Provider = StorageProviders.S3Compatible,
                IsAvailable = false,
                FailureCode = "provider_unavailable",
                Message = "Provider unavailable"
            },
            Usage = new InstanceStorageUsageDto
            {
                UsedBytes = 10,
                ReservedBytes = 3,
                QuarantinedBytes = 2,
                ObjectCount = 4
            }
        });
        smtpService.ReadSettingsAsync().Returns(new InstanceSmtpSettingsDto());
        var handler = new GetControlPlaneOperationsQueryHandler(
            outboxRepository,
            emailRepository,
            storageService,
            smtpService,
            configuration);

        var result = await handler.Handle(new GetControlPlaneOperationsQuery(), CancellationToken.None);

        await Assert.That(result.Statuses.Select(status => status.Key))
            .IsEquivalentTo(["general-outbox", "email-dispatch", "storage"]);
        var outbox = result.Statuses.Single(status => status.Key == "general-outbox");
        await Assert.That(outbox.Severity).IsEqualTo("critical");
        await Assert.That(outbox.Metrics.Single(metric => metric.Key == "due").IsCapped).IsTrue();
        await Assert.That(outbox.Metrics.Single(metric => metric.Key == "dead-lettered").Value).IsEqualTo(1);

        var email = result.Statuses.Single(status => status.Key == "email-dispatch");
        await Assert.That(email.Severity).IsEqualTo("critical");
        await Assert.That(email.Metrics.Single(metric => metric.Key == "due").Value).IsEqualTo(5);

        var storage = result.Statuses.Single(status => status.Key == "storage");
        await Assert.That(storage.Severity).IsEqualTo("critical");
        await Assert.That(storage.Metrics.Single(metric => metric.Key == "object-count").Value).IsEqualTo(4);
        await Assert.That(result.Warnings.Select(warning => warning.Code))
            .Contains("email_dispatch_dead_letters");
        await Assert.That(result.Warnings.Select(warning => warning.Code))
            .Contains("storage_provider_unavailable");
    }

    private static OutboxMessage CreateOutbox(OutboxMessageStatus status) => new()
    {
        Id = Guid.NewGuid(),
        AggregateType = "TestAggregate",
        AggregateId = Guid.NewGuid(),
        EventType = "TestEvent",
        Status = status,
        CreatedAt = DateTime.UtcNow
    };
}
