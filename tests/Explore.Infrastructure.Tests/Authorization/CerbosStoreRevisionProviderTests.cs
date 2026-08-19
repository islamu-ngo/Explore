// ABOUTME: Pins when the Cerbos policy revision counts as observed versus unknown, and that it is cached.
// ABOUTME: Unknown is the fail-closed signal, so anything that could fake certainty is a security defect.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Explore.Infrastructure.Tests.Authorization;

/// <summary>
/// Every path through this provider ends in one of two answers, and the asymmetry matters: reporting
/// <c>Observed</c> when the revision is not actually known would let sensitive actions through against a
/// policy set nobody can identify, while reporting <c>Unknown</c> too eagerly only costs availability.
/// </summary>
public class CerbosStoreRevisionProviderTests : IDisposable
{
    private readonly IPolicyPackageService _policyPackageService = Substitute.For<IPolicyPackageService>();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    public void Dispose()
    {
        _cache.Dispose();
        GC.SuppressFinalize(this);
    }

    private CerbosStoreRevisionProvider CreateProvider() =>
        new(_policyPackageService, _cache, NullLogger<CerbosStoreRevisionProvider>.Instance);

    private static PolicyPackageStatusResult Status(
        PolicyPackageIssueCode issueCode,
        string? observedRevision) =>
        new(
            PackageId: "test-package",
            ContentHash: new string('a', 64),
            CheckedAt: DateTimeOffset.UtcNow,
            IssueCode: issueCode,
            Message: "test",
            Warnings: [],
            ObservedRevision: observedRevision);

    [Test]
    public async Task GetCurrentAsync_WhenPackageIsHealthyAndRevisionIsReadable_ReportsObserved()
    {
        _policyPackageService.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(Status(PolicyPackageIssueCode.None, "a1b2c3d4e5f60718"));

        var revision = await CreateProvider().GetCurrentAsync();

        await Assert.That(revision.Certainty).IsEqualTo(AuthorizationRevisionCertainty.Observed);
        await Assert.That(revision.IsCertain).IsTrue();
        await Assert.That(revision.Value).IsEqualTo("a1b2c3d4e5f60718");
    }

    /// <summary>
    /// A store that lists cleanly but whose hashes could not be read is exactly the case an in-place
    /// policy edit hides in. "Healthy package, no revision" must not read as certainty.
    /// </summary>
    [Test]
    public async Task GetCurrentAsync_WhenPackageIsHealthyButRevisionIsMissing_ReportsUnknown()
    {
        _policyPackageService.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(Status(PolicyPackageIssueCode.None, observedRevision: null));

        var revision = await CreateProvider().GetCurrentAsync();

        await Assert.That(revision.Certainty).IsEqualTo(AuthorizationRevisionCertainty.Unknown);
        await Assert.That(revision.IsCertain).IsFalse();
        await Assert.That(revision.Value).IsNull();
    }

    /// <summary>
    /// A revision read off an unhealthy package describes a store that is not the one we published.
    /// Carrying it forward would stamp decisions with a revision that means the opposite of what it says.
    /// </summary>
    [Test]
    [Arguments(PolicyPackageIssueCode.PackageStatusUnknown)]
    [Arguments(PolicyPackageIssueCode.PackageMismatch)]
    [Arguments(PolicyPackageIssueCode.AdminApiNotConfigured)]
    [Arguments(PolicyPackageIssueCode.PackageUnavailable)]
    public async Task GetCurrentAsync_WhenPackageIsUnhealthy_ReportsUnknownEvenWithARevision(
        PolicyPackageIssueCode issueCode)
    {
        _policyPackageService.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(Status(issueCode, "a1b2c3d4e5f60718"));

        var revision = await CreateProvider().GetCurrentAsync();

        await Assert.That(revision.IsCertain).IsFalse();
    }

    /// <summary>
    /// This sits on the decision path. A throwing status check must degrade to uncertainty, not surface
    /// as a 500 on every authorized request.
    /// </summary>
    [Test]
    public async Task GetCurrentAsync_WhenStatusThrows_ReportsUnknownWithoutPropagating()
    {
        _policyPackageService.GetStatusAsync(Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("admin api down"));

        var revision = await CreateProvider().GetCurrentAsync();

        await Assert.That(revision.IsCertain).IsFalse();
    }

    [Test]
    public async Task GetCurrentAsync_CachesTheObservationInsteadOfQueryingPerDecision()
    {
        _policyPackageService.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(Status(PolicyPackageIssueCode.None, "a1b2c3d4e5f60718"));

        var provider = CreateProvider();
        await provider.GetCurrentAsync();
        await provider.GetCurrentAsync();
        await provider.GetCurrentAsync();

        await _policyPackageService.Received(1).GetStatusAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// The uncertain result has to be cached too. Re-querying an unreachable Admin API on every batch
    /// turns a policy-store outage into a latency outage on the whole request path.
    /// </summary>
    /// <summary>
    /// The operator-visible half of convergence: after publishing, the very next decision must see the
    /// new store. Without invalidation a successful publish looks like it did nothing for a full cache
    /// window — the gate keeps denying and the status keeps showing the old revision.
    /// </summary>
    [Test]
    public async Task Invalidate_ForcesTheNextCallToReObserveTheStore()
    {
        _policyPackageService.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(
                Status(PolicyPackageIssueCode.PackageMismatch, observedRevision: null),
                Status(PolicyPackageIssueCode.None, "a1b2c3d4e5f60718"));

        var provider = CreateProvider();

        var beforePublish = await provider.GetCurrentAsync();
        provider.Invalidate();
        var afterPublish = await provider.GetCurrentAsync();

        await Assert.That(beforePublish.IsCertain).IsFalse();
        await Assert.That(afterPublish.IsCertain).IsTrue();
        await Assert.That(afterPublish.Value).IsEqualTo("a1b2c3d4e5f60718");
        await _policyPackageService.Received(2).GetStatusAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetCurrentAsync_CachesTheUnknownResultToo()
    {
        _policyPackageService.GetStatusAsync(Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("admin api down"));

        var provider = CreateProvider();
        await provider.GetCurrentAsync();
        await provider.GetCurrentAsync();

        await _policyPackageService.Received(1).GetStatusAsync(Arg.Any<CancellationToken>());
    }
}
