// ABOUTME: Proves startup replay re-invalidates the latest retained erasure checkpoint.
// ABOUTME: Ensures stale cache state keeps the replay gate closed without leaking provider detail.

using Explore.Application.Caching;
using Explore.Application.Contracts.LocationPrivacy;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TUnit.Core;

namespace Event.Application.UnitTests.Services;

public sealed class GlobalLocationPrivacyReplayCacheGateTests
{
    [Test]
    public async Task LatestCheckpoint_IsReinvalidatedBeforeReadingLaterFacts()
    {
        ReplayHarness harness = CreateHarness();

        await harness.Service.ReplayPendingAsync(CancellationToken.None);

        await Assert.That(harness.Cache.RemovedKeys)
            .Contains($"user:detail:{harness.Intent.OwnerUserId}");
        await Assert.That(harness.Cache.RemovedTags).Contains(CacheTags.EventLocations);
        await harness.Authority.Received(1).ReadAfterAsync(
            harness.Intent.AuthoritySequence,
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task LatestCheckpoint_CacheFailureFailsClosedBeforeReadingLaterFacts()
    {
        ReplayHarness harness = CreateHarness(failOnTag: CacheTags.EventLocations);

        InvalidOperationException? exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Service.ReplayPendingAsync(CancellationToken.None));

        await Assert.That(exception!.Message)
            .IsEqualTo("Post-commit privacy-erasure cache invalidation failed.");
        await Assert.That(exception.Message).DoesNotContain(RecordingHybridCache.RawFailureCanary);
        await harness.Authority.DidNotReceive().ReadAfterAsync(
            harness.Intent.AuthoritySequence,
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    private static ReplayHarness CreateHarness(string? failOnTag = null)
    {
        Guid ownerUserId = Guid.CreateVersion7();
        DateTime recordedAtUtc = DateTime.UtcNow;
        LocationPrivacyErasureAuthorityIntent intent = LocationPrivacyErasureAuthorityIntent.Record(
            Guid.CreateVersion7(),
            1,
            ownerUserId,
            [],
            LocationPrivacyErasureReasonEnum.AccountDeletion,
            recordedAtUtc,
            recordedAtUtc);
        LocationPrivacyErasureReplayCheckpoint checkpoint =
            LocationPrivacyErasureReplayCheckpoint.Start(
                intent,
                recordedAtUtc,
                Guid.CreateVersion7());

        ILocationPrivacyErasureReplayCheckpointRepository checkpointRepository =
            Substitute.For<ILocationPrivacyErasureReplayCheckpointRepository>();
        checkpointRepository.GetLatestAsync(Arg.Any<CancellationToken>())
            .Returns(checkpoint);

        ILocationPrivacyErasureAuthority authority =
            Substitute.For<ILocationPrivacyErasureAuthority>();
        authority.ReadAfterAsync(Arg.Any<long>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<long>(0) == 0 ? [intent] : []);

        IGlobalLocationPrivacyErasureRepository erasureRepository =
            Substitute.For<IGlobalLocationPrivacyErasureRepository>();
        erasureRepository
            .GetEventLocationsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var cache = new RecordingHybridCache(failOnTag);
        var service = new GlobalLocationPrivacyErasureService(
            Substitute.For<IUserRepository>(),
            Substitute.For<IGenericRepository<UserPii, Guid>>(),
            Substitute.For<IUserAuthenticationTokenRepository>(),
            erasureRepository,
            checkpointRepository,
            Substitute.For<IOutboxRepository>(),
            authority,
            Substitute.For<IUnitOfWork>(),
            cache,
            TimeProvider.System,
            Substitute.For<ILogger<GlobalLocationPrivacyErasureService>>());

        return new ReplayHarness(service, intent, authority, cache);
    }

    private sealed record ReplayHarness(
        GlobalLocationPrivacyErasureService Service,
        LocationPrivacyErasureAuthorityIntent Intent,
        ILocationPrivacyErasureAuthority Authority,
        RecordingHybridCache Cache);

    private sealed class RecordingHybridCache(string? failOnTag) : HybridCache
    {
        public const string RawFailureCanary = "redis-endpoint-and-secret-canary";

        public List<string> RemovedKeys { get; } = [];
        public List<string> RemovedTags { get; } = [];

        public override ValueTask<T> GetOrCreateAsync<TState, T>(
            string key,
            TState state,
            Func<TState, CancellationToken, ValueTask<T>> factory,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default) => factory(state, cancellationToken);

        public override ValueTask RemoveAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            RemovedKeys.Add(key);
            return ValueTask.CompletedTask;
        }

        public override ValueTask RemoveByTagAsync(
            string tag,
            CancellationToken cancellationToken = default)
        {
            RemovedTags.Add(tag);
            if (tag == failOnTag)
            {
                throw new IOException(RawFailureCanary);
            }

            return ValueTask.CompletedTask;
        }

        public override ValueTask SetAsync<T>(
            string key,
            T value,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
