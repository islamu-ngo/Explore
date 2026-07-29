// ABOUTME: Verifies canonical and tenant-contextual public Actor detail query behavior.
// ABOUTME: Protects tenant discoverability, safe participation overrides, and storage presentation.

using AutoMapper;
using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Actor;
using Explore.Application.Features.Actors.Handlers.Queries;
using Explore.Application.Features.Actors.Requests.Queries;
using Explore.Domain;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Actors.Queries;

public class GetActorDetailsRequestHandlerTests
{
    private readonly IActorRepository _actorRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;
    private readonly ILogger<GetActorDetailsRequestHandler> _logger;
    private readonly GetActorDetailsRequestHandler _handler;

    public GetActorDetailsRequestHandlerTests()
    {
        _actorRepository = Substitute.For<IActorRepository>();
        _tenantContext = Substitute.For<ITenantContext>();
        _mapper = Substitute.For<IMapper>();
        _logger = Substitute.For<ILogger<GetActorDetailsRequestHandler>>();

        _handler = new GetActorDetailsRequestHandler(
            _actorRepository,
            _tenantContext,
            _mapper,
            _logger);
    }

    [Test]
    public async Task Handle_WithLocallyDiscoverableActor_SetsRequestLocalAffordanceState()
    {
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var actor = DataBuilder.Actor.Generate();
        actor.Id = actorId;
        var dto = new ActorDto { Id = actorId };

        _tenantContext.TenantId.Returns(tenantId);
        _actorRepository.GetPublicActorProfileAsync(actorId).Returns(actor);
        _actorRepository.GetLocallyDiscoverableSubscriptionTargetAsync(
                tenantId,
                actorId,
                Arg.Any<CancellationToken>())
            .Returns(actor);
        _mapper.Map<ActorDto>(actor).Returns(dto);

        var result = await _handler.Handle(
            new GetActorDetailsRequest { Id = actorId },
            CancellationToken.None);

        await Assert.That(result.IsLocallyDiscoverable).IsTrue();
    }

    [Test]
    public async Task Handle_WithTenantContext_ReturnsExactDiscoverableActorWithPublicOverrides()
    {
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var actor = DataBuilder.Actor.Generate();
        actor.Id = actorId;
        var organization = DataBuilder.Organization.Generate();
        organization.Actor = actor;
        organization.TenantParticipations.Add(new OrganizationTenant
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Tenant = null!,
            OrganizationId = organization.Id,
            Organization = organization,
            ApprovalStatus = null!,
            DisplayNameOverride = "Local organization",
            DescriptionOverride = "Local public description",
            BannerColor = "#123456"
        });
        actor.OrganizationId = organization.Id;
        actor.Organization = organization;
        var dto = new ActorDto
        {
            Id = actorId,
            DisplayName = "Canonical organization",
            Description = "Canonical description"
        };
        var cancellationToken = new CancellationTokenSource().Token;

        _actorRepository.GetPublicActorProfileByTenantAsync(tenantId, actorId, cancellationToken)
            .Returns(actor);
        _mapper.Map<ActorDto>(actor).Returns(dto);

        var result = await _handler.Handle(
            new GetActorDetailsRequest { Id = actorId, TenantId = tenantId },
            cancellationToken);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.TenantId).IsEqualTo(tenantId);
        await Assert.That(result.IsLocallyDiscoverable).IsTrue();
        await Assert.That(result.DisplayName).IsEqualTo("Local organization");
        await Assert.That(result.Description).IsEqualTo("Local public description");
        await Assert.That(result.BannerColor).IsEqualTo("#123456");
        await _actorRepository.DidNotReceive()
            .GetPublicActorProfileAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _actorRepository.DidNotReceive()
            .GetLocallyDiscoverableSubscriptionTargetAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
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
        actor.AtprotoIdentities.Add(new AtprotoIdentity
        {
            Did = "did:plc:test123",
            ActorId = actorId,
            Actor = actor,
            PdsHost = "https://pds.example.com",
            LastResolvedAt = DateTime.UtcNow
        });

        var expectedDto = new ActorDto
        {
            Id = actorId,
            DisplayName = "Test Actor",
            Did = "did:plc:test123"
        };

        _actorRepository.GetPublicActorProfileAsync(actorId).Returns(actor);
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

        _actorRepository.GetPublicActorProfileAsync(actorId).Returns((Actor?)null);
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
        var request = new GetActorDetailsRequest { Id = actorId };

        var actor = DataBuilder.Actor.Generate();
        actor.Id = actorId;
        actor.ProfilePictureUri = "https://storage.example.com/image.jpg";

        var expectedDto = new ActorDto
        {
            Id = actorId,
            ProfilePictureUri = "https://storage.example.com/image.jpg"
        };

        _actorRepository.GetPublicActorProfileAsync(actorId).Returns(actor);
        _mapper.Map<ActorDto>(actor).Returns(expectedDto);
        var result = await _handler.Handle(request, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.ProfilePictureUri).IsNotNull();
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

        _actorRepository.GetPublicActorProfileAsync(actorId).Returns(actor);
        _mapper.Map<ActorDto>(actor).Returns(expectedDto);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.ActorTypeId).IsEqualTo(actorTypeId);
        await Assert.That(result.ActorTypeFullName).IsEqualTo("User");
    }
}
