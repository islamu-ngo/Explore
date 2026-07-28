// ABOUTME: Unit tests for UpdateTenantNavLinkCommandHandler.
// ABOUTME: Verifies update, validation, not-found, normalization (trim, blank icon→null).
using System;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Tenant;
using Explore.Application.Features.Tenants.Handlers.Commands.UpdateTenantNavLink;
using Explore.Application.Features.Tenants.Requests.Commands.UpdateTenantNavLink;
using Explore.Application.Models.Common;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Tenants.Commands;

public class UpdateTenantNavLinkCommandHandlerTests
{
    private readonly ITenantNavigationLinkRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly IHierarchicalSettingsResolver _settingsResolver;
    private readonly UpdateTenantNavLinkCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public UpdateTenantNavLinkCommandHandlerTests()
    {
        _repository = Substitute.For<ITenantNavigationLinkRepository>();
        _tenantContext = Substitute.For<ITenantContext>();
        _settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        _tenantContext.TenantId.Returns(_tenantId);
        _settingsResolver.ResolveAsync<bool>(
                Arg.Any<string>(),
                Arg.Any<Explore.Application.Settings.SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        _handler = new UpdateTenantNavLinkCommandHandler(_repository, _tenantContext, _settingsResolver);
    }

    [Test]
    public async Task Handle_UpdatesLink_WhenValid()
    {
        // Arrange
        var linkId = Guid.NewGuid();
        var dto = new UpdateTenantNavigationLinkDto
        {
            Label = new() { Value = "Updated" },
            Url = new() { Value = "https://updated.com" },
            Icon = new() { Value = OptionalUpdate<string?>.Set("star") },
            OpenInNewTab = new() { Value = true }
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

        var command = new UpdateTenantNavLinkCommand
        {
            NavigationLinkId = linkId,
            TenantId = _tenantId,
            Update = dto
        };

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
            Label = new() { Value = "" },
            Url = new() { Value = "https://example.com" }
        };
        var command = new UpdateTenantNavLinkCommand
        {
            NavigationLinkId = Guid.NewGuid(),
            TenantId = _tenantId,
            Update = dto
        };

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
            Label = new() { Value = "Label" },
            Url = new() { Value = "https://example.com" }
        };
        _repository.GetByIdAndTenantAsync(linkId, _tenantId, Arg.Any<CancellationToken>())
            .Returns((TenantNavigationLink?)null);

        var command = new UpdateTenantNavLinkCommand
        {
            NavigationLinkId = linkId,
            TenantId = _tenantId,
            Update = dto
        };

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
            Label = new() { Value = "  Trimmed  " },
            Url = new() { Value = "  https://trimmed.com  " }
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

        var command = new UpdateTenantNavLinkCommand
        {
            NavigationLinkId = linkId,
            TenantId = _tenantId,
            Update = dto
        };

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
            Label = new() { Value = "Link" },
            Url = new() { Value = "https://example.com" },
            Icon = new() { Value = OptionalUpdate<string?>.Set("   ") }
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

        var command = new UpdateTenantNavLinkCommand
        {
            NavigationLinkId = linkId,
            TenantId = _tenantId,
            Update = dto
        };

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
            Label = new() { Value = "Evil" },
            Url = new() { Value = "javascript:alert(1)" }
        };
        var command = new UpdateTenantNavLinkCommand
        {
            NavigationLinkId = Guid.NewGuid(),
            TenantId = _tenantId,
            Update = dto
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await _repository.DidNotReceive().Update(Arg.Any<TenantNavigationLink>());
    }
}
