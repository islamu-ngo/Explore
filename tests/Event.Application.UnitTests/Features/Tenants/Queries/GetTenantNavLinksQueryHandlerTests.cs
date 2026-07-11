// ABOUTME: Unit tests for GetTenantNavLinksQueryHandler.
// ABOUTME: Verifies query returns mapped DTOs from repository.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Tenant;
using Explore.Application.Features.Tenants.Handlers.Queries;
using Explore.Application.Features.Tenants.Requests.Queries;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Tenants.Queries;

public class GetTenantNavLinksQueryHandlerTests
{
    private readonly ITenantNavigationLinkRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;
    private readonly GetTenantNavLinksQueryHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetTenantNavLinksQueryHandlerTests()
    {
        _repository = Substitute.For<ITenantNavigationLinkRepository>();
        _tenantContext = Substitute.For<ITenantContext>();
        _mapper = Substitute.For<IMapper>();
        _tenantContext.TenantId.Returns(_tenantId);
        _handler = new GetTenantNavLinksQueryHandler(_repository, _tenantContext, _mapper);
    }

    [Test]
    public async Task Handle_ReturnsOrderedLinks_WhenLinksExist()
    {
        // Arrange
        var entities = new List<TenantNavigationLink>
        {
            new() { Id = Guid.NewGuid(), TenantId = _tenantId, Label = "Home", Url = "/", Order = 0 },
            new() { Id = Guid.NewGuid(), TenantId = _tenantId, Label = "About", Url = "/about", Order = 1 }
        };
        var expectedDtos = new List<TenantNavigationLinkDto>
        {
            new() { Id = entities[0].Id, Label = "Home", Url = "/", Order = 0 },
            new() { Id = entities[1].Id, Label = "About", Url = "/about", Order = 1 }
        };

        _repository.GetByTenantIdOrderedAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns(entities);
        _mapper.Map<List<TenantNavigationLinkDto>>(entities)
            .Returns(expectedDtos);

        // Act
        var result = await _handler.Handle(new GetTenantNavLinksQuery(), CancellationToken.None);

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0].Label).IsEqualTo("Home");
        await Assert.That(result[1].Label).IsEqualTo("About");
        await _repository.Received(1).GetByTenantIdOrderedAsync(_tenantId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ReturnsEmptyList_WhenNoLinksExist()
    {
        // Arrange
        _repository.GetByTenantIdOrderedAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<TenantNavigationLink>());
        _mapper.Map<List<TenantNavigationLinkDto>>(Arg.Any<List<TenantNavigationLink>>())
            .Returns(new List<TenantNavigationLinkDto>());

        // Act
        var result = await _handler.Handle(new GetTenantNavLinksQuery(), CancellationToken.None);

        // Assert
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Handle_UsesCorrectTenantId_FromContext()
    {
        // Arrange
        _repository.GetByTenantIdOrderedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<TenantNavigationLink>());
        _mapper.Map<List<TenantNavigationLinkDto>>(Arg.Any<List<TenantNavigationLink>>())
            .Returns(new List<TenantNavigationLinkDto>());

        // Act
        await _handler.Handle(new GetTenantNavLinksQuery(), CancellationToken.None);

        // Assert
        await _repository.Received(1).GetByTenantIdOrderedAsync(
            Arg.Is<Guid>(id => id == _tenantId),
            Arg.Any<CancellationToken>());
    }
}
