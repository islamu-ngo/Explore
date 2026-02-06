using AutoMapper;
using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Actor;
using Explore.Application.Features.Actors.Handlers.Commands;
using Explore.Application.Features.Actors.Requests.Commands;
using Explore.Domain;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Actors.Commands;

public class CreateActorCommandHandlerTests
{
    private readonly IActorRepository _actorRepository;
    private readonly IActorTypeRepository _actorTypeRepository;
    private readonly IDidCustodyTypeRepository _didCustodyTypeRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;
    private readonly CreateActorCommandHandler _handler;

    public CreateActorCommandHandlerTests()
    {
        _actorRepository = Substitute.For<IActorRepository>();
        _actorTypeRepository = Substitute.For<IActorTypeRepository>();
        _didCustodyTypeRepository = Substitute.For<IDidCustodyTypeRepository>();
        _storageObjectRepository = Substitute.For<IStorageObjectRepository>();
        _tenantRepository = Substitute.For<ITenantRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _organizationRepository = Substitute.For<IOrganizationRepository>();
        _tenantContext = Substitute.For<ITenantContext>();
        _mapper = Substitute.For<IMapper>();

        _handler = new CreateActorCommandHandler(
            _actorRepository,
            _actorTypeRepository,
            _didCustodyTypeRepository,
            _storageObjectRepository,
            _tenantRepository,
            _userRepository,
            _organizationRepository,
            _tenantContext,
            _mapper
        );
    }

    [Test]
    public async Task Handle_WithValidUserActor_ReturnsSuccessResponse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var actorTypeId = 1; // User type

        var command = new CreateActorCommand
        {
            ActorDto = new CreateActorDto
            {
                ActorTypeId = actorTypeId,
                UserId = userId,
                DisplayName = "Test User Actor",
                TenantId = tenantId
            }
        };

        _tenantContext.TenantId.Returns(tenantId);

        // Mock validations
        _actorTypeRepository.Exists(actorTypeId).Returns(true);
        _tenantRepository.Exists(tenantId).Returns(true);
        _userRepository.Exists(userId).Returns(true);

        // Mock actor creation
        var actor = new Actor { Id = actorId, DisplayName = "Test User Actor" };
        _mapper.Map<Actor>(command.ActorDto).Returns(actor);
        _actorRepository.Create(Arg.Any<Actor>()).Returns(actor);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(actorId);
        await _actorRepository.Received(1).Create(Arg.Any<Actor>());
    }

    [Test]
    public async Task Handle_WithValidOrganizationActor_ReturnsSuccessResponse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var actorTypeId = 2; // Organization type

        var command = new CreateActorCommand
        {
            ActorDto = new CreateActorDto
            {
                ActorTypeId = actorTypeId,
                OrganizationId = organizationId,
                DisplayName = "Test Organization Actor",
                TenantId = tenantId
            }
        };

        _tenantContext.TenantId.Returns(tenantId);

        // Mock validations
        _actorTypeRepository.Exists(actorTypeId).Returns(true);
        _tenantRepository.Exists(tenantId).Returns(true);
        _organizationRepository.Exists(organizationId).Returns(true);

        // Mock actor creation
        var actor = new Actor { Id = actorId, DisplayName = "Test Organization Actor" };
        _mapper.Map<Actor>(command.ActorDto).Returns(actor);
        _actorRepository.Create(Arg.Any<Actor>()).Returns(actor);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(actorId);
        await _actorRepository.Received(1).Create(Arg.Any<Actor>());
    }

    [Test]
    public async Task Handle_WithInvalidActorType_ReturnsFailedResponse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var invalidActorTypeId = 999;

        var command = new CreateActorCommand
        {
            ActorDto = new CreateActorDto
            {
                ActorTypeId = invalidActorTypeId,
                UserId = userId,
                DisplayName = "Test Actor",
                TenantId = tenantId
            }
        };

        _tenantContext.TenantId.Returns(tenantId);
        _actorTypeRepository.Exists(invalidActorTypeId).Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await _actorRepository.DidNotReceive().Create(Arg.Any<Actor>());
    }

    [Test]
    public async Task Handle_WithBothUserAndOrganization_ReturnsValidationError()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        var command = new CreateActorCommand
        {
            ActorDto = new CreateActorDto
            {
                ActorTypeId = 1,
                UserId = userId,
                OrganizationId = organizationId, // Both user and org set - invalid
                DisplayName = "Test Actor",
                TenantId = tenantId
            }
        };

        _tenantContext.TenantId.Returns(tenantId);
        _actorTypeRepository.Exists(1).Returns(true);
        _tenantRepository.Exists(tenantId).Returns(true);
        _userRepository.Exists(userId).Returns(true);
        _organizationRepository.Exists(organizationId).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await _actorRepository.DidNotReceive().Create(Arg.Any<Actor>());
    }

    [Test]
    public async Task Handle_WithNeitherUserNorOrganization_ReturnsValidationError()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        var command = new CreateActorCommand
        {
            ActorDto = new CreateActorDto
            {
                ActorTypeId = 1,
                UserId = null, // Neither user nor org set - invalid
                OrganizationId = null,
                DisplayName = "Test Actor",
                TenantId = tenantId
            }
        };

        _tenantContext.TenantId.Returns(tenantId);
        _actorTypeRepository.Exists(1).Returns(true);
        _tenantRepository.Exists(tenantId).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await _actorRepository.DidNotReceive().Create(Arg.Any<Actor>());
    }

    [Test]
    public async Task Handle_WithMissingDisplayName_ReturnsValidationError()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var command = new CreateActorCommand
        {
            ActorDto = new CreateActorDto
            {
                ActorTypeId = 1,
                UserId = userId,
                DisplayName = "", // Missing required field
                TenantId = tenantId
            }
        };

        _tenantContext.TenantId.Returns(tenantId);
        _actorTypeRepository.Exists(1).Returns(true);
        _tenantRepository.Exists(tenantId).Returns(true);
        _userRepository.Exists(userId).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await _actorRepository.DidNotReceive().Create(Arg.Any<Actor>());
    }

    [Test]
    public async Task Handle_SetsTenantIdFromContext()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var command = new CreateActorCommand
        {
            ActorDto = new CreateActorDto
            {
                ActorTypeId = 1,
                UserId = userId,
                DisplayName = "Test Actor",
                TenantId = Guid.NewGuid() // Different tenant in DTO
            }
        };

        _tenantContext.TenantId.Returns(tenantId);
        _actorTypeRepository.Exists(1).Returns(true);
        _tenantRepository.Exists(Arg.Any<Guid>()).Returns(true);
        _userRepository.Exists(userId).Returns(true);

        var actor = new Actor { Id = actorId, DisplayName = "Test Actor" };
        _mapper.Map<Actor>(command.ActorDto).Returns(actor);
        _actorRepository.Create(Arg.Any<Actor>()).Returns(actor);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        // Verify that the tenant context's tenant ID is used
        await _actorRepository.Received(1).Create(Arg.Is<Actor>(a => a.TenantId == tenantId));
    }
}
