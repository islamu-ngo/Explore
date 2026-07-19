// ABOUTME: Unit tests for user deletion and linked PII cleanup behavior.
// ABOUTME: Proves account deletion removes user PII while anonymizing actor identity.

using System;
using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.LocationPrivacy;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Exceptions;
using Explore.Application.Features.Users.Handlers.Commands;
using Explore.Application.Features.Users.Requests.Commands;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
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
    private readonly IGlobalLocationPrivacyErasureRepository _erasureRepository;
    private readonly HybridCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly DeleteUserCommandHandler _handler;

    public DeleteUserCommandHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _userPiiRepository = Substitute.For<IGenericRepository<UserPii, Guid>>();
        _userAuthenticationTokenRepository = Substitute.For<IUserAuthenticationTokenRepository>();
        _actorPiiRepository = Substitute.For<IGenericRepository<ActorPii, Guid>>();
        _erasureRepository = Substitute.For<IGlobalLocationPrivacyErasureRepository>();
        ILocationPrivacyErasureReplayCheckpointRepository checkpointRepository =
            Substitute.For<ILocationPrivacyErasureReplayCheckpointRepository>();
        IOutboxRepository outboxRepository = Substitute.For<IOutboxRepository>();
        ILocationPrivacyErasureAuthority authority = Substitute.For<ILocationPrivacyErasureAuthority>();
        _cache = Substitute.For<HybridCache>();
        _unitOfWork = new ImmediateUnitOfWork();
        LocationPrivacyErasureAuthorityIntent? retainedIntent = null;
        LocationPrivacyErasureReplayCheckpoint? checkpoint = null;

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
            .AppendAsync(Arg.Any<LocationPrivacyErasureIntent>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                LocationPrivacyErasureIntent intent = call.Arg<LocationPrivacyErasureIntent>();
                DateTime recordedAt = DateTime.UtcNow;
                retainedIntent = LocationPrivacyErasureAuthorityIntent.Record(
                    intent.IntentId,
                    1,
                    intent.OwnerUserId,
                    intent.LocationIds,
                    LocationPrivacyErasureReasonEnum.AccountDeletion,
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
                Arg.Any<LocationPrivacyErasureReplayCheckpoint>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                checkpoint = call.Arg<LocationPrivacyErasureReplayCheckpoint>();
                return checkpoint;
            });
        outboxRepository
            .CreateRange(Arg.Any<IReadOnlyCollection<OutboxMessage>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<IReadOnlyCollection<OutboxMessage>>().ToArray());

        IGlobalLocationPrivacyErasureService service = new GlobalLocationPrivacyErasureService(
            _userRepository,
            _userPiiRepository,
            _userAuthenticationTokenRepository,
            _erasureRepository,
            checkpointRepository,
            outboxRepository,
            authority,
            _unitOfWork,
            _cache,
            TimeProvider.System,
            Substitute.For<ILogger<GlobalLocationPrivacyErasureService>>());
        _handler = new DeleteUserCommandHandler(service);
    }

    [Test]
    public async Task Handle_WithExistingUser_ReturnsUnit()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new DeleteUserCommand { UserId = userId };

        var user = DataBuilder.User.Generate();
        user.Id = userId;

        _userRepository.GetById(userId).Returns(user);
        _userRepository.Update(user).Returns(Task.CompletedTask);
        _userPiiRepository.GetById(userId).Returns((UserPii?)null);
        _userAuthenticationTokenRepository.GetByUser(userId).Returns(new List<UserAuthenticationToken>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result).IsEqualTo(Unit.Value);
        await _userRepository.Received(1).Update(Arg.Is<User>(deleted =>
            deleted == user && deleted.IsDeleted));
    }

    [Test]
    public async Task Handle_WithNonExistentUser_ThrowsNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new DeleteUserCommand { UserId = userId };

        _userRepository.GetById(userId).Returns((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            async () => await _handler.Handle(command, CancellationToken.None));
        await _userRepository.DidNotReceive().Update(Arg.Any<User>());
    }

    [Test]
    public async Task Handle_DeletesCorrectUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new DeleteUserCommand { UserId = userId };

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
        await Assert.That(result).IsEqualTo(Unit.Value);
        await _userRepository.Received(1).Update(Arg.Is<User>(u =>
            u.Id == userId && u.IsDeleted));
    }

    [Test]
    public async Task Handle_AnonymizesActorPii_InsteadOfHardDeleting()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var command = new DeleteUserCommand { UserId = userId };

        var user = DataBuilder.User.Generate();
        user.Id = userId;

        var actorPii = new ActorPii
        {
            ActorId = actorId,
            DisplayName = "Real Name",
            Did = "did:plc:abc123",
            Handle = "user.bsky.social",
            ProfilePictureUri = "https://cdn.example.com/avatar.jpg"
        };
        var actor = new Actor
        {
            Id = actorId,
            UserId = userId,
            ActorType = new ActorType { FullName = "User", MasterCode = "USER" },
            Tenant = new Tenant
            {
                FullName = "Test",
                Slug = "test",
                TenantStatus = new TenantStatus { FullName = "Active", MasterCode = "ACTIVE", IsActiveState = true }
            },
            Pii = actorPii
        };

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
        await Assert.That(actorPii.DisplayName).StartsWith("DeletedUser");
        await Assert.That(actorPii.Did).IsNull();
        await Assert.That(actorPii.Handle).IsNull();
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
