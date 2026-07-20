// ABOUTME: Verifies Infrastructure startup replay delegates to the atomic Application boundary.
// ABOUTME: Preserves cancellation and fails closed on continuity or integrity rejection.

using Explore.Application.Contracts.Services;
using Explore.Infrastructure.Services.Privacy;
using NSubstitute;
using TUnit.Core;

namespace Explore.Infrastructure.Tests.Infrastructure.Privacy;

public sealed class LocationErasureReplayServiceTests
{
    [Test]
    public async Task ReplayAsync_UsesApplicationErasureBoundaryAndCancellationToken()
    {
        IGlobalLocationPrivacyErasureService erasure =
            Substitute.For<IGlobalLocationPrivacyErasureService>();
        using var cancellation = new CancellationTokenSource();
        var service = new LocationErasureReplayService(erasure);

        await service.ReplayAsync(cancellation.Token);

        await erasure.Received(1).ReplayPendingAsync(cancellation.Token);
    }

    [Test]
    public async Task ReplayAsync_ContinuityOrIntegrityFailure_FailsClosed()
    {
        IGlobalLocationPrivacyErasureService erasure =
            Substitute.For<IGlobalLocationPrivacyErasureService>();
        erasure.ReplayPendingAsync(Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("sequence gap"));
        var service = new LocationErasureReplayService(erasure);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ReplayAsync(CancellationToken.None));
    }
}
