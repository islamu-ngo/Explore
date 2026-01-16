using AutoMapper;
using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Actor;
using Explore.Application.Features.Actors.Handlers.Queries;
using Explore.Application.Features.Actors.Requests.Queries;
using Explore.Domain;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Actors.Queries;

public class GetActorDetailsRequestHandlerTests
{
    private readonly IActorRepository _actorRepository;
    private readonly IMapper _mapper;
    private readonly GetActorDetailsRequestHandler _handler;

    public GetActorDetailsRequestHandlerTests()
    {
        _actorRepository = Substitute.For<IActorRepository>();
        _mapper = Substitute.For<IMapper>();

        _handler = new GetActorDetailsRequestHandler(_actorRepository, _mapper);
    }

    [Test]
    public async Task Handle_WithExistingActor_ReturnsActorDto()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var request = new GetActorDetailsRequest { Id = actorId };

        var actor = DataBuilder.Actor.Generate();
        actor.Id = actorId;
        actor.DisplayName = "Test Actor";
        actor.Did = "did:plc:test123";

        var expectedDto = new ActorDto
        {
            Id = actorId,
            DisplayName = "Test Actor",
            Did = "did:plc:test123"
        };

        _actorRepository.GetActorWithDetails(actorId).Returns(actor);
        _mapper.Map<ActorDto>(actor).Returns(expectedDto);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Id).IsEqualTo(actorId);
        await Assert.That(result.DisplayName).IsEqualTo("Test Actor");
        await Assert.That(result.Did).IsEqualTo("did:plc:test123");
    }

    [Test]
    public async Task Handle_WithNonExistentActor_ReturnsNull()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var request = new GetActorDetailsRequest { Id = actorId };

        _actorRepository.GetActorWithDetails(actorId).Returns((Actor?)null);
        _mapper.Map<ActorDto>(Arg.Any<Actor?>()).Returns((ActorDto?)null);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Handle_ReturnsActorWithProfilePicture()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var profilePictureId = Guid.NewGuid();
        var request = new GetActorDetailsRequest { Id = actorId };

        var actor = DataBuilder.Actor.Generate();
        actor.Id = actorId;
        actor.ProfilePictureId = profilePictureId;
        actor.ProfilePicture = DataBuilder.StorageObject.Generate();
        actor.ProfilePicture.Id = profilePictureId;
        actor.ProfilePicture.Uri = "https://storage.example.com/image.jpg";

        var expectedDto = new ActorDto
        {
            Id = actorId,
            ProfilePictureId = profilePictureId,
            ProfilePictureUri = "https://storage.example.com/image.jpg"
        };

        _actorRepository.GetActorWithDetails(actorId).Returns(actor);
        _mapper.Map<ActorDto>(actor).Returns(expectedDto);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.ProfilePictureId).IsEqualTo(profilePictureId);
        await Assert.That(result.ProfilePictureUri).IsEqualTo("https://storage.example.com/image.jpg");
    }

    [Test]
    public async Task Handle_ReturnsActorWithActorType()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var actorTypeId = 1;
        var request = new GetActorDetailsRequest { Id = actorId };

        var actor = DataBuilder.Actor.Generate();
        actor.Id = actorId;
        actor.ActorTypeId = actorTypeId;
        actor.ActorType = new ActorType { Id = actorTypeId, FullName = "User", MasterCode = "USER" };

        var expectedDto = new ActorDto
        {
            Id = actorId,
            ActorTypeId = actorTypeId,
            ActorTypeFullName = "User"
        };

        _actorRepository.GetActorWithDetails(actorId).Returns(actor);
        _mapper.Map<ActorDto>(actor).Returns(expectedDto);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.ActorTypeId).IsEqualTo(actorTypeId);
        await Assert.That(result.ActorTypeFullName).IsEqualTo("User");
    }
}
