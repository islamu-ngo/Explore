// ABOUTME: Pins the current location-only account-deletion behavior before platform erasure expands it.
// ABOUTME: Records current deletion, anonymization, and ATProto cleanup-ordering gaps.

using Explore.Application.Configuration;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Services;
using Explore.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Users.Commands;

public sealed class DeleteUserCurrentBehaviorCharacterizationTests
{
    [Test]
    public async Task CurrentApplierDeletesUserPiiAndVisibleTokensButOnlyAnonymizesActorPii()
    {
        Guid userId = Guid.CreateVersion7();
        User user = CreateUser(userId);
        UserPii userPii = user.Pii;
        UserAuthenticationToken visibleToken = CreateToken(user, Guid.CreateVersion7());
        Actor actor = CreateActor(userId, "did:example:redacted");
        Harness harness = CreateHarness(user, userPii, [visibleToken], [actor]);

        await harness.Applier.ApplyInCurrentTransactionAsync(harness.Intent, harness.Prepared, CancellationToken.None);

        await harness.UserPiiRepository.Received(1).Delete(userPii);
        await harness.TokenRepository.Received(1).Delete(visibleToken);
        await Assert.That(user.IsDeleted).IsTrue();
        await Assert.That(actor.Pii).IsNotNull();
        await Assert.That(actor.Pii!.DisplayName).IsEqualTo("Deleted user");
        await Assert.That(actor.AtprotoIdentities.Single().IsDeleted).IsTrue();
        await Assert.That(actor.AtprotoIdentities.Single().Handle).IsNull();
    }

