// ABOUTME: Pins the current location-only account-deletion behavior before platform erasure expands it.
// ABOUTME: Records current deletion, anonymization, and ATProto cleanup-ordering gaps.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Services;
using Explore.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
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
        await Assert.That(actor.Pii!.DisplayName).StartsWith("DeletedUser");
        await Assert.That(actor.Pii.Did).IsNull();
        await Assert.That(actor.Pii.Handle).IsNull();
    }

    [Test]
    public async Task CurrentAtprotoIdentifierIsClearedBeforeAnyProviderCleanupWorkExists()
    {
        Guid userId = Guid.CreateVersion7();
        Actor actor = CreateActor(userId, "did:example:redacted");
        Harness harness = CreateHarness(CreateUser(userId), null, [], [actor]);

        await harness.Applier.ApplyInCurrentTransactionAsync(harness.Intent, harness.Prepared, CancellationToken.None);

        await Assert.That(actor.Pii!.Did).IsNull();
        await harness.OutboxRepository.Received(1).CreateRange(
            Arg.Is<IReadOnlyCollection<OutboxMessage>>(messages => messages != null && messages.Count == 0),
            Arg.Any<CancellationToken>());
    }

    private static Harness CreateHarness(
        User user,
        UserPii? userPii,
        List<UserAuthenticationToken> tokens,
        IReadOnlyList<Actor> actors)
    {
        IUserRepository userRepository = Substitute.For<IUserRepository>();
        IGenericRepository<UserPii, Guid> userPiiRepository = Substitute.For<IGenericRepository<UserPii, Guid>>();
        IUserAuthenticationTokenRepository tokenRepository = Substitute.For<IUserAuthenticationTokenRepository>();
        IUserLocationPrivacyErasureRepository erasureRepository = Substitute.For<IUserLocationPrivacyErasureRepository>();
        IPrivacyErasureReplayCheckpointRepository checkpointRepository =
            Substitute.For<IPrivacyErasureReplayCheckpointRepository>();
        IPrivacyErasureLedgerRepository ledgerRepository =
            Substitute.For<IPrivacyErasureLedgerRepository>();
        IOutboxRepository outboxRepository = Substitute.For<IOutboxRepository>();
        HybridCache cache = Substitute.For<HybridCache>();
        DateTime appliedAt = new(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);
        PrivacyErasureIntent intent = PrivacyErasureIntent.Record(
            Guid.CreateVersion7(), 1, PrivacyErasureSubjectKind.User, user.Id,
            PrivacyErasureReasonCode.AccountDeletion, 1,
            appliedAt, appliedAt);
        PrivacyErasureReplayCheckpoint checkpoint =
            PrivacyErasureReplayCheckpoint.Start(intent, appliedAt, Guid.CreateVersion7());

        userRepository.GetById(user.Id).Returns(user);
        userRepository.Update(user).Returns(Task.CompletedTask);
        userPiiRepository.GetById(user.Id).Returns(userPii);
        tokenRepository.GetByUser(user.Id, Arg.Any<CancellationToken>()).Returns(tokens);
        erasureRepository.GetOwnedPrivateHomesAsync(user.Id, Arg.Any<CancellationToken>()).Returns([]);
        erasureRepository.GetEventLocationsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns([]);
        erasureRepository.GetUserActorsAsync(user.Id, Arg.Any<CancellationToken>()).Returns(actors);
        checkpointRepository.GetLatestAsync(Arg.Any<CancellationToken>()).Returns((PrivacyErasureReplayCheckpoint?)null);
        checkpointRepository.AppendAsync(Arg.Any<PrivacyErasureReplayCheckpoint>(), Arg.Any<CancellationToken>())
            .Returns(checkpoint);
        ledgerRepository.AppendAsync(intent, Arg.Any<CancellationToken>()).Returns(intent);
        outboxRepository.CreateRange(Arg.Any<IReadOnlyCollection<OutboxMessage>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<IReadOnlyCollection<OutboxMessage>>()!.ToArray());

        var applier = new PrivacyErasureApplier(
            userRepository, userPiiRepository, tokenRepository, erasureRepository, checkpointRepository,
            ledgerRepository, outboxRepository, cache, TimeProvider.System,
            Substitute.For<ILogger<PrivacyErasureApplier>>());
        var prepared = new PrivacyErasureApplier.PreparedErasure(
            new Dictionary<Guid, Guid>(), new Dictionary<Guid, Guid>(), checkpoint.Id, appliedAt);
        return new Harness(applier, intent, prepared, userPiiRepository, tokenRepository, outboxRepository);
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
        var tenant = new Tenant
        {
            Id = Guid.CreateVersion7(),
            FullName = "Redacted tenant",
            Slug = $"redacted-{actorId:N}",
            TenantStatus = new TenantStatus { FullName = "Active", MasterCode = "ACTIVE", IsActiveState = true }
        };
        return new Actor
        {
            Id = actorId,
            UserId = userId,
            TenantId = tenant.Id,
            Tenant = tenant,
            ActorType = new ActorType { FullName = "User", MasterCode = "USER" },
            Pii = new ActorPii
            {
                ActorId = actorId,
                DisplayName = "redacted",
                Did = did,
                Handle = "redacted.invalid"
            }
        };
    }

    private sealed record Harness(
        PrivacyErasureApplier Applier,
        PrivacyErasureIntent Intent,
        PrivacyErasureApplier.PreparedErasure Prepared,
        IGenericRepository<UserPii, Guid> UserPiiRepository,
        IUserAuthenticationTokenRepository TokenRepository,
        IOutboxRepository OutboxRepository);
}
