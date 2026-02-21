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
    private readonly DeleteUserCommandHandler _handler;

    public DeleteUserCommandHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _userPiiRepository = Substitute.For<IGenericRepository<UserPii, Guid>>();
        _userAuthenticationTokenRepository = Substitute.For<IUserAuthenticationTokenRepository>();
        _actorRepository = Substitute.For<IActorRepository>();
        _actorPiiRepository = Substitute.For<IGenericRepository<ActorPii, Guid>>();
        _cache = Substitute.For<HybridCache>();
        _handler = new DeleteUserCommandHandler(
            _userRepository,
            _userPiiRepository,
            _userAuthenticationTokenRepository,
            _actorRepository,
            _actorPiiRepository,
            _cache);
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
}
