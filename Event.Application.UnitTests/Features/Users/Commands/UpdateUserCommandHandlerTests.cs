// ABOUTME: Unit tests for the grouped user update command handler.
// ABOUTME: Verifies If-Match concurrency, single-save multi-repository updates, and cache invalidation.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.User;
using Explore.Application.Exceptions;
using Explore.Application.Features.Users.Handlers.Commands;
using Explore.Application.Features.Users.Requests.Commands;
using Explore.Application.Profiles;
using Explore.Application.Responses;
using Explore.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Users.Commands;

public sealed class UpdateUserCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IActorRepository _actorRepository = Substitute.For<IActorRepository>();
    private readonly IStorageObjectRepository _storageObjectRepository = Substitute.For<IStorageObjectRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly UpdateUserCommandHandler _handler;

    public UpdateUserCommandHandlerTests()
    {
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var operation = callInfo.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>();
                return operation!(CancellationToken.None);
            });

        var mapper = new MapperConfiguration(
            configuration => configuration.AddProfile<UserMappingProfile>(),
            NullLoggerFactory.Instance).CreateMapper();

        _handler = new UpdateUserCommandHandler(
            _userRepository,
            _actorRepository,
            _storageObjectRepository,
            _unitOfWork,
            mapper,
            _cache);
    }

    [Test]
    public async Task Handle_WhenExpectedConcurrencyStampIsStale_ThrowsConflictAndDoesNotSave()
    {
        var user = CreateUser();
        _userRepository.GetById(user.Id).Returns(user);

        await Assert.That(async () => await _handler.Handle(new UpdateUserCommand
        {
            UserId = user.Id,
            ExpectedConcurrencyStamp = Guid.CreateVersion7(),
            UpdateUserDto = new UpdateUserDto
            {
                Names = new UpdateUserNamesDto
                {
                    FirstName = "Updated",
                    LastName = "User"
                }
            }
        }, CancellationToken.None)).Throws<ConcurrencyConflictException>();

        await _userRepository.DidNotReceive().Update(Arg.Any<User>());
        await _actorRepository.DidNotReceive().Update(Arg.Any<Actor>());
        await _storageObjectRepository.DidNotReceive().Update(Arg.Any<StorageObject>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenProfileImageGroupIsPresent_UpdatesTrackedActorAndStorageWithSingleUserSave()
    {
        var user = CreateUser(actorId: Guid.CreateVersion7());
        var actor = new Actor
        {
            Id = user.ActorId!.Value,
            ActorTypeId = 1,
            ActorType = null!,
            Tenant = null!,
            Pii = new ActorPii
            {
                ActorId = user.ActorId.Value,
                DisplayName = "User",
                Handle = "user"
            }
        };
        var storageObject = new StorageObject
        {
            Id = Guid.CreateVersion7(),
            FileType = null!,
            Uri = "https://cdn.example.test/profiles/user.png",
            ObjectKey = "profiles/user.png",
            Provider = "local",
            FullName = "user.png",
            SafeDisplayName = "user.png",
            Extension = ".png",
            Visibility = "public",
            Purpose = "profile",
            LifecycleState = "active",
            Tenant = null!
        };

        _userRepository.GetById(user.Id).Returns(user);
        _actorRepository.GetById(actor.Id).Returns(actor);
        _storageObjectRepository.GetById(storageObject.Id).Returns(storageObject);

        var result = await _handler.Handle(new UpdateUserCommand
        {
            UserId = user.Id,
            ExpectedConcurrencyStamp = user.ConcurrencyStamp,
            UpdateUserDto = new UpdateUserDto
            {
                ProfileImage = new UpdateUserProfileImageDto
                {
                    ProfilePictureId = storageObject.Id
                }
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(actor.ProfilePictureId).IsEqualTo(storageObject.Id);
        await Assert.That(storageObject.ActorId).IsEqualTo(actor.Id);
        await _userRepository.Received(1).Update(user);
        await _actorRepository.DidNotReceive().Update(Arg.Any<Actor>());
        await _storageObjectRepository.DidNotReceive().Update(Arg.Any<StorageObject>());
        await _cache.Received(1).RemoveAsync($"user:detail:{user.Id}", Arg.Any<CancellationToken>());
    }

    private static User CreateUser(Guid? actorId = null)
    {
        var id = Guid.CreateVersion7();
        return new User
        {
            Id = id,
            ActorId = actorId,
            ConcurrencyStamp = Guid.CreateVersion7(),
            Pii = new UserPii
            {
                UserId = id,
                Email = "user@example.com",
                FirstName = "Existing",
                LastName = "User"
            }
        };
    }
}
