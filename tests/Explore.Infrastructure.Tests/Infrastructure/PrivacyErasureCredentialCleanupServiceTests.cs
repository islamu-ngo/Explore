// ABOUTME: Unit tests for privacy-erasure receipt and provider-locator credential cleanup.
// ABOUTME: Verifies dry-run aggregates and that cleanup never claims executable provider work.

using Explore.Application.Configuration;
using Explore.Application.Contracts.Persistence;
using Explore.Infrastructure;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Infrastructure;

public sealed class PrivacyErasureCredentialCleanupServiceTests
{
    [Test]
    public async Task CleanupAsyncDryRunReportsAggregateEligibilityWithoutClaimingProviderWork()
    {
        DateTime utcNow = new(2026, 7, 25, 9, 0, 0, DateTimeKind.Utc);
        var stateRepository = Substitute.For<IPrivacyErasureStateRepository>();
        var providerWorkRepository = Substitute.For<IPrivacyErasureProviderWorkRepository>();
        stateRepository.ClearExpiredReceiptHashesAsync(utcNow, 25, true, Arg.Any<CancellationToken>())
            .Returns(2);
        providerWorkRepository.ExpireLocatorsAsync(utcNow, 25, true, Arg.Any<CancellationToken>())
            .Returns(3);
        var service = new PrivacyErasureCredentialCleanupService(
            stateRepository,
            providerWorkRepository,
            Options.Create(new PrivacyErasureOptions
            {
                RetentionCleanupBatchSize = 25,
                RetentionCleanupDryRun = true
            }));

        var result = await service.CleanupAsync(utcNow, CancellationToken.None);

        await Assert.That(result.DryRun).IsTrue();
        await Assert.That(result.ReceiptHashesEligible).IsEqualTo(2);
        await Assert.That(result.ReceiptHashesCleared).IsEqualTo(0);
        await Assert.That(result.ProviderLocatorsEligible).IsEqualTo(3);
        await Assert.That(result.ProviderLocatorsCleared).IsEqualTo(0);
        await providerWorkRepository.DidNotReceiveWithAnyArgs().ClaimDueAsync(
            default!,
            default,
            default,
            default,
            default);
    }

    [Test]
    public async Task CleanupAsyncCancellationStopsBeforeProviderLocatorCleanup()
    {
        DateTime utcNow = new(2026, 7, 25, 9, 0, 0, DateTimeKind.Utc);
        var stateRepository = Substitute.For<IPrivacyErasureStateRepository>();
        var providerWorkRepository = Substitute.For<IPrivacyErasureProviderWorkRepository>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        stateRepository.ClearExpiredReceiptHashesAsync(utcNow, 25, true, cancellation.Token)
            .Returns(Task.FromCanceled<int>(cancellation.Token));
        var service = new PrivacyErasureCredentialCleanupService(
            stateRepository,
            providerWorkRepository,
            Options.Create(new PrivacyErasureOptions
            {
                RetentionCleanupBatchSize = 25,
                RetentionCleanupDryRun = true
            }));

        await Assert.That(async () => await service.CleanupAsync(utcNow, cancellation.Token))
            .Throws<OperationCanceledException>();
        await providerWorkRepository.DidNotReceiveWithAnyArgs().ExpireLocatorsAsync(
            default,
            default,
            default,
            default);
    }
}
