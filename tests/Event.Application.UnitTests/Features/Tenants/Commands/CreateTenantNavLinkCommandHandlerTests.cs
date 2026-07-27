// ABOUTME: Unit tests for CreateTenantNavLinkCommandHandler.
// ABOUTME: Verifies creation, validation, normalization (trim, blank icon→null), and auto-order.
using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Tenant;
using Explore.Application.Features.Tenants.Handlers.Commands.CreateTenantNavLink;
using Explore.Application.Features.Tenants.Requests.Commands.CreateTenantNavLink;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Tenants.Commands;

public class CreateTenantNavLinkCommandHandlerTests
{
    private readonly ITenantNavigationLinkRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly IHierarchicalSettingsResolver _settingsResolver;
    private readonly IMapper _mapper;
    private readonly CreateTenantNavLinkCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public CreateTenantNavLinkCommandHandlerTests()
    {
        _repository = Substitute.For<ITenantNavigationLinkRepository>();
        _tenantContext = Substitute.For<ITenantContext>();
        _settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        _mapper = Substitute.For<IMapper>();
        _tenantContext.TenantId.Returns(_tenantId);
        _settingsResolver.ResolveAsync<bool>(
                Arg.Any<string>(),
                Arg.Any<Explore.Application.Settings.SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        _handler = new CreateTenantNavLinkCommandHandler(_repository, _tenantContext, _settingsResolver, _mapper);
    }

    [Test]
    public async Task Handle_CreatesLink_WithValidDto()
    {
        // Arrange
        var linkId = Guid.NewGuid();
        var dto = new CreateTenantNavigationLinkDto
        {
            Label = "Home",
            Url = "https://example.com",
            Icon = "home",
            OpenInNewTab = true
        };
        var entity = new TenantNavigationLink { Id = linkId, Label = "Home", Url = "https://example.com", Icon = "home" };

        _mapper.Map<TenantNavigationLink>(dto).Returns(entity);
        _repository.GetMaxOrderByTenantIdAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(2);
        _repository.Create(Arg.Any<TenantNavigationLink>()).Returns(callInfo => callInfo.Arg<TenantNavigationLink>());

        var command = new CreateTenantNavLinkCommand { NavigationLinkDto = dto };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(linkId);
        await _repository.Received(1).Create(Arg.Is<TenantNavigationLink>(e =>
            e.TenantId == _tenantId &&
            e.Order == 3 &&
            e.IsActive == true));
    }

    [Test]
    public async Task Handle_TrimsLabelAndUrl()
    {
        // Arrange
        var dto = new CreateTenantNavigationLinkDto
        {
            Label = "  Home  ",
            Url = "  https://example.com  ",
            OpenInNewTab = false
        };
        var entity = new TenantNavigationLink { Id = Guid.NewGuid(), Label = "  Home  ", Url = "  https://example.com  " };

        _mapper.Map<TenantNavigationLink>(dto).Returns(entity);
        _repository.GetMaxOrderByTenantIdAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(0);
        _repository.Create(Arg.Any<TenantNavigationLink>()).Returns(callInfo => callInfo.Arg<TenantNavigationLink>());

        var command = new CreateTenantNavLinkCommand { NavigationLinkDto = dto };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await _repository.Received(1).Create(Arg.Is<TenantNavigationLink>(e =>
            e.Label == "Home" &&
            e.Url == "https://example.com"));
    }

    [Test]
    public async Task Handle_NormalizesBlankIconToNull()
    {
        // Arrange
        var dto = new CreateTenantNavigationLinkDto
        {
            Label = "Link",
            Url = "https://example.com",
            Icon = "   ",
            OpenInNewTab = false
        };
        var entity = new TenantNavigationLink { Id = Guid.NewGuid(), Label = "Link", Url = "https://example.com", Icon = "   " };

        _mapper.Map<TenantNavigationLink>(dto).Returns(entity);
        _repository.GetMaxOrderByTenantIdAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(0);
        _repository.Create(Arg.Any<TenantNavigationLink>()).Returns(callInfo => callInfo.Arg<TenantNavigationLink>());

        var command = new CreateTenantNavLinkCommand { NavigationLinkDto = dto };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).Create(Arg.Is<TenantNavigationLink>(e => e.Icon == null));
    }

