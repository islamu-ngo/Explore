// ABOUTME: Proves startup replay re-invalidates the latest retained erasure checkpoint.
// ABOUTME: Ensures durable convergence lets replay continue when immediate cache invalidation fails.

using Explore.Application.Caching;
using Explore.Application.Configuration;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.PrivacyErasure;
using Explore.Application.Contracts.Services;
using Explore.Application.Exceptions;
using Explore.Application.Services;
using Explore.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
            .Contains($"user:detail:{harness.Intent.SubjectId}");
        await Assert.That(harness.Cache.RemovedTags).Contains(CacheTags.Events);
        await Assert.That(harness.Cache.RemovedTags).Contains(CacheTags.EventLists);
        await Assert.That(harness.Cache.RemovedTags).Contains(CacheTags.EventDetails);
        await Assert.That(harness.Cache.RemovedTags).Contains(CacheTags.EventLocations);
        await Assert.That(harness.Cache.RemovedTags)
            .Contains(CacheTags.EventLocationsByTenant(harness.HomeTenantId));
        await Assert.That(harness.Cache.RemovedTags)
            .Contains(CacheTags.EventListByTenant(harness.HomeTenantId));
        await harness.Authority.Received(1).ReadAfterAsync(
            harness.Intent.AuthoritySequence,
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task LatestCheckpoint_CacheFailureContinuesToDurableConvergence()
    {
        ReplayHarness harness = CreateHarness(failOnTag: CacheTags.EventLocations);

        await harness.Service.ReplayPendingAsync(CancellationToken.None);

        await harness.Authority.Received(1).ReadAfterAsync(
            harness.Intent.AuthoritySequence,
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CheckpointBelowRetainedFloor_FailsBeforeReadingOrMutating()
    {
        ReplayHarness harness = CreateHarness(new PrivacyErasureAuthorityState(2, 2));

        StaleRestoreBelowRetainedFloorException? exception = await Assert.ThrowsAsync<
            StaleRestoreBelowRetainedFloorException>(() =>
                harness.Service.ReplayPendingAsync(CancellationToken.None));

        await Assert.That(exception!.ReasonCode)
            .IsEqualTo("stale_restore_below_retained_floor");
        await harness.Authority.DidNotReceive().ReadAfterAsync(
            Arg.Any<long>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CheckpointAheadOfAuthority_FailsBeforeReadingOrMutating()
    {
        ReplayHarness harness = CreateHarness(new PrivacyErasureAuthorityState(0, 0));

        PrivacyErasureReplayException? exception = await Assert.ThrowsAsync<
            PrivacyErasureReplayException>(() =>
                harness.Service.ReplayPendingAsync(CancellationToken.None));

        await Assert.That(exception!.ReasonCode).IsEqualTo("checkpoint_ahead_of_authority");
        await harness.Authority.DidNotReceive().ReadAfterAsync(
            Arg.Any<long>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    private static ReplayHarness CreateHarness(
        PrivacyErasureAuthorityState? authorityState = null,
        string? failOnTag = null)
    {
        Guid ownerUserId = Guid.CreateVersion7();
        DateTime recordedAtUtc = DateTime.UtcNow;
        PrivacyErasureIntent intent = PrivacyErasureIntent.Record(
            Guid.CreateVersion7(),
            1,
            PrivacyErasureSubjectKind.User,
            ownerUserId,
            PrivacyErasureReasonCode.AccountDeletion,
            1,
            recordedAtUtc,
            recordedAtUtc);
        PrivacyErasureReplayCheckpoint checkpoint =
            PrivacyErasureReplayCheckpoint.Start(
                intent,
                recordedAtUtc,
                Guid.CreateVersion7());

        IPrivacyErasureReplayCheckpointRepository checkpointRepository =
            Substitute.For<IPrivacyErasureReplayCheckpointRepository>();
        checkpointRepository.GetLatestAsync(Arg.Any<CancellationToken>())
            .Returns(checkpoint);

        IPrivacyErasureAuthority authority =
            Substitute.For<IPrivacyErasureAuthority>();
        authority.GetStateAsync(Arg.Any<CancellationToken>())
            .Returns(authorityState ?? new PrivacyErasureAuthorityState(1, 0));
        authority.ReadAfterAsync(Arg.Any<long>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<long>(0) == 0 ? [intent] : []);

        IUserLocationPrivacyErasureRepository erasureRepository =
            Substitute.For<IUserLocationPrivacyErasureRepository>();
        Guid homeTenantId = Guid.CreateVersion7();
        var home = new Location
        {
            Id = Guid.CreateVersion7(),
            TenantId = homeTenantId,
            Tenant = null!,
            FullName = "Erased home",
            Country = "BE",
            City = "Brussels",
            ConcurrencyStamp = Guid.CreateVersion7()
        };
        erasureRepository
            .GetOwnedPrivateHomesAsync(ownerUserId, Arg.Any<CancellationToken>())
            .Returns([home]);
        erasureRepository
            .GetEventLocationsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var cache = new RecordingHybridCache(failOnTag);
        IUserRepository userRepository = Substitute.For<IUserRepository>();
        IGenericRepository<UserPii, Guid> userPiiRepository =
            Substitute.For<IGenericRepository<UserPii, Guid>>();
        IUserAuthenticationTokenRepository tokenRepository =
            Substitute.For<IUserAuthenticationTokenRepository>();
        IOutboxRepository outboxRepository = Substitute.For<IOutboxRepository>();
        IPrivacyErasureStateRepository stateRepository = Substitute.For<IPrivacyErasureStateRepository>();
        PrivacyErasureSaga? saga = null;
        stateRepository.GetBySubjectAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(_ => saga);
        stateRepository.GetByIntentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(_ => saga);
        stateRepository.HasCoverageAsync(intent.IntentId, 1, Arg.Any<CancellationToken>()).Returns(true);
        stateRepository.AddSagaAsync(Arg.Any<PrivacyErasureSaga>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                saga = call.Arg<PrivacyErasureSaga>();
                return Task.CompletedTask;
            });
        var applier = new PrivacyErasureApplier(
            userRepository,
            userPiiRepository,
            tokenRepository,
            erasureRepository,
            Substitute.For<IUserPrivacyErasureRepository>(),
            Substitute.For<IAiConversationRepository>(),
            Substitute.For<IPrivacyErasureProviderWorkRepository>(),
            Substitute.For<IPrivacyErasureProviderLocatorProtector>(),
            checkpointRepository,
            stateRepository,
            outboxRepository,
            cache,
            TimeProvider.System,
            Substitute.For<ILogger<PrivacyErasureApplier>>(),
            Options.Create(new PrivacyErasureOptions()));
        var service = new RetainedAuthorityPrivacyErasureWorkflow(
            checkpointRepository,
            stateRepository,
            authority,
            Substitute.For<IUnitOfWork>(),
            applier,
            Options.Create(new PrivacyErasureOptions()),
            TimeProvider.System);

        return new ReplayHarness(service, intent, authority, cache, homeTenantId);
    }

    private sealed record ReplayHarness(
        RetainedAuthorityPrivacyErasureWorkflow Service,
        PrivacyErasureIntent Intent,
        IPrivacyErasureAuthority Authority,
        RecordingHybridCache Cache,
        Guid HomeTenantId);

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
