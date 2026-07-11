// ABOUTME: Unit tests for DeleteTenantNavLinkCommandHandler.
// ABOUTME: Verifies soft-delete via repository Delete, and not-found handling.
using System;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Tenants.Handlers.Commands.DeleteTenantNavLink;
using Explore.Application.Features.Tenants.Requests.Commands.DeleteTenantNavLink;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Tenants.Commands;

public class DeleteTenantNavLinkCommandHandlerTests
{
    private readonly ITenantNavigationLinkRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly DeleteTenantNavLinkCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public DeleteTenantNavLinkCommandHandlerTests()
    {
        _repository = Substitute.For<ITenantNavigationLinkRepository>();
        _tenantContext = Substitute.For<ITenantContext>();
        _tenantContext.TenantId.Returns(_tenantId);
        _handler = new DeleteTenantNavLinkCommandHandler(_repository, _tenantContext);
    }

    [Test]
    public async Task Handle_DeletesLink_WhenExists()
    {
        // Arrange
        var linkId = Guid.NewGuid();
        var existingLink = new TenantNavigationLink
        {
            Id = linkId,
            TenantId = _tenantId,
            Label = "ToDelete",
            Url = "/delete",
            Order = 1
        };

        _repository.GetByIdAndTenantAsync(linkId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(existingLink);

        var command = new DeleteTenantNavLinkCommand { Id = linkId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).Contains("deleted successfully");
        await _repository.Received(1).Delete(existingLink);
    }

    [Test]
    public async Task Handle_ReturnsNotFound_WhenLinkDoesNotExist()
    {
        // Arrange
        var linkId = Guid.NewGuid();
        _repository.GetByIdAndTenantAsync(linkId, _tenantId, Arg.Any<CancellationToken>())
            .Returns((TenantNavigationLink?)null);

        var command = new DeleteTenantNavLinkCommand { Id = linkId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("not found");
        await _repository.DidNotReceive().Delete(Arg.Any<TenantNavigationLink>());
    }

    [Test]
    public async Task Handle_UsesCorrectTenantId_ForLookup()
    {
        // Arrange
        var linkId = Guid.NewGuid();
        _repository.GetByIdAndTenantAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((TenantNavigationLink?)null);

        var command = new DeleteTenantNavLinkCommand { Id = linkId };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).GetByIdAndTenantAsync(
            linkId,
            _tenantId,
            Arg.Any<CancellationToken>());
    }
}