    [Test]
    public async Task Handle_AutoAssignsOrder_AsMaxPlusOne()
    {
        // Arrange
        var dto = new CreateTenantNavigationLinkDto { Label = "New", Url = "/new" };
        var entity = new TenantNavigationLink { Id = Guid.NewGuid(), Label = "New", Url = "/new" };

        _mapper.Map<TenantNavigationLink>(dto).Returns(entity);
        _repository.GetMaxOrderByTenantIdAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(5);
        _repository.Create(Arg.Any<TenantNavigationLink>()).Returns(callInfo => callInfo.Arg<TenantNavigationLink>());

        var command = new CreateTenantNavLinkCommand { NavigationLinkDto = dto };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).Create(Arg.Is<TenantNavigationLink>(e => e.Order == 6));
    }

    [Test]
    public async Task Handle_FailsValidation_WhenLabelEmpty()
    {
        // Arrange
        var dto = new CreateTenantNavigationLinkDto { Label = "", Url = "https://example.com" };
        var command = new CreateTenantNavLinkCommand { NavigationLinkDto = dto };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Validation failed.");
        await Assert.That(result.Errors).IsNotNull();
        await Assert.That(result.Errors!.Count).IsGreaterThan(0);
        await _repository.DidNotReceive().Create(Arg.Any<TenantNavigationLink>());
    }

    [Test]
    public async Task Handle_FailsValidation_WhenUrlIsJavascript()
    {
        // Arrange
        var dto = new CreateTenantNavigationLinkDto { Label = "Evil", Url = "javascript:alert(1)" };
        var command = new CreateTenantNavLinkCommand { NavigationLinkDto = dto };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await _repository.DidNotReceive().Create(Arg.Any<TenantNavigationLink>());
    }

    [Test]
    public async Task Handle_FailsValidation_WhenUrlIsDataScheme()
    {
        // Arrange
        var dto = new CreateTenantNavigationLinkDto { Label = "Data", Url = "data:text/html,<h1>hi</h1>" };
        var command = new CreateTenantNavLinkCommand { NavigationLinkDto = dto };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await _repository.DidNotReceive().Create(Arg.Any<TenantNavigationLink>());
    }

    [Test]
    public async Task Handle_PassesValidation_WithRelativePath()
    {
        // Arrange
        var dto = new CreateTenantNavigationLinkDto { Label = "Home", Url = "/home" };
        var entity = new TenantNavigationLink { Id = Guid.NewGuid(), Label = "Home", Url = "/home" };

        _mapper.Map<TenantNavigationLink>(dto).Returns(entity);
        _repository.GetMaxOrderByTenantIdAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(0);
        _repository.Create(Arg.Any<TenantNavigationLink>()).Returns(callInfo => callInfo.Arg<TenantNavigationLink>());

        var command = new CreateTenantNavLinkCommand { NavigationLinkDto = dto };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
    }

    [Test]
    public async Task Handle_RejectsHttpUrl_WhenHttpsIsRequired()
    {
        var dto = new CreateTenantNavigationLinkDto { Label = "Internal", Url = "http://internal.example" };

        var result = await _handler.Handle(
            new CreateTenantNavLinkCommand { NavigationLinkDto = dto },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await _repository.DidNotReceive().Create(Arg.Any<TenantNavigationLink>());
    }

    [Test]
    public async Task Handle_AllowsHttpUrl_WhenHttpsRequirementIsDisabled()
    {
        _settingsResolver.ResolveAsync<bool>(
                Arg.Any<string>(),
                Arg.Any<Explore.Application.Settings.SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(false);
        var dto = new CreateTenantNavigationLinkDto { Label = "Internal", Url = "http://internal.example" };
        var entity = new TenantNavigationLink
        {
            Id = Guid.NewGuid(),
            Label = dto.Label,
            Url = dto.Url
        };
        _mapper.Map<TenantNavigationLink>(dto).Returns(entity);
        _repository.Create(Arg.Any<TenantNavigationLink>()).Returns(call => call.Arg<TenantNavigationLink>());

        var result = await _handler.Handle(
            new CreateTenantNavLinkCommand { NavigationLinkDto = dto },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _repository.Received(1).Create(Arg.Is<TenantNavigationLink>(link => link.Url == dto.Url));
    }
}
