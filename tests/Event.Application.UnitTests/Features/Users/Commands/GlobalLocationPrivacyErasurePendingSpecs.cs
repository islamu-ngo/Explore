// ABOUTME: Behavioral red specifications for global account deletion's future location-erasure orchestration.
// ABOUTME: Exercises the current command boundary so Todo 10 must fail closed and erase every owned Home.

using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.PrivacyErasure;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.Users.Handlers.Commands;
using Explore.Application.Features.Users.Requests.Commands;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Users.Commands;

[Category("EventLocationPrivacy")]
public sealed class GlobalLocationPrivacyErasurePendingSpecs
{
    [Test]
    public async Task AuthorityUnavailable_FailsClosedBeforeDeletingUser()
    {
        var userId = Guid.CreateVersion7();
        await using DeletionHarness harness = CreateHarness(userId);
        harness.Authority
            .AppendAsync(
                Arg.Any<PrivacyErasureRequest>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<PrivacyErasureIntent>>(_ =>
                throw new InvalidOperationException("The retained erasure authority is unavailable."));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Handler.Handle(new DeleteUserCommand { UserId = userId }, CancellationToken.None));
        await harness.UserRepository.DidNotReceive().Update(Arg.Any<User>());
    }

    [Test]
    public async Task TwoTenantOwnedHomes_AreTombstonedBeforeUserDeletion()
    {
        var userId = Guid.CreateVersion7();
        var tenantAHome = CreatePrivateHome(Guid.CreateVersion7(), userId, "Owner home A");
        var tenantBHome = CreatePrivateHome(Guid.CreateVersion7(), userId, "Owner home B");
        var unrelatedHome = CreatePrivateHome(Guid.CreateVersion7(), Guid.CreateVersion7(), "Unrelated home");
        await using DeletionHarness harness = CreateHarness(userId);
        harness.ErasureRepository
            .GetOwnedPrivateHomesAsync(userId, Arg.Any<CancellationToken>())
            .Returns([tenantAHome, tenantBHome]);

        await harness.Handler.Handle(
            new DeleteUserCommand { UserId = userId },
            CancellationToken.None);

        await Assert.That(tenantAHome.LocationPrivacyStateId)
            .IsEqualTo((int)LocationPrivacyStateEnum.Erased);
        await Assert.That(tenantBHome.LocationPrivacyStateId)
            .IsEqualTo((int)LocationPrivacyStateEnum.Erased);
        await Assert.That(tenantAHome.Pii).IsNull();
        await Assert.That(tenantBHome.Pii).IsNull();
        await Assert.That(tenantAHome.FullName).IsEqualTo(Location.ErasedPrivateVenueLabel);
        await Assert.That(tenantBHome.FullName).IsEqualTo(Location.ErasedPrivateVenueLabel);
        await Assert.That(unrelatedHome.LocationPrivacyStateId)
            .IsEqualTo((int)LocationPrivacyStateEnum.Active);
    }

    [Test]
    public async Task AmbiguousAuthorityAcknowledgement_RetriesWithSameIntentId()
    {
        var userId = Guid.CreateVersion7();
        await using DeletionHarness harness = CreateHarness(userId);
        PrivacyErasureIntent? retained = null;
        Guid? firstIntentId = null;
        var appendCount = 0;
        harness.Authority
            .AppendAsync(
                Arg.Any<PrivacyErasureRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                PrivacyErasureRequest intent = call.Arg<PrivacyErasureRequest>();
                appendCount++;
                firstIntentId ??= intent.IntentId;
                if (intent.IntentId != firstIntentId)
                {
                    throw new InvalidOperationException("An ambiguous append retry changed IntentId.");
                }

                retained ??= PrivacyErasureIntent.Record(
                    intent.IntentId,
                    1,
                    intent.SubjectKind,
                    intent.SubjectId,
                    intent.ReasonCode,
                    intent.PolicyVersion,
                    DateTime.UtcNow,
                    DateTime.UtcNow);
                if (appendCount == 1)
                {
                    throw new TimeoutException("The authority retained the fact but the acknowledgement was lost.");
                }

                return retained;
            });
        harness.Authority
            .ReadAfterAsync(Arg.Any<long>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<long>(0) < 1 && retained is not null
                ? [retained]
                : []);

        await harness.Handler.Handle(
            new DeleteUserCommand { UserId = userId },
            CancellationToken.None);

