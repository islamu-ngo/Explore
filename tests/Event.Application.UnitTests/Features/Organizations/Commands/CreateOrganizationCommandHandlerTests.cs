// ABOUTME: Unit tests for organization creation command behavior.
// ABOUTME: Verifies approval status, tenant assignment, membership, and create DTO mapping.

using System.Diagnostics.Metrics;
using System.Globalization;
using AutoMapper;
using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Organization;
using Explore.Application.Features.Organizations.Handlers.Commands;
using Explore.Application.Features.Organizations.Requests.Commands;
using Explore.Application.Profiles;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Organizations.Commands;

public class CreateOrganizationCommandHandlerTests
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrganizationTenantRepository _organizationTenantRepository;
    private readonly IOrganizationMemberRepository _organizationMemberRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IAdminContext _adminContext;
    private readonly IAdminCacheInvalidator _adminCacheInvalidator;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CreateOrganizationCommandHandler _handler;

    public CreateOrganizationCommandHandlerTests()
    {
        _organizationRepository = Substitute.For<IOrganizationRepository>();
        _organizationTenantRepository = Substitute.For<IOrganizationTenantRepository>();
        _organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        _actorRepository = Substitute.For<IActorRepository>();
        _storageObjectRepository = Substitute.For<IStorageObjectRepository>();
        _adminContext = Substitute.For<IAdminContext>();
        _adminCacheInvalidator = Substitute.For<IAdminCacheInvalidator>();
        _mapper = Substitute.For<IMapper>();
        _tenantContext = Substitute.For<ITenantContext>();
        _cache = Substitute.For<HybridCache>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));
        _organizationTenantRepository.Create(Arg.Any<OrganizationTenant>())
            .Returns(call => call.Arg<OrganizationTenant>());
        _adminContext.IsTenantAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter("test"));

        _handler = new CreateOrganizationCommandHandler(
            _organizationRepository,
            _organizationTenantRepository,
            _organizationMemberRepository,
            _actorRepository,
            _storageObjectRepository,
            _adminContext,
            _adminCacheInvalidator,
            _mapper,
            _tenantContext,
            _cache,
            new BusinessMetrics(meterFactory),
            _unitOfWork
        );
    }

    [Test]
    public async Task Mapping_CreateOrganizationDto_InitializesPiiContactFields()
    {
        // Arrange
        var config = new MapperConfiguration(
#if USE_COMMERCIAL_LUCKYPENNY_LIBS
            cfg => cfg.AddProfile<OrganizationMappingProfile>(),
            NullLoggerFactory.Instance);
#else
            cfg => cfg.AddProfile<OrganizationMappingProfile>());
