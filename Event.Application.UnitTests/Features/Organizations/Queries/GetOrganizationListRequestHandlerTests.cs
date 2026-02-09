using AutoMapper;
using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Organization;
using Explore.Application.Features.Organizations.Handlers.Queries;
using Explore.Application.Features.Organizations.Requests.Queries;
using Explore.Domain;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Organizations.Queries;

public class GetOrganizationListRequestHandlerTests
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IMapper _mapper;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<GetOrganizationListRequestHandler> _logger;
    private readonly GetOrganizationListRequestHandler _handler;

    public GetOrganizationListRequestHandlerTests()
    {
        _organizationRepository = Substitute.For<IOrganizationRepository>();
        _mapper = Substitute.For<IMapper>();
        _objectStorageService = Substitute.For<IObjectStorageService>();
        _logger = Substitute.For<ILogger<GetOrganizationListRequestHandler>>();

        _handler = new GetOrganizationListRequestHandler(
            _organizationRepository,
            _mapper,
            _objectStorageService,
            _logger);
    }

    [Test]
    public async Task Handle_WithDefaultPagination_ReturnsFirstPage()
    {
        // Arrange
        var request = new GetOrganizationListRequest
        {
            PageNumber = 1,
            PageSize = 20
        };

        var organizations = DataBuilder.Organization.Generate(5);
        var organizationDtos = organizations.Select(o => new OrganizationListDto
        {
            Id = o.Id,
            FullName = o.FullName,
            Email = o.Email,
            Country = string.Empty,
            City = string.Empty,
            Postcode = string.Empty,
            Address = string.Empty,
            ApprovalStatusFullName = string.Empty
        }).ToList();

        _organizationRepository.GetOrganizationsWithDetailsPaged(1, 20)
            .Returns((organizations, 5));
        _mapper.Map<List<OrganizationListDto>>(organizations).Returns(organizationDtos);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Items.Count).IsEqualTo(5);
        await Assert.That(result.TotalCount).IsEqualTo(5);
        await Assert.That(result.PageNumber).IsEqualTo(1);
        await Assert.That(result.PageSize).IsEqualTo(20);
    }

    [Test]
    public async Task Handle_WithCustomPagination_ReturnsCorrectPage()
    {
        // Arrange
        var request = new GetOrganizationListRequest
        {
            PageNumber = 2,
            PageSize = 10
        };

        var organizations = DataBuilder.Organization.Generate(10);
        var organizationDtos = organizations.Select(o => new OrganizationListDto
        {
            Id = o.Id,
            FullName = o.FullName,
            Email = o.Email,
            Country = string.Empty,
            City = string.Empty,
            Postcode = string.Empty,
            Address = string.Empty,
            ApprovalStatusFullName = string.Empty
        }).ToList();

        _organizationRepository.GetOrganizationsWithDetailsPaged(2, 10)
            .Returns((organizations, 25));
        _mapper.Map<List<OrganizationListDto>>(organizations).Returns(organizationDtos);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Items.Count).IsEqualTo(10);
        await Assert.That(result.TotalCount).IsEqualTo(25);
        await Assert.That(result.PageNumber).IsEqualTo(2);
        await Assert.That(result.TotalPages).IsEqualTo(3);
    }

    [Test]
    public async Task Handle_WithEmptyResult_ReturnsEmptyPaginatedResult()
    {
        // Arrange
        var request = new GetOrganizationListRequest
        {
            PageNumber = 1,
            PageSize = 20
        };

        var emptyList = new List<Organization>();
        var emptyDtos = new List<OrganizationListDto>();

        _organizationRepository.GetOrganizationsWithDetailsPaged(1, 20)
            .Returns((emptyList, 0));
        _mapper.Map<List<OrganizationListDto>>(emptyList).Returns(emptyDtos);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Items.Count).IsEqualTo(0);
        await Assert.That(result.TotalCount).IsEqualTo(0);
        await Assert.That(result.HasNextPage).IsFalse();
        await Assert.That(result.HasPreviousPage).IsFalse();
    }
}
