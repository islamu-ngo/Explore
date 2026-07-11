// ABOUTME: Unit tests for ReorderTenantNavLinksCommandHandler.
// ABOUTME: Verifies reorder success, empty-list rejection, cross-tenant ID rejection (no partial apply).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Tenant;
using Explore.Application.Features.Tenants.Handlers.Commands.ReorderTenantNavLinks;
using Explore.Application.Features.Tenants.Requests.Commands.ReorderTenantNavLinks;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Tenants.Commands;

public class ReorderTenantNavLinksCommandHandlerTests
{
    private readonly ITenantNavigationLinkRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ReorderTenantNavLinksCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ReorderTenantNavLinksCommandHandlerTests()
    {
        _repository = Substitute.For<ITenantNavigationLinkRepository>();
        _tenantContext = Substitute.For<ITenantContext>();
        _tenantContext.TenantId.Returns(_tenantId);
        _handler = new ReorderTenantNavLinksCommandHandler(_repository, _tenantContext);
    }

    [Test]
    public async Task Handle_ReordersLinks_WhenAllIdsValid()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var existingLinks = new List<TenantNavigationLink>
        {
            new() { Id = id1, TenantId = _tenantId, Label = "A", Url = "/a", Order = 0 },
            new() { Id = id2, TenantId = _tenantId, Label = "B", Url = "/b", Order = 1 }
        };

        _repository.GetByTenantIdOrderedAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns(existingLinks);

        var command = new ReorderTenantNavLinksCommand
        {
            NavigationLinkOrders = new List<UpdateTenantNavigationLinkOrderDto>
            {
                new() { Id = id1, Order = 1 },
                new() { Id = id2, Order = 0 }
            }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).Contains("reordered successfully");
        await _repository.Received(2).Update(Arg.Any<TenantNavigationLink>());
    }

    [Test]
    public async Task Handle_Fails_WhenListEmpty()
    {
        // Arrange
        var command = new ReorderTenantNavLinksCommand
        {
            NavigationLinkOrders = new List<UpdateTenantNavigationLinkOrderDto>()
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("No navigation links provided");
        await _repository.DidNotReceive().Update(Arg.Any<TenantNavigationLink>());
    }

    [Test]
    public async Task Handle_Fails_WhenCrossTenantIdIncluded_NoPartialApply()
    {
        // Arrange
        var validId = Guid.NewGuid();
        var foreignId = Guid.NewGuid(); // does not belong to tenant
        var existingLinks = new List<TenantNavigationLink>
        {
            new() { Id = validId, TenantId = _tenantId, Label = "Valid", Url = "/valid", Order = 0 }
        };

        _repository.GetByTenantIdOrderedAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns(existingLinks);

        var command = new ReorderTenantNavLinksCommand
        {
            NavigationLinkOrders = new List<UpdateTenantNavigationLinkOrderDto>
            {
                new() { Id = validId, Order = 1 },
                new() { Id = foreignId, Order = 0 } // cross-tenant
            }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("not found");
        // No partial apply — Update should NOT be called for any link
        await _repository.DidNotReceive().Update(Arg.Any<TenantNavigationLink>());
    }

    [Test]
    public async Task Handle_UpdatesCorrectOrderValues()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();
        var existingLinks = new List<TenantNavigationLink>
        {
            new() { Id = id1, TenantId = _tenantId, Label = "A", Url = "/a", Order = 0 },
            new() { Id = id2, TenantId = _tenantId, Label = "B", Url = "/b", Order = 1 },
            new() { Id = id3, TenantId = _tenantId, Label = "C", Url = "/c", Order = 2 }
        };

        _repository.GetByTenantIdOrderedAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns(existingLinks);

        var command = new ReorderTenantNavLinksCommand
        {
            NavigationLinkOrders = new List<UpdateTenantNavigationLinkOrderDto>
            {
                new() { Id = id3, Order = 0 },
                new() { Id = id1, Order = 1 },
                new() { Id = id2, Order = 2 }
            }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        // Verify the order values were correctly set
        var link1 = existingLinks.First(l => l.Id == id1);
        var link2 = existingLinks.First(l => l.Id == id2);
        var link3 = existingLinks.First(l => l.Id == id3);
        await Assert.That(link3.Order).IsEqualTo(0);
        await Assert.That(link1.Order).IsEqualTo(1);
        await Assert.That(link2.Order).IsEqualTo(2);
    }
}
