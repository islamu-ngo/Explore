// ABOUTME: Unit tests for UpdateTenantNavLinkCommandHandler.
// ABOUTME: Verifies update, validation, not-found, normalization (trim, blank icon→null).
using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Tenant;
using Explore.Application.Features.Tenants.Handlers.Commands.UpdateTenantNavLink;
using Explore.Application.Features.Tenants.Requests.Commands.UpdateTenantNavLink;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Tenants.Commands;

public class UpdateTenantNavLinkCommandHandlerTests
{
    private readonly ITenantNavigationLinkRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;
    private readonly UpdateTenantNavLinkCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public UpdateTenantNavLinkCommandHandlerTests()
    {
        _repository = Substitute.For<ITenantNavigationLinkRepository>();
        _tenantContext = Substitute.For<ITenantContext>();
        _mapper = Substitute.For<IMapper>();
        _tenantContext.TenantId.Returns(_tenantId);
        _handler = new UpdateTenantNavLinkCommandHandler(_repository, _tenantContext, _mapper);
    }

    [Test]
    public async Task Handle_UpdatesLink_WhenValid()
    {
        // Arrange
        var linkId = Guid.NewGuid();
        var dto = new UpdateTenantNavigationLinkDto
        {
            Id = linkId,
            Label = "Updated",
            Url = "https://updated.com",
            Icon = "star",
            OpenInNewTab = true
        };
        var existingLink = new TenantNavigationLink
        {
            Id = linkId,
            TenantId = _tenantId,
            Label = "Old",
            Url = "https://old.com",
            Order = 1
        };

        _repository.GetByIdAndTenantAsync(linkId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(existingLink);

        var command = new UpdateTenantNavLinkCommand { NavigationLinkDto = dto };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await _repository.Received(1).Update(Arg.Is<TenantNavigationLink>(e =>
            e.Label == "Updated" &&
            e.Url == "https://updated.com" &&
            e.Icon == "star" &&
            e.OpenInNewTab == true));
    }

    [Test]
    public async Task Handle_FailsValidation_WhenLabelEmpty()
    {
        // Arrange
        var dto = new UpdateTenantNavigationLinkDto
        {
            Id = Guid.NewGuid(),
            Label = "",
            Url = "https://example.com"
        };
        var command = new UpdateTenantNavLinkCommand { NavigationLinkDto = dto };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Validation failed.");
        await _repository.DidNotReceive().Update(Arg.Any<TenantNavigationLink>());
    }

    [Test]
    public async Task Handle_ReturnsNotFound_WhenLinkDoesNotExist()
    {
        // Arrange
        var linkId = Guid.NewGuid();
        var dto = new UpdateTenantNavigationLinkDto
        {
            Id = linkId,
            Label = "Label",
            Url = "https://example.com"
        };
        _repository.GetByIdAndTenantAsync(linkId, _tenantId, Arg.Any<CancellationToken>())
            .Returns((TenantNavigationLink?)null);

        var command = new UpdateTenantNavLinkCommand { NavigationLinkDto = dto };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("not found");
        await _repository.DidNotReceive().Update(Arg.Any<TenantNavigationLink>());
    }

    [Test]
    public async Task Handle_TrimsLabelAndUrl()
    {
        // Arrange
        var linkId = Guid.NewGuid();
        var dto = new UpdateTenantNavigationLinkDto
        {
            Id = linkId,
            Label = "  Trimmed  ",
            Url = "  https://trimmed.com  "
        };
        var existingLink = new TenantNavigationLink
        {
            Id = linkId,
            TenantId = _tenantId,
            Label = "Old",
            Url = "https://old.com",
            Order = 1
        };

        _repository.GetByIdAndTenantAsync(linkId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(existingLink);

        var command = new UpdateTenantNavLinkCommand { NavigationLinkDto = dto };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).Update(Arg.Is<TenantNavigationLink>(e =>
            e.Label == "Trimmed" &&
            e.Url == "https://trimmed.com"));
    }

    [Test]
    public async Task Handle_NormalizesBlankIconToNull()
    {
        // Arrange
        var linkId = Guid.NewGuid();
        var dto = new UpdateTenantNavigationLinkDto
        {
            Id = linkId,
            Label = "Link",
            Url = "https://example.com",
            Icon = "   "
        };
        var existingLink = new TenantNavigationLink
        {
            Id = linkId,
            TenantId = _tenantId,
            Label = "Old",
            Url = "https://old.com",
            Icon = "old-icon",
            Order = 1
        };

        _repository.GetByIdAndTenantAsync(linkId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(existingLink);

        var command = new UpdateTenantNavLinkCommand { NavigationLinkDto = dto };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).Update(Arg.Is<TenantNavigationLink>(e => e.Icon == null));
    }

    [Test]
    public async Task Handle_FailsValidation_WhenUrlIsJavascript()
    {
        // Arrange
        var dto = new UpdateTenantNavigationLinkDto
        {
            Id = Guid.NewGuid(),
            Label = "Evil",
            Url = "javascript:alert(1)"
        };
        var command = new UpdateTenantNavLinkCommand { NavigationLinkDto = dto };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await _repository.DidNotReceive().Update(Arg.Any<TenantNavigationLink>());
    }
}