        await Assert.That(appendCount).IsEqualTo(2);
        await Assert.That(firstIntentId).IsNotNull();
        await Assert.That(harness.User.IsDeleted).IsTrue();
    }

    [Test]
    public async Task PreCanceledDeletion_DoesNotReadUserAppendAuthorityOrMutateApplicationState()
    {
        var userId = Guid.CreateVersion7();
        await using DeletionHarness harness = CreateHarness(userId);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            harness.Handler.Handle(
                new DeleteUserCommand { UserId = userId },
                cancellation.Token));

        _ = harness.UserRepository.DidNotReceive().GetById(Arg.Any<Guid>());
        await harness.Authority.DidNotReceive().AppendAsync(
            Arg.Any<PrivacyErasureRequest>(),
            Arg.Any<CancellationToken>());
        await Assert.That(harness.User.IsDeleted).IsFalse();
        await harness.UserRepository.DidNotReceive().Update(Arg.Any<User>());
        await harness.UserRepository.DidNotReceive().Delete(Arg.Any<User>());
    }

    private static DeletionHarness CreateHarness(Guid userId)
    {
        IUserRepository userRepository = Substitute.For<IUserRepository>();
        IGenericRepository<UserPii, Guid> userPiiRepository =
            Substitute.For<IGenericRepository<UserPii, Guid>>();
        IUserAuthenticationTokenRepository tokenRepository =
            Substitute.For<IUserAuthenticationTokenRepository>();
        IUserLocationPrivacyErasureRepository erasureRepository =
            Substitute.For<IUserLocationPrivacyErasureRepository>();
        IPrivacyErasureReplayCheckpointRepository checkpointRepository =
            Substitute.For<IPrivacyErasureReplayCheckpointRepository>();
        IOutboxRepository outboxRepository = Substitute.For<IOutboxRepository>();
        IPrivacyErasureAuthority authority =
            Substitute.For<IPrivacyErasureAuthority>();
        HybridCache cache = Substitute.For<HybridCache>();
        IUnitOfWork unitOfWork = new ImmediateUnitOfWork();
        User user = DataBuilder.User.Generate();
        user.Id = userId;
        PrivacyErasureIntent? retainedIntent = null;
        PrivacyErasureReplayCheckpoint? checkpoint = null;

        userRepository.GetById(userId).Returns(user);
        userRepository.Update(user).Returns(Task.CompletedTask);
        userPiiRepository.GetById(userId).Returns((UserPii?)null);
        tokenRepository.GetByUser(userId, Arg.Any<CancellationToken>())
            .Returns([]);
        erasureRepository
            .GetOwnedPrivateHomesAsync(userId, Arg.Any<CancellationToken>())
            .Returns([]);
        erasureRepository
            .GetEventLocationsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        erasureRepository
            .GetUserActorsAsync(userId, Arg.Any<CancellationToken>())
            .Returns([]);
        authority
            .AppendAsync(Arg.Any<PrivacyErasureRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                PrivacyErasureRequest intent = call.Arg<PrivacyErasureRequest>();
                DateTime recordedAt = DateTime.UtcNow;
                retainedIntent = PrivacyErasureIntent.Record(
                    intent.IntentId,
                    1,
                    intent.SubjectKind,
                    intent.SubjectId,
                    intent.ReasonCode,
                    intent.PolicyVersion,
                    recordedAt,
                    recordedAt);
                return retainedIntent;
            });
        authority
            .ReadAfterAsync(Arg.Any<long>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                long afterSequence = call.ArgAt<long>(0);
                return retainedIntent is not null && afterSequence < retainedIntent.AuthoritySequence
                    ? [retainedIntent]
                    : [];
            });
        checkpointRepository
            .GetLatestAsync(Arg.Any<CancellationToken>())
            .Returns(_ => checkpoint);
        checkpointRepository
            .AppendAsync(
                Arg.Any<PrivacyErasureReplayCheckpoint>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                checkpoint = call.Arg<PrivacyErasureReplayCheckpoint>();
                return checkpoint;
            });
        outboxRepository
            .CreateRange(Arg.Any<IReadOnlyCollection<OutboxMessage>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<IReadOnlyCollection<OutboxMessage>>().ToArray());
        IPrivacyErasureLedgerRepository ledgerRepository =
            Substitute.For<IPrivacyErasureLedgerRepository>();
        ledgerRepository
            .AppendAsync(Arg.Any<PrivacyErasureIntent>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<PrivacyErasureIntent>());
        var applier = new PrivacyErasureApplier(
            userRepository,
            userPiiRepository,
            tokenRepository,
            erasureRepository,
            checkpointRepository,
            ledgerRepository,
            outboxRepository,
            cache,
            TimeProvider.System,
            Substitute.For<ILogger<PrivacyErasureApplier>>());

        IPrivacyErasureService service = new RetainedAuthorityPrivacyErasureWorkflow(
            userRepository,
            checkpointRepository,
            authority,
            unitOfWork,
            applier);
        var handler = new DeleteUserCommandHandler(service);

        return new DeletionHarness(handler, user, userRepository, erasureRepository, authority);
    }

    private static Location CreatePrivateHome(Guid tenantId, Guid ownerUserId, string name)
    {
        var location = new Location
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Tenant = null!,
            FullName = name,
            Country = "BE",
            City = "Brussels",
            ConcurrencyStamp = Guid.CreateVersion7(),
        };
        location.ClassifyAsPrivateHome(ownerUserId);
        location.AttachPii(new LocationPii
        {
            LocationId = location.Id,
            Address = $"{name} address",
            Postcode = "1000",
        });
        return location;
    }

    private sealed record DeletionHarness(
        DeleteUserCommandHandler Handler,
        User User,
        IUserRepository UserRepository,
        IUserLocationPrivacyErasureRepository ErasureRepository,
        IPrivacyErasureAuthority Authority) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class ImmediateUnitOfWork : IUnitOfWork
    {
        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken ct = default) => operation(ct);

        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default) => operation(ct);

        public Task<T> ExecuteSerializableAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default) => operation(ct);
    }
}
