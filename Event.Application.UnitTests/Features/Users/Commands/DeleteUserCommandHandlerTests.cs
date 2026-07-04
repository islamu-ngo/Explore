// ABOUTME: Unit tests for user deletion and linked PII cleanup behavior.
// ABOUTME: Proves account deletion removes user PII while anonymizing actor identity.

using System;
using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Application.Features.Users.Handlers.Commands;
using Explore.Application.Features.Users.Requests.Commands;
using Explore.Domain;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Users.Commands;

public class DeleteUserCommandHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly IGenericRepository<UserPii, Guid> _userPiiRepository;
    private readonly IUserAuthenticationTokenRepository _userAuthenticationTokenRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IGenericRepository<ActorPii, Guid> _actorPiiRepository;
    private readonly HybridCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly DeleteUserCommandHandler _handler;

    public DeleteUserCommandHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _userPiiRepository = Substitute.For<IGenericRepository<UserPii, Guid>>();
        _userAuthenticationTokenRepository = Substitute.For<IUserAuthenticationTokenRepository>();
        _actorRepository = Substitute.For<IActorRepository>();
        _actorPiiRepository = Substitute.For<IGenericRepository<ActorPii, Guid>>();
        _cache = Substitute.For<HybridCache>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        // Execute the lambda so inner repo logic runs in tests
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var op = callInfo.Arg<Func<CancellationToken, Task>>();
                return op(CancellationToken.None);
            });

        _handler = new DeleteUserCommandHandler(
            _userRepository,
            _userPiiRepository,
            _userAuthenticationTokenRepository,
            _actorRepository,
            _actorPiiRepository,
            _cache,
            _unitOfWork);
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
        _userRepository.Delete(user).Returns(Task.CompletedTask);
        _userPiiRepository.GetById(userId).Returns((UserPii?)null);
        _userAuthenticationTokenRepository.GetByUser(userId).Returns(new List<UserAuthenticationToken>());
        _actorRepository.GetActorByUserId(userId).Returns((Actor?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result).IsEqualTo(Unit.Value);
        await _userRepository.Received(1).Delete(user);
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
        await _userRepository.DidNotReceive().Delete(Arg.Any<User>());
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
        _userRepository.Delete(user).Returns(Task.CompletedTask);
        _userPiiRepository.GetById(userId).Returns((UserPii?)null);
        _userAuthenticationTokenRepository.GetByUser(userId).Returns(new List<UserAuthenticationToken>());
        _actorRepository.GetActorByUserId(userId).Returns((Actor?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result).IsEqualTo(Unit.Value);
        await _userRepository.Received(1).Delete(Arg.Is<User>(u =>
            u.Id == userId));
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
            Pii = new ActorPii { DisplayName = "Real Name" }
        };
        var actorPii = new ActorPii
        {
            ActorId = actorId,
            DisplayName = "Real Name",
            Did = "did:plc:abc123",
            Handle = "user.bsky.social",
            ProfilePictureUri = "https://cdn.example.com/avatar.jpg"
        };

        _userRepository.GetById(userId).Returns(user);
        _userRepository.Delete(user).Returns(Task.CompletedTask);
        _userPiiRepository.GetById(userId).Returns((UserPii?)null);
        _userAuthenticationTokenRepository.GetByUser(userId).Returns(new List<UserAuthenticationToken>());
        _actorRepository.GetActorByUserId(userId).Returns(actor);
        _actorPiiRepository.GetById(actorId).Returns(actorPii);
        _actorPiiRepository.Update(Arg.Any<ActorPii>()).Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _actorPiiRepository.DidNotReceive().Delete(Arg.Any<ActorPii>());
        await _actorPiiRepository.Received(1).Update(Arg.Is<ActorPii>(a =>
            a.ActorId == actorId
            && a.DisplayName.StartsWith("DeletedUser")
            && a.Did == null
            && a.Handle == null
            && a.ProfilePictureUri == null));
    }
}
