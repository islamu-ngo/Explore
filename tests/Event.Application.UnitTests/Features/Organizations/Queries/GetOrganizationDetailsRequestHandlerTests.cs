using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Organization;
using Explore.Application.Features.Organizations.Handlers.Queries;
using Explore.Application.Features.Organizations.Requests.Queries;
using Explore.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Organizations.Queries;

public class GetOrganizationDetailsRequestHandlerTests
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IMapper _mapper;
    private readonly IObjectStorageService _objectStorageService;
    private readonly GetOrganizationDetailsRequestHandler _handler;

    public GetOrganizationDetailsRequestHandlerTests()
    {
        _organizationRepository = Substitute.For<IOrganizationRepository>();
        _mapper = Substitute.For<IMapper>();
        _objectStorageService = Substitute.For<IObjectStorageService>();
        var logger = Substitute.For<ILogger<GetOrganizationDetailsRequestHandler>>();

        _handler = new GetOrganizationDetailsRequestHandler(
            _organizationRepository,
            _mapper,
            _objectStorageService,
            logger,
            new TestHybridCache());
    }

    [Test]
    public async Task Handle_WhenOrganizationIdMatches_ReturnsOrganizationWithoutActorFallback()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var organization = CreateOrganization(organizationId, actorId);
        var dto = CreateOrganizationDto(organizationId, actorId);

        _organizationRepository.GetOrganizationWithDetails(organizationId, Arg.Any<CancellationToken>())
            .Returns(organization);
        _mapper.Map<OrganizationDto>(organization).Returns(dto);

        // Act
        var result = await _handler.Handle(new GetOrganizationDetailsRequest { Id = organizationId }, CancellationToken.None);

        // Assert
        await Assert.That(result).IsSameReferenceAs(dto);
        await _organizationRepository.DidNotReceive()
            .GetOrganizationWithDetailsByActorId(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenActorIdMatches_ReturnsResolvedOrganization()
    {
        // Arrange
        var requestedActorId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var organization = CreateOrganization(organizationId, requestedActorId);
        var dto = CreateOrganizationDto(organizationId, requestedActorId);

        _organizationRepository.GetOrganizationWithDetails(requestedActorId, Arg.Any<CancellationToken>())
            .Returns((Organization?)null);
        _organizationRepository.GetOrganizationWithDetailsByActorId(requestedActorId, Arg.Any<CancellationToken>())
            .Returns(organization);
        _mapper.Map<OrganizationDto>(organization).Returns(dto);

        // Act
        var result = await _handler.Handle(new GetOrganizationDetailsRequest { Id = requestedActorId }, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(organizationId);
        await Assert.That(result.ActorId).IsEqualTo(requestedActorId);
    }

    [Test]
    public async Task Handle_WhenNoOrganizationOrActorMatches_ReturnsNull()
    {
        // Arrange
        var requestedId = Guid.NewGuid();
        _organizationRepository.GetOrganizationWithDetails(requestedId, Arg.Any<CancellationToken>())
            .Returns((Organization?)null);
        _organizationRepository.GetOrganizationWithDetailsByActorId(requestedId, Arg.Any<CancellationToken>())
            .Returns((Organization?)null);

        // Act
        var result = await _handler.Handle(new GetOrganizationDetailsRequest { Id = requestedId }, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNull();
        _mapper.DidNotReceive().Map<OrganizationDto>(Arg.Any<Organization>());
    }

    private static Organization CreateOrganization(Guid organizationId, Guid actorId)
    {
        return new Organization
        {
            Id = organizationId,
            ActorId = actorId,
            Pii = new OrganizationPii { FullName = "ISLAMU" },
            ApprovalStatus = new ApprovalStatus { Id = 1, FullName = "Approved", MasterCode = "APPROVED" },
            Tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                FullName = "Default Tenant",
                Slug = "default",
                TenantStatus = new TenantStatus
                {
                    Id = 1,
                    FullName = "Active",
                    MasterCode = "ACTIVE",
                    IsActiveState = true
                }
            }
        };
    }

    private static OrganizationDto CreateOrganizationDto(Guid organizationId, Guid actorId)
    {
        return new OrganizationDto
        {
            Id = organizationId,
            ActorId = actorId,
            FullName = "ISLAMU",
            Email = "info@islamu.test",
            Country = "Belgium",
            City = "Brussels",
            Postcode = "1000",
            Address = "Main Street 1"
        };
    }

    private sealed class TestHybridCache : HybridCache
    {
        public override ValueTask<T> GetOrCreateAsync<TState, T>(
            string key,
            TState state,
            Func<TState, CancellationToken, ValueTask<T>> factory,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default)
        {
            return factory(state, cancellationToken);
        }

        public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public override ValueTask SetAsync<T>(
            string key,
            T value,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
