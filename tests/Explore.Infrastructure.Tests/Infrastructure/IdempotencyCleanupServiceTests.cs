// ABOUTME: Unit tests for expired idempotency replay-cache cleanup orchestration.
// ABOUTME: Verifies dry-run mode, bounded deletion, cutoff grace, and failure propagation.

using System.Diagnostics.Metrics;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Telemetry;
using Explore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Infrastructure;

public sealed class IdempotencyCleanupServiceTests
{
    [Test]
    public async Task CleanupExpiredAsync_WhenDryRun_DoesNotDeleteRows()
    {
        var repository = Substitute.For<IIdempotencyRepository>();
        var utcNow = new DateTime(2026, 5, 28, 12, 0, 0, DateTimeKind.Utc);
        repository.CountExpiredAsync(utcNow.AddHours(-24), 25, Arg.Any<CancellationToken>())
            .Returns(7);
        var service = CreateService(repository, new IdempotencyCleanupSettings
        {
            DryRun = true,
            BatchSize = 25,
            ExpirationGraceHours = 24
        });

        var result = await service.CleanupExpiredAsync(utcNow, CancellationToken.None);

        await Assert.That(result.DryRun).IsTrue();
        await Assert.That(result.EligibleCount).IsEqualTo(7);
        await Assert.That(result.DeletedCount).IsEqualTo(0);
        await repository.DidNotReceiveWithAnyArgs().DeleteExpiredAsync(default, default, default);
    }

    [Test]
    public async Task CleanupExpiredAsync_WhenDeleteMode_DeletesOnlyEligibleBatch()
    {
        var repository = Substitute.For<IIdempotencyRepository>();
        var utcNow = new DateTime(2026, 5, 28, 12, 0, 0, DateTimeKind.Utc);
        var cutoff = utcNow.AddHours(-12);
        repository.CountExpiredAsync(cutoff, 10, Arg.Any<CancellationToken>()).Returns(10);
        repository.DeleteExpiredAsync(cutoff, 10, Arg.Any<CancellationToken>()).Returns(8);
        var service = CreateService(repository, new IdempotencyCleanupSettings
        {
            BatchSize = 10,
            ExpirationGraceHours = 12
        });

        var result = await service.CleanupExpiredAsync(utcNow, CancellationToken.None);

        await Assert.That(result.DryRun).IsFalse();
        await Assert.That(result.ExpiresBeforeUtc).IsEqualTo(cutoff);
        await Assert.That(result.EligibleCount).IsEqualTo(10);
        await Assert.That(result.DeletedCount).IsEqualTo(8);
        await repository.Received(1).DeleteExpiredAsync(cutoff, 10, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CleanupExpiredAsync_WhenNoEligibleRows_SkipsDelete()
    {
        var repository = Substitute.For<IIdempotencyRepository>();
        var utcNow = new DateTime(2026, 5, 28, 12, 0, 0, DateTimeKind.Utc);
        repository.CountExpiredAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(0);
        var service = CreateService(repository, new IdempotencyCleanupSettings());

        var result = await service.CleanupExpiredAsync(utcNow, CancellationToken.None);

        await Assert.That(result.EligibleCount).IsEqualTo(0);
        await Assert.That(result.DeletedCount).IsEqualTo(0);
        await repository.DidNotReceiveWithAnyArgs().DeleteExpiredAsync(default, default, default);
    }

    private static IdempotencyCleanupService CreateService(
        IIdempotencyRepository repository,
        IdempotencyCleanupSettings settings)
    {
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter(BusinessMetrics.MeterName));

        return new IdempotencyCleanupService(
            repository,
            Options.Create(settings),
            new BusinessMetrics(meterFactory),
            NullLogger<IdempotencyCleanupService>.Instance);
    }
}