#endif
        var mapper = config.CreateMapper();
        var dto = new CreateOrganizationDto
        {
            FullName = "Mapped Organization",
            Email = "mapped@example.com",
            Country = "Belgium",
            City = "Brussels",
            Address = "Mapped Street 1",
            Postcode = 1000
        };

        // Act
        var organization = mapper.Map<Organization>(dto);

        // Assert
        await Assert.That(organization.Pii).IsNotNull();
        await Assert.That(organization.FullName).IsEqualTo(dto.FullName);
        await Assert.That(organization.Email).IsEqualTo(dto.Email);
        await Assert.That(organization.Country).IsEqualTo(dto.Country);
        await Assert.That(organization.City).IsEqualTo(dto.City);
        await Assert.That(organization.Address).IsEqualTo(dto.Address);
        await Assert.That(organization.Postcode).IsEqualTo(dto.Postcode.ToString(CultureInfo.InvariantCulture));
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
            CreatorUserId = userId,
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
        // Mock Organization creation
        var organization = new Organization
        {
            Id = organizationId,
            Pii = new OrganizationPii { FullName = "Test Organization" }
        };
        _mapper.Map<Organization>(command.OrganizationDto).Returns(organization);
        _organizationRepository.Create(Arg.Any<Organization>()).Returns(organization);
        _organizationRepository.Update(Arg.Any<Organization>()).Returns(Task.CompletedTask);

        // Mock Actor creation
        var actor = new Actor { Id = actorId, Pii = new ActorPii { DisplayName = "Test Organization" }, ActorType = null! };
        _actorRepository.Create(Arg.Any<Actor>()).Returns(actor);

        // Mock OrganizationMember creation
        _organizationMemberRepository.Create(Arg.Any<OrganizationMember>()).Returns(
            new OrganizationMember { Id = Guid.NewGuid(), OrganizationTenant = null!, User = null!, Role = null!, Tenant = null! });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Id).IsEqualTo(organizationId);
        await _organizationRepository.Received(1).Create(Arg.Any<Organization>());
        await _actorRepository.Received(1).Create(Arg.Any<Actor>());
        await _organizationMemberRepository.Received(1).Create(Arg.Any<OrganizationMember>());
        _adminCacheInvalidator.Received(1).InvalidateUser(userId);
    }

    [Test]
    public async Task Handle_WhenProfileImageIsCrossTenant_RejectsBeforeTransactionOrPersistence()
    {
        var tenantId = Guid.NewGuid();
        var imageId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);
        _storageObjectRepository.GetById(imageId).Returns(new StorageObject
        {
            Id = imageId,
            TenantId = Guid.NewGuid(),
            Tenant = null!,
            FileType = null!,
            Uri = "storage://organization.png",
            Provider = "local",
            FullName = "organization.png",
            SafeDisplayName = "organization.png",
            Extension = "png",
            ContentType = "image/png",
            Purpose = StorageObjectPurposes.ProfileImage,
            Visibility = StorageObjectVisibilities.PublicImage,
            LifecycleState = StorageObjectLifecycleStates.Active
        });
        var command = new CreateOrganizationCommand
        {
            CreatorUserId = Guid.NewGuid(),
            OrganizationDto = new CreateOrganizationDto
            {
                FullName = "Unsafe image organization",
                Email = "unsafe@example.test",
                Country = "Belgium",
                City = "Brussels",
                Address = "Unsafe Street",
                Postcode = 1000,
                ProfilePictureId = imageId
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await _adminContext.DidNotReceive().IsTenantAdminAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task>>(),
            Arg.Any<CancellationToken>());
        await _organizationRepository.DidNotReceive().Create(Arg.Any<Organization>());
        await _actorRepository.DidNotReceive().Create(Arg.Any<Actor>());
        await _organizationMemberRepository.DidNotReceive().Create(Arg.Any<OrganizationMember>());
    }

    [Test]
    public async Task Handle_WhenCreatorIsNotTenantAdmin_CreatesOrganizationWithPendingStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new CreateOrganizationCommand
        {
            CreatorUserId = userId,
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
        _adminContext.IsTenantAdminAsync(tenantId, Arg.Any<CancellationToken>()).Returns(false);

        var organization = new Organization
        {
            Id = organizationId,
            Pii = new OrganizationPii { FullName = string.Empty }
        };
        _mapper.Map<Organization>(command.OrganizationDto).Returns(organization);
        _organizationRepository.Create(Arg.Any<Organization>()).Returns(organization);
        _organizationRepository.Update(Arg.Any<Organization>()).Returns(Task.CompletedTask);

        var actor = new Actor { Id = actorId, Pii = new ActorPii { DisplayName = "Test Organization" }, ActorType = null! };
        _actorRepository.Create(Arg.Any<Actor>()).Returns(actor);
        _organizationMemberRepository.Create(Arg.Any<OrganizationMember>()).Returns(
            new OrganizationMember { Id = Guid.NewGuid(), OrganizationTenant = null!, User = null!, Role = null!, Tenant = null! });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await _organizationTenantRepository.Received(1).Create(Arg.Is<OrganizationTenant>(o =>
            o.ApprovalStatusId == (int)ApprovalStatusEnum.Pending
            && o.ApprovedAt == null
            && o.ApprovedBy == null));
    }

    [Test]
    public async Task Handle_WhenCreatorIsTenantAdmin_CreatesOrganizationWithApprovedStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new CreateOrganizationCommand
        {
            CreatorUserId = userId,
            OrganizationDto = new CreateOrganizationDto
            {
                FullName = "Tenant Admin Organization",
                Email = "admin-org@example.com",
                Country = "Belgium",
                City = "Brussels",
                Address = "123 Test Street",
                Postcode = 1000
            }
        };

        _tenantContext.TenantId.Returns(tenantId);
        _adminContext.IsTenantAdminAsync(tenantId, Arg.Any<CancellationToken>()).Returns(true);

        var organization = new Organization
        {
            Id = organizationId,
            Pii = new OrganizationPii { FullName = string.Empty }
        };
        _mapper.Map<Organization>(command.OrganizationDto).Returns(organization);
        _organizationRepository.Create(Arg.Any<Organization>()).Returns(organization);
        _organizationRepository.Update(Arg.Any<Organization>()).Returns(Task.CompletedTask);

        var actor = new Actor { Id = actorId, Pii = new ActorPii { DisplayName = "Tenant Admin Organization" }, ActorType = null! };
        _actorRepository.Create(Arg.Any<Actor>()).Returns(actor);
        _organizationMemberRepository.Create(Arg.Any<OrganizationMember>()).Returns(
            new OrganizationMember { Id = Guid.NewGuid(), OrganizationTenant = null!, User = null!, Role = null!, Tenant = null! });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await _organizationTenantRepository.Received(1).Create(Arg.Is<OrganizationTenant>(o =>
            o.ApprovalStatusId == (int)ApprovalStatusEnum.Approved
            && o.ApprovedAt.HasValue
            && o.ApprovedBy == userId));
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
            CreatorUserId = userId,
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
        var organization = new Organization
        {
            Id = organizationId,
            Pii = new OrganizationPii { FullName = string.Empty }
        };
        _mapper.Map<Organization>(command.OrganizationDto).Returns(organization);
        _organizationRepository.Create(Arg.Any<Organization>()).Returns(organization);
        _organizationRepository.Update(Arg.Any<Organization>()).Returns(Task.CompletedTask);

        var actor = new Actor { Id = actorId, Pii = new ActorPii { DisplayName = "Test Organization" }, ActorType = null! };
        _actorRepository.Create(Arg.Any<Actor>()).Returns(actor);
        _organizationMemberRepository.Create(Arg.Any<OrganizationMember>()).Returns(
            new OrganizationMember { Id = Guid.NewGuid(), OrganizationTenant = null!, User = null!, Role = null!, Tenant = null! });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await _organizationTenantRepository.Received(1).Create(Arg.Is<OrganizationTenant>(o => o.TenantId == tenantId));
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
            CreatorUserId = userId,
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
        var organization = new Organization
        {
            Id = organizationId,
            Pii = new OrganizationPii { FullName = string.Empty }
        };
        _mapper.Map<Organization>(command.OrganizationDto).Returns(organization);
        _organizationRepository.Create(Arg.Any<Organization>()).Returns(organization);
        _organizationRepository.Update(Arg.Any<Organization>()).Returns(Task.CompletedTask);

        var actor = new Actor { Id = actorId, Pii = new ActorPii { DisplayName = "Test Organization" }, ActorType = null! };
        _actorRepository.Create(Arg.Any<Actor>()).Returns(actor);
        _organizationMemberRepository.Create(Arg.Any<OrganizationMember>()).Returns(
            new OrganizationMember { Id = Guid.NewGuid(), OrganizationTenant = null!, User = null!, Role = null!, Tenant = null! });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await _organizationMemberRepository.Received(1).Create(
            Arg.Is<OrganizationMember>(m =>
                m != null &&
                m.UserId == userId &&
                m.OrganizationTenant.OrganizationId == organizationId));
    }
}
