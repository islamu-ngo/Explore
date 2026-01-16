using AutoMapper;
using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Application.Features.Users.Handlers.Commands;
using Explore.Application.Features.Users.Requests.Commands;
using Explore.Domain;
using MediatR;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Users.Commands;

public class DeleteUserCommandHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly DeleteUserCommandHandler _handler;

    public DeleteUserCommandHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _mapper = Substitute.For<IMapper>();
        _handler = new DeleteUserCommandHandler(_userRepository, _mapper);
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

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result).IsEqualTo(Unit.Value);
        await _userRepository.Received(1).Delete(Arg.Is<User>(u => u.Id == userId && u.Email == "test@example.com"));
    }
}
