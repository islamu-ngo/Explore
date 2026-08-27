// ABOUTME: Unit tests for user deletion and linked PII cleanup behavior.
// ABOUTME: Proves account deletion removes user PII while anonymizing actor identity.

using System;
using Event.Application.UnitTests.Common;
using Explore.Application.Configuration;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.PrivacyErasure;
using Explore.Application.Contracts.Services;
using Explore.Application.Exceptions;
using Explore.Application.Features.Users.Handlers.Commands;
using Explore.Application.Features.Users.Requests.Commands;
using Explore.Application.Services;
using Explore.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Users.Commands;

public class DeleteUserCommandHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly IGenericRepository<UserPii, Guid> _userPiiRepository;
    private readonly IUserAuthenticationTokenRepository _userAuthenticationTokenRepository;
    private readonly IGenericRepository<ActorPii, Guid> _actorPiiRepository;
    private readonly IUserLocationPrivacyErasureRepository _erasureRepository;
    private readonly HybridCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly DeleteUserCommandHandler _handler;

    public DeleteUserCommandHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _userPiiRepository = Substitute.For<IGenericRepository<UserPii, Guid>>();
        _userAuthenticationTokenRepository = Substitute.For<IUserAuthenticationTokenRepository>();
        _actorPiiRepository = Substitute.For<IGenericRepository<ActorPii, Guid>>();
        _erasureRepository = Substitute.For<IUserLocationPrivacyErasureRepository>();
        IPrivacyErasureReplayCheckpointRepository checkpointRepository =
            Substitute.For<IPrivacyErasureReplayCheckpointRepository>();
        IOutboxRepository outboxRepository = Substitute.For<IOutboxRepository>();
        IPrivacyErasureAuthority authority = Substitute.For<IPrivacyErasureAuthority>();
        _cache = Substitute.For<HybridCache>();
        _unitOfWork = new ImmediateUnitOfWork();
        PrivacyErasureIntent? retainedIntent = null;
        PrivacyErasureReplayCheckpoint? checkpoint = null;

        _erasureRepository
            .GetOwnedPrivateHomesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _erasureRepository
            .GetEventLocationsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _erasureRepository
            .GetUserActorsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
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
        authority
            .GetStateAsync(Arg.Any<CancellationToken>())
            .Returns(_ => new PrivacyErasureAuthorityState(
                retainedIntent?.AuthoritySequence ?? 0,
                0));
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
        IPrivacyErasureStateRepository stateRepository = Substitute.For<IPrivacyErasureStateRepository>();
        PrivacyErasureSaga? saga = null;
        stateRepository.GetBySubjectAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(_ => saga);
        stateRepository.GetByIntentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(_ => saga);
        stateRepository.AddSagaAsync(Arg.Any<PrivacyErasureSaga>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                saga = call.Arg<PrivacyErasureSaga>();
                return Task.CompletedTask;
            });
        var applier = new PrivacyErasureApplier(
            _userRepository,
            _userPiiRepository,
            _userAuthenticationTokenRepository,
            _erasureRepository,
            Substitute.For<IUserPrivacyErasureRepository>(),
            Substitute.For<IAiConversationRepository>(),
            Substitute.For<IPrivacyErasureProviderWorkRepository>(),
            Substitute.For<IPrivacyErasureProviderLocatorProtector>(),
            checkpointRepository,
            stateRepository,
            outboxRepository,
            _cache,
            TimeProvider.System,
            Substitute.For<ILogger<PrivacyErasureApplier>>(),
            Options.Create(new PrivacyErasureOptions()));

        IPrivacyErasureService service = new RetainedAuthorityPrivacyErasureWorkflow(
            checkpointRepository,
            stateRepository,
            authority,
            _unitOfWork,
            applier,
            Options.Create(new PrivacyErasureOptions()),
            TimeProvider.System);
        _handler = new DeleteUserCommandHandler(service);
    }

    [Test]
    public async Task Handle_WithExistingUser_ReturnsUnit()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new DeleteUserCommand { UserId = userId, IntentId = Guid.CreateVersion7() };

        var user = DataBuilder.User.Generate();
        user.Id = userId;

        _userRepository.GetById(userId).Returns(user);
        _userRepository.Update(user).Returns(Task.CompletedTask);
        _userPiiRepository.GetById(userId).Returns((UserPii?)null);
        _userAuthenticationTokenRepository.GetByUser(userId).Returns(new List<UserAuthenticationToken>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Receipt).IsNotNull();
        await _userRepository.Received(1).Update(Arg.Is<User>(deleted =>
            deleted == user && deleted.IsDeleted));
    }

    [Test]
    public async Task Handle_WithNonExistentUser_ReturnsIndistinguishableReceipt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new DeleteUserCommand { UserId = userId, IntentId = Guid.CreateVersion7() };

        _userRepository.GetById(userId).Returns((User?)null);
        _userPiiRepository.GetById(userId).Returns((UserPii?)null);
        _userAuthenticationTokenRepository.GetByUser(userId).Returns([]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Receipt).IsNotNull();
        await _userRepository.DidNotReceive().Update(Arg.Any<User>());
    }

    [Test]
    public async Task Handle_DeletesCorrectUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new DeleteUserCommand { UserId = userId, IntentId = Guid.CreateVersion7() };

        var user = DataBuilder.User.Generate();
        user.Id = userId;
        user.Email = "test@example.com";

        _userRepository.GetById(userId).Returns(user);
        _userRepository.Update(user).Returns(Task.CompletedTask);
        _userPiiRepository.GetById(userId).Returns((UserPii?)null);
        _userAuthenticationTokenRepository.GetByUser(userId).Returns(new List<UserAuthenticationToken>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Receipt).IsNotNull();
        await _userRepository.Received(1).Update(Arg.Is<User>(u =>
            u.Id == userId && u.IsDeleted));
    }

    [Test]
    public async Task Handle_AnonymizesActorPii_InsteadOfHardDeleting()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var command = new DeleteUserCommand { UserId = userId, IntentId = Guid.CreateVersion7() };

        var user = DataBuilder.User.Generate();
        user.Id = userId;

        var actorPii = new ActorPii
        {
            ActorId = actorId,
            DisplayName = "Real Name",
            ProfilePictureUri = "https://cdn.example.com/avatar.jpg"
        };
        var actor = new Actor
        {
            Id = actorId,
            UserId = userId,
            ActorType = new ActorType { FullName = "User", MasterCode = "USER" },
            Pii = actorPii
        };
        actor.AtprotoIdentities.Add(new AtprotoIdentity
        {
            Id = Guid.CreateVersion7(),
            Did = "did:plc:abc123",
            ActorId = actorId,
            Actor = actor,
            Handle = "user.bsky.social",
            PdsHost = "https://pds.example.test",
            IsActive = true,
            LastResolvedAt = DateTime.UtcNow,
            ConcurrencyStamp = Guid.CreateVersion7()
        });

        _userRepository.GetById(userId).Returns(user);
        _userRepository.Update(user).Returns(Task.CompletedTask);
        _userPiiRepository.GetById(userId).Returns((UserPii?)null);
        _userAuthenticationTokenRepository.GetByUser(userId).Returns(new List<UserAuthenticationToken>());
        _erasureRepository
            .GetUserActorsAsync(userId, Arg.Any<CancellationToken>())
            .Returns([actor]);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _actorPiiRepository.DidNotReceive().Delete(Arg.Any<ActorPii>());
        await Assert.That(actorPii.ActorId).IsEqualTo(actorId);
        await Assert.That(actor.UserId).IsNull();
        await Assert.That(actorPii.DisplayName).IsEqualTo("Deleted user");
        await Assert.That(actor.AtprotoIdentities.Single().IsDeleted).IsTrue();
        await Assert.That(actor.AtprotoIdentities.Single().Handle).IsNull();
        await Assert.That(actorPii.ProfilePictureUri).IsNull();
        await _erasureRepository.Received(1).SaveChangesAsync(
            Arg.Any<IReadOnlyCollection<EventLocationDisclosureAudit>>(),
            Arg.Any<CancellationToken>());
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
