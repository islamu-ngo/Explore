// ABOUTME: Unit tests for bounded email dispatch content-redaction orchestration.
// ABOUTME: Verifies retention cutoff, dry-run behavior, and bounded repository mutation.

using Explore.Application.Contracts.Persistence;
using Explore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Infrastructure;

public sealed class EmailDispatchRetentionCleanupServiceTests
{
    [Test]
    public async Task CleanupAsyncWhenDryRunCountsWithoutRedacting()
    {
        var repository = Substitute.For<IEmailDispatchOutboxRepository>();
        var tenantId = Guid.NewGuid();
        var utcNow = new DateTime(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc);
        var cutoff = utcNow.AddDays(-180);
        repository.GetRetentionTenantIds(cutoff, 100, Arg.Any<CancellationToken>()).Returns([tenantId]);
        repository.CountRetentionRedactionEligible(tenantId, cutoff, 25, Arg.Any<CancellationToken>()).Returns(7);
        var service = CreateService(repository, new EmailDispatchRetentionSettings
        {
            DryRun = true,
            BatchSize = 25,
            RetentionDays = 180
        });

        var result = await service.CleanupAsync(utcNow, CancellationToken.None);

        await Assert.That(result.DryRun).IsTrue();
        await Assert.That(result.CutoffUtc).IsEqualTo(cutoff);
        await Assert.That(result.EligibleCount).IsEqualTo(7);
        await Assert.That(result.RedactedCount).IsEqualTo(0);
        await repository.DidNotReceiveWithAnyArgs().RedactRetentionEligible(default, default, default, default, default);
    }

    [Test]
    public async Task CleanupAsyncRedactsOnlyOneBoundedBatch()
    {
        var repository = Substitute.For<IEmailDispatchOutboxRepository>();
        var tenantId = Guid.NewGuid();
        var utcNow = new DateTime(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc);
        var cutoff = utcNow.AddDays(-90);
        repository.GetRetentionTenantIds(cutoff, 100, Arg.Any<CancellationToken>()).Returns([tenantId]);
        repository.CountRetentionRedactionEligible(tenantId, cutoff, 10, Arg.Any<CancellationToken>()).Returns(10);
        repository.RedactRetentionEligible(tenantId, cutoff, utcNow, 10, Arg.Any<CancellationToken>()).Returns(8);
        var service = CreateService(repository, new EmailDispatchRetentionSettings
        {
            BatchSize = 10,
            RetentionDays = 90
        });

        var result = await service.CleanupAsync(utcNow, CancellationToken.None);

        await Assert.That(result.DryRun).IsFalse();
        await Assert.That(result.EligibleCount).IsEqualTo(10);
        await Assert.That(result.RedactedCount).IsEqualTo(8);
        await repository.Received(1).RedactRetentionEligible(tenantId, cutoff, utcNow, 10, Arg.Any<CancellationToken>());
    }

    private static EmailDispatchRetentionCleanupService CreateService(
        IEmailDispatchOutboxRepository repository,
        EmailDispatchRetentionSettings settings)
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<int>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<int>>>()(call.ArgAt<CancellationToken>(1)));
        return new(
            repository,
            unitOfWork,
            Options.Create(settings),
            NullLogger<EmailDispatchRetentionCleanupService>.Instance);
    }
}