    [Test]
    public async Task CurrentAtprotoIdentifierIsClearedBeforeAnyProviderCleanupWorkExists()
    {
        Guid userId = Guid.CreateVersion7();
        Actor actor = CreateActor(userId, "did:example:redacted");
        Harness harness = CreateHarness(CreateUser(userId), null, [], [actor]);

        await harness.Applier.ApplyInCurrentTransactionAsync(harness.Intent, harness.Prepared, CancellationToken.None);

        await Assert.That(actor.AtprotoIdentities.Single().Did).StartsWith("did:deleted:");
        await harness.OutboxRepository.Received(1).CreateRange(
            Arg.Is<IReadOnlyCollection<OutboxMessage>>(messages => messages.Count == 1
                && messages.Single().EventType == PrivacyErasureCacheInvalidationOutboxMessageFactory.EventType
                && messages.Single().AggregateId == userId
                && messages.Single().Payload == null),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SharedActorTombstoneRemovesUserAndProviderLinksWithoutEmbeddingTheIntentId()
    {
        Guid userId = Guid.CreateVersion7();
        Actor actor = CreateActor(userId, "did:example:redacted");
        Actor actorWithoutPii = CreateActor(userId, "did:example:missing-pii");
        actorWithoutPii.Pii = null!;
        Harness harness = CreateHarness(CreateUser(userId), null, [], [actor, actorWithoutPii]);

        await harness.Applier.ApplyInCurrentTransactionAsync(
            harness.Intent,
            harness.Prepared,
            CancellationToken.None);

        await Assert.That(actor.UserId).IsNull();
        await Assert.That(actor.AtprotoIdentities.Single().PdsHost).IsEqualTo(string.Empty);
        await Assert.That(actor.Pii!.DisplayName).IsEqualTo("Deleted user");
        await Assert.That(actor.Pii.DisplayName).DoesNotContain(harness.Intent.IntentId.ToString("N"));
        await Assert.That(actorWithoutPii.UserId).IsNull();
        await Assert.That(actorWithoutPii.AtprotoIdentities.Single().PdsHost).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ApplierErasesMembershipsAndPreferencesInsideTheApplicationTransaction()
    {
        Guid userId = Guid.CreateVersion7();
        Harness harness = CreateHarness(CreateUser(userId), null, [], []);

        await harness.Applier.ApplyInCurrentTransactionAsync(
            harness.Intent,
            harness.Prepared,
            CancellationToken.None);

        await harness.PrivacyErasureRepository.Received(1)
            .EraseMembershipsAndPreferencesAsync(userId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplierErasesRegistrationAndLocalNotificationCopiesInsideTheApplicationTransaction()
    {
        Guid userId = Guid.CreateVersion7();
        Harness harness = CreateHarness(CreateUser(userId), null, [], []);

        await harness.Applier.ApplyInCurrentTransactionAsync(
            harness.Intent,
            harness.Prepared,
            CancellationToken.None);

        await harness.PrivacyErasureRepository.Received(1)
            .EraseRegistrationAndLocalNotificationsAsync(userId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplierAnonymizesRetainedAuditEvidenceInsideTheApplicationTransaction()
    {
        Guid userId = Guid.CreateVersion7();
        Harness harness = CreateHarness(CreateUser(userId), null, [], []);

        await harness.Applier.ApplyInCurrentTransactionAsync(
            harness.Intent,
            harness.Prepared,
            CancellationToken.None);

        await harness.PrivacyErasureRepository.Received(1)
            .AnonymizeRetainedAuditEvidenceAsync(userId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplierHardDeletesAiConversationGraphInsideTheApplicationTransaction()
    {
        Guid userId = Guid.CreateVersion7();
        Harness harness = CreateHarness(CreateUser(userId), null, [], []);

        await harness.Applier.ApplyInCurrentTransactionAsync(
            harness.Intent,
            harness.Prepared,
            CancellationToken.None);

        await harness.AiConversationRepository.Received(1)
            .HardDeleteUserConversationGraphAsync(userId, Arg.Any<CancellationToken>());
        harness.ProviderWorkRepository.Received(1).AddMissingAsync(
            Arg.Is<IReadOnlyCollection<PrivacyErasureProviderWork>>(work => work.Count == 0),
            Arg.Any<CancellationToken>());
        harness.ProviderLocatorProtector.DidNotReceive()
            .Protect(Arg.Any<string>(), Arg.Any<TimeSpan>());
    }


    [Test]
    public async Task ApplierProtectsAndPersistsTypedProviderWorkBeforeLocalSettlement()
    {
        Guid userId = Guid.CreateVersion7();
        Guid targetId = Guid.CreateVersion7();
        var candidate = new PrivacyErasureProviderCandidate(
            PrivacyErasureProviderKind.WebPush,
            PrivacyErasureProviderAction.InvalidateSubscription,
            Guid.CreateVersion7(),
            targetId,
            PrivacyErasureProviderLocatorKind.WebPushEndpoint,
            "https://push.example.invalid/private");
        Harness harness = CreateHarness(CreateUser(userId), null, [], [], providerCandidates: [candidate, candidate]);

        await harness.Applier.ApplyInCurrentTransactionAsync(
            harness.Intent,
            harness.Prepared,
            CancellationToken.None);

        await harness.ProviderWorkRepository.Received(1).AddMissingAsync(
            Arg.Is<IReadOnlyCollection<PrivacyErasureProviderWork>>(work =>
                work.Count == 1
                && work.Single().TargetId == targetId
                && work.Single().LocatorKind == PrivacyErasureProviderLocatorKind.WebPushEndpoint
                && work.Single().ProtectedLocator == "protected-locator"
                && work.Single().LocatorExpiresAtUtc == harness.Prepared.AppliedAtUtc.AddDays(7)),
            Arg.Any<CancellationToken>());
        await Assert.That(harness.Saga.ProviderWorkCount).IsEqualTo(1);
        await Assert.That(harness.Saga.Status).IsEqualTo(PrivacyErasureSagaStatus.ProviderPending);
    }

    [Test]
    public async Task ApplierPersistsProviderWorkBeforeClearingProviderBackedLocalMetadata()
    {
        Guid userId = Guid.CreateVersion7();
        Guid targetId = Guid.CreateVersion7();
        var candidate = new PrivacyErasureProviderCandidate(
            PrivacyErasureProviderKind.Keycloak,
            PrivacyErasureProviderAction.RevokeOrUnlinkExternalIdentity,
            Guid.CreateVersion7(),
            targetId,
            PrivacyErasureProviderLocatorKind.AccountIdentifier,
            "keycloak-subject");
        Harness harness = CreateHarness(CreateUser(userId), null, [], [], providerCandidates: [candidate]);

        await harness.Applier.ApplyInCurrentTransactionAsync(
            harness.Intent,
            harness.Prepared,
            CancellationToken.None);

        Received.InOrder(() =>
        {
            harness.ProviderWorkRepository.AddMissingAsync(
                Arg.Any<IReadOnlyCollection<PrivacyErasureProviderWork>>(),
                Arg.Any<CancellationToken>());
            harness.StateRepository.SaveChangesAsync(Arg.Any<CancellationToken>());
            harness.PrivacyErasureRepository.EraseProviderBackedLocalUserMetadataAsync(
                userId,
                Arg.Any<CancellationToken>());
            harness.PrivacyErasureRepository.AnonymizeRetainedAuditEvidenceAsync(
                userId,
                Arg.Any<CancellationToken>());
            harness.PrivacyErasureRepository.EraseRegistrationAndLocalNotificationsAsync(
                userId,
                Arg.Any<CancellationToken>());
            harness.PrivacyErasureRepository.EraseMembershipsAndPreferencesAsync(
                userId,
                Arg.Any<CancellationToken>());
            harness.StateRepository.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }

    [Test]
    public async Task PolicyUpgradeForOlderIntent_DoesNotAppendCheckpointBehindCurrentSequence()
    {
        Guid userId = Guid.CreateVersion7();
        Harness harness = CreateHarness(CreateUser(userId), null, [], [], currentCheckpointAhead: true);

        await harness.Applier.ApplyInCurrentTransactionAsync(
            harness.Intent,
            harness.Prepared,
            CancellationToken.None);

        await harness.CheckpointRepository.DidNotReceive().AppendAsync(
            Arg.Any<PrivacyErasureReplayCheckpoint>(),
            Arg.Any<CancellationToken>());
        await harness.StateRepository.Received(1).AddCoverageAsync(
            Arg.Is<PrivacyErasurePolicyCoverage>(coverage =>
                coverage.IntentId == harness.Intent.IntentId && coverage.PolicyVersion == 1),
            Arg.Any<CancellationToken>());
    }

    private static Harness CreateHarness(
        User user,
        UserPii? userPii,
        List<UserAuthenticationToken> tokens,
        IReadOnlyList<Actor> actors,
        bool currentCheckpointAhead = false,
        IReadOnlyList<PrivacyErasureProviderCandidate>? providerCandidates = null)
    {
        IUserRepository userRepository = Substitute.For<IUserRepository>();
        IGenericRepository<UserPii, Guid> userPiiRepository = Substitute.For<IGenericRepository<UserPii, Guid>>();
        IUserAuthenticationTokenRepository tokenRepository = Substitute.For<IUserAuthenticationTokenRepository>();
        IUserLocationPrivacyErasureRepository erasureRepository = Substitute.For<IUserLocationPrivacyErasureRepository>();
        IUserPrivacyErasureRepository privacyErasureRepository = Substitute.For<IUserPrivacyErasureRepository>();
        IAiConversationRepository aiConversationRepository = Substitute.For<IAiConversationRepository>();
        IPrivacyErasureReplayCheckpointRepository checkpointRepository =
            Substitute.For<IPrivacyErasureReplayCheckpointRepository>();
        IOutboxRepository outboxRepository = Substitute.For<IOutboxRepository>();
        IPrivacyErasureProviderWorkRepository providerWorkRepository =
            Substitute.For<IPrivacyErasureProviderWorkRepository>();
        IPrivacyErasureProviderLocatorProtector providerLocatorProtector =
            Substitute.For<IPrivacyErasureProviderLocatorProtector>();
        HybridCache cache = Substitute.For<HybridCache>();
        DateTime appliedAt = new(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);
        PrivacyErasureIntent intent = PrivacyErasureIntent.Record(
            Guid.CreateVersion7(), 1, PrivacyErasureSubjectKind.User, user.Id,
            PrivacyErasureReasonCode.AccountDeletion, 1,
            appliedAt, appliedAt);
        PrivacyErasureReplayCheckpoint checkpoint =
            PrivacyErasureReplayCheckpoint.Start(intent, appliedAt, Guid.CreateVersion7());
        PrivacyErasureIntent laterIntent = PrivacyErasureIntent.Record(
            Guid.CreateVersion7(), 2, PrivacyErasureSubjectKind.User, Guid.CreateVersion7(),
            PrivacyErasureReasonCode.AccountDeletion, 1,
            appliedAt, appliedAt);
        PrivacyErasureReplayCheckpoint laterCheckpoint = PrivacyErasureReplayCheckpoint.Advance(
            checkpoint, laterIntent, appliedAt, Guid.CreateVersion7());

        userRepository.GetById(user.Id).Returns(user);
        userRepository.Update(user).Returns(Task.CompletedTask);
        userPiiRepository.GetById(user.Id).Returns(userPii);
        tokenRepository.GetByUser(user.Id, Arg.Any<CancellationToken>()).Returns(tokens);
        erasureRepository.GetOwnedPrivateHomesAsync(user.Id, Arg.Any<CancellationToken>()).Returns([]);
        erasureRepository.GetEventLocationsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns([]);
        erasureRepository.GetUserActorsAsync(user.Id, Arg.Any<CancellationToken>()).Returns(actors);
        privacyErasureRepository.GetProviderCandidatesAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(providerCandidates ?? []);
        providerLocatorProtector.CurrentVersion.Returns(1);
        providerLocatorProtector.Protect(Arg.Any<string>(), Arg.Any<TimeSpan>()).Returns("protected-locator");
        providerWorkRepository.AddMissingAsync(
                Arg.Any<IReadOnlyCollection<PrivacyErasureProviderWork>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<IReadOnlyCollection<PrivacyErasureProviderWork>>().Count);
        checkpointRepository.GetLatestAsync(Arg.Any<CancellationToken>()).Returns(
            currentCheckpointAhead ? laterCheckpoint : null);
        checkpointRepository.AppendAsync(Arg.Any<PrivacyErasureReplayCheckpoint>(), Arg.Any<CancellationToken>())
            .Returns(checkpoint);
        IPrivacyErasureStateRepository stateRepository = Substitute.For<IPrivacyErasureStateRepository>();
        PrivacyErasureSaga saga = PrivacyErasureSaga.Start(
            intent,
            1,
            new byte[32],
            appliedAt.AddDays(1),
            appliedAt,
            Guid.CreateVersion7());
        stateRepository.GetByIntentAsync(intent.IntentId, Arg.Any<CancellationToken>()).Returns(saga);
        outboxRepository.CreateRange(Arg.Any<IReadOnlyCollection<OutboxMessage>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<IReadOnlyCollection<OutboxMessage>>()!.ToArray());

        var applier = new PrivacyErasureApplier(
            userRepository, userPiiRepository, tokenRepository, erasureRepository, privacyErasureRepository,
            aiConversationRepository, providerWorkRepository, providerLocatorProtector, checkpointRepository, stateRepository,
            outboxRepository, cache, TimeProvider.System,
            Substitute.For<ILogger<PrivacyErasureApplier>>(), Options.Create(new PrivacyErasureOptions()));
        var prepared = new PrivacyErasureApplier.PreparedErasure(
            new Dictionary<Guid, Guid>(),
            new Dictionary<Guid, Guid>(),
            checkpoint.Id,
            Guid.CreateVersion7(),
            appliedAt);
        return new Harness(
            applier,
            intent,
            prepared,
            userPiiRepository,
            tokenRepository,
            outboxRepository,
            checkpointRepository,
            stateRepository,
            privacyErasureRepository,
            aiConversationRepository,
            providerWorkRepository,
            providerLocatorProtector,
            saga);
    }

    private static User CreateUser(Guid userId) =>
        new()
        {
            Id = userId,
            Pii = new UserPii
            {
                UserId = userId,
                Email = "redacted@example.invalid",
                FirstName = "Redacted",
                LastName = "User"
            }
        };

    private static UserAuthenticationToken CreateToken(User user, Guid tenantId)
    {
        var tenant = new Tenant
        {
            Id = tenantId,
            FullName = "Redacted tenant",
            Slug = $"redacted-{tenantId:N}",
            TenantStatus = new TenantStatus { FullName = "Active", MasterCode = "ACTIVE", IsActiveState = true }
        };
        return new UserAuthenticationToken
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            User = user,
            TenantId = tenantId,
            Tenant = tenant,
            Provider = "atproto",
            SubjectDid = "did:example:redacted",
            SessionCiphertext = [1],
            EncryptionKeyId = "redacted",
            OAuthClientKeyId = "redacted"
        };
    }

    private static Actor CreateActor(Guid userId, string did)
    {
        Guid actorId = Guid.CreateVersion7();
        var actor = new Actor
        {
            Id = actorId,
            UserId = userId,
            ActorType = new ActorType { FullName = "User", MasterCode = "USER" },
            Pii = new ActorPii
            {
                ActorId = actorId,
                DisplayName = "redacted"
            }
        };
        actor.AtprotoIdentities.Add(new AtprotoIdentity
        {
            Id = Guid.CreateVersion7(),
            Did = did,
            ActorId = actorId,
            Actor = actor,
            Handle = "redacted.invalid",
            PdsHost = "https://pds.example.invalid",
            IsActive = true,
            LastResolvedAt = DateTime.UtcNow,
            ConcurrencyStamp = Guid.CreateVersion7()
        });
        return actor;
    }


    private sealed record Harness(
        PrivacyErasureApplier Applier,
        PrivacyErasureIntent Intent,
        PrivacyErasureApplier.PreparedErasure Prepared,
        IGenericRepository<UserPii, Guid> UserPiiRepository,
        IUserAuthenticationTokenRepository TokenRepository,
        IOutboxRepository OutboxRepository,
        IPrivacyErasureReplayCheckpointRepository CheckpointRepository,
        IPrivacyErasureStateRepository StateRepository,
        IUserPrivacyErasureRepository PrivacyErasureRepository,
        IAiConversationRepository AiConversationRepository,
        IPrivacyErasureProviderWorkRepository ProviderWorkRepository,
        IPrivacyErasureProviderLocatorProtector ProviderLocatorProtector,
        PrivacyErasureSaga Saga);
}
