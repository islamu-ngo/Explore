using AutoMapper;
using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Organization;
using Explore.Application.Features.Organizations.Handlers.Commands;
using Explore.Application.Features.Organizations.Requests.Commands;
using Explore.Domain;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Organizations.Commands;

public class CreateOrganizationCommandHandlerTests
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrganizationMemberRepository _organizationMemberRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;
    private readonly CreateOrganizationCommandHandler _handler;

    public CreateOrganizationCommandHandlerTests()
    {
        _organizationRepository = Substitute.For<IOrganizationRepository>();
        _organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        _actorRepository = Substitute.For<IActorRepository>();
        _storageObjectRepository = Substitute.For<IStorageObjectRepository>();
        _userContext = Substitute.For<IUserContext>();
        _mapper = Substitute.For<IMapper>();
        _tenantContext = Substitute.For<ITenantContext>();

        _handler = new CreateOrganizationCommandHandler(
            _organizationRepository,
            _organizationMemberRepository,
            _actorRepository,
            _storageObjectRepository,
            _userContext,
            _mapper,
            _tenantContext
        );
    }

    [Test]
    public async Task Handle_WithValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new CreateOrganizationCommand
        {
            OrganizationDto = new CreateOrganizationDto
            {
                FullName = "Test Organization",
                Email = "test@example.com",
                Country = "Belgium",
                City = "Brussels",
                Address = "123 Test Street",
                Postcode = 1000
            }
        };

        _tenantContext.TenantId.Returns(tenantId);
        _userContext.GetRequiredUserId().Returns(userId);

        // Mock Organization creation
        var organization = new Organization { Id = organizationId, FullName = "Test Organization" };
        _mapper.Map<Organization>(command.OrganizationDto).Returns(organization);
        _organizationRepository.Create(Arg.Any<Organization>()).Returns(organization);
        _organizationRepository.Update(Arg.Any<Organization>()).Returns(Task.CompletedTask);

        // Mock Actor creation
        var actor = new Actor { Id = actorId, DisplayName = "Test Organization" };
        _actorRepository.Create(Arg.Any<Actor>()).Returns(actor);

        // Mock OrganizationMember creation
        _organizationMemberRepository.Create(Arg.Any<OrganizationMember>()).Returns(new OrganizationMember { Id = Guid.NewGuid() });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(organizationId);
        await _organizationRepository.Received(1).Create(Arg.Any<Organization>());
        await _actorRepository.Received(1).Create(Arg.Any<Actor>());
        await _organizationMemberRepository.Received(1).Create(Arg.Any<OrganizationMember>());
    }

    [Test]
    public async Task Handle_CreatesOrganizationWithPendingStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new CreateOrganizationCommand
        {
            OrganizationDto = new CreateOrganizationDto
            {
                FullName = "Test Organization",
                Email = "test@example.com",
                Country = "Belgium",
                City = "Brussels",
                Address = "123 Test Street",
                Postcode = 1000
            }
        };

        _tenantContext.TenantId.Returns(tenantId);
        _userContext.GetRequiredUserId().Returns(userId);

        var organization = new Organization { Id = organizationId };
        _mapper.Map<Organization>(command.OrganizationDto).Returns(organization);
        _organizationRepository.Create(Arg.Any<Organization>()).Returns(organization);
        _organizationRepository.Update(Arg.Any<Organization>()).Returns(Task.CompletedTask);

        var actor = new Actor { Id = actorId, DisplayName = "Test Organization" };
        _actorRepository.Create(Arg.Any<Actor>()).Returns(actor);
        _organizationMemberRepository.Create(Arg.Any<OrganizationMember>()).Returns(new OrganizationMember { Id = Guid.NewGuid() });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        // Verify that pending status (enum value 1) was set
        await _organizationRepository.Received(1).Create(Arg.Is<Organization>(o => o.ApprovalStatusId == 1));
    }

    [Test]
    public async Task Handle_SetsTenantIdFromContext()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new CreateOrganizationCommand
        {
            OrganizationDto = new CreateOrganizationDto
            {
                FullName = "Test Organization",
                Email = "test@example.com",
                Country = "Belgium",
                City = "Brussels",
                Address = "123 Test Street",
                Postcode = 1000
            }
        };

        _tenantContext.TenantId.Returns(tenantId);
        _userContext.GetRequiredUserId().Returns(userId);

        var organization = new Organization { Id = organizationId };
        _mapper.Map<Organization>(command.OrganizationDto).Returns(organization);
        _organizationRepository.Create(Arg.Any<Organization>()).Returns(organization);
        _organizationRepository.Update(Arg.Any<Organization>()).Returns(Task.CompletedTask);

        var actor = new Actor { Id = actorId, DisplayName = "Test Organization" };
        _actorRepository.Create(Arg.Any<Actor>()).Returns(actor);
        _organizationMemberRepository.Create(Arg.Any<OrganizationMember>()).Returns(new OrganizationMember { Id = Guid.NewGuid() });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await _organizationRepository.Received(1).Create(Arg.Is<Organization>(o => o.TenantId == tenantId));
    }

    [Test]
    public async Task Handle_AddsCreatorAsMember()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new CreateOrganizationCommand
        {
            OrganizationDto = new CreateOrganizationDto
            {
                FullName = "Test Organization",
                Email = "test@example.com",
                Country = "Belgium",
                City = "Brussels",
                Address = "123 Test Street",
                Postcode = 1000
            }
        };

        _tenantContext.TenantId.Returns(tenantId);
        _userContext.GetRequiredUserId().Returns(userId);

        var organization = new Organization { Id = organizationId };
        _mapper.Map<Organization>(command.OrganizationDto).Returns(organization);
        _organizationRepository.Create(Arg.Any<Organization>()).Returns(organization);
        _organizationRepository.Update(Arg.Any<Organization>()).Returns(Task.CompletedTask);

        var actor = new Actor { Id = actorId, DisplayName = "Test Organization" };
        _actorRepository.Create(Arg.Any<Actor>()).Returns(actor);
        _organizationMemberRepository.Create(Arg.Any<OrganizationMember>()).Returns(new OrganizationMember { Id = Guid.NewGuid() });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await _organizationMemberRepository.Received(1).Create(
            Arg.Is<OrganizationMember>(m =>
                m.UserId == userId &&
                m.OrganizationId == organizationId));
    }
}
