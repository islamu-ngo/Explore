// ABOUTME: Unit tests for UI theme deletion authorization and default-protection behavior.
// ABOUTME: Verifies scope-aware auth rules and the invariant that a default theme cannot be removed.

namespace Event.Application.UnitTests.Features.Appearance.Commands;

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Application.Features.Appearance.Handlers.Commands;
using Explore.Application.Features.Appearance.Requests.Commands;
using Explore.Domain;
using NSubstitute;

public class DeleteUiThemeCommandHandlerTests
{
    private static readonly Guid TestTenantId = Guid.NewGuid();

    private readonly IUiThemeRepository _uiThemeRepository;
    private readonly IAdminContext _adminContext;
    private readonly DeleteUiThemeCommandHandler _handler;

    public DeleteUiThemeCommandHandlerTests()
    {
        _uiThemeRepository = Substitute.For<IUiThemeRepository>();
        _adminContext = Substitute.For<IAdminContext>();

        _handler = new DeleteUiThemeCommandHandler(_uiThemeRepository, _adminContext);
    }

    [Test]
    public async Task Handle_WhenThemeDoesNotExist_ReturnsFalseWithoutDeleting()
    {
        var command = new DeleteUiThemeCommand { Id = Guid.NewGuid() };
        _uiThemeRepository.GetById(command.Id).Returns((UiTheme?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result).IsFalse();
        await _uiThemeRepository.DidNotReceive().Delete(Arg.Any<UiTheme>());
    }

    [Test]
    public async Task Handle_WhenTenantAdminDeletingOwnTenantTheme_DeletesAndReturnsTrue()
    {
        var theme = CreateTenantTheme(isDefault: false);
        _uiThemeRepository.GetById(theme.Id).Returns(theme);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(new DeleteUiThemeCommand { Id = theme.Id }, CancellationToken.None);

        await Assert.That(result).IsTrue();
        await _uiThemeRepository.Received(1).Delete(theme);
    }

    [Test]
    public async Task Handle_WhenInstanceAdminDeletingPlatformTheme_DeletesAndReturnsTrue()
    {
        var theme = CreatePlatformTheme(isDefault: false);
        _uiThemeRepository.GetById(theme.Id).Returns(theme);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(new DeleteUiThemeCommand { Id = theme.Id }, CancellationToken.None);

        await Assert.That(result).IsTrue();
        await _uiThemeRepository.Received(1).Delete(theme);
    }

    [Test]
    public async Task Handle_WhenNonAdminDeletingTenantTheme_ThrowsAuthorization()
    {
        var theme = CreateTenantTheme(isDefault: false);
        _uiThemeRepository.GetById(theme.Id).Returns(theme);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<AuthorizationException>(async () =>
            await _handler.Handle(new DeleteUiThemeCommand { Id = theme.Id }, CancellationToken.None));

        await _uiThemeRepository.DidNotReceive().Delete(Arg.Any<UiTheme>());
    }

    [Test]
    public async Task Handle_WhenTenantAdminDeletingPlatformTheme_ThrowsAuthorization()
    {
        var theme = CreatePlatformTheme(isDefault: false);
        _uiThemeRepository.GetById(theme.Id).Returns(theme);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<AuthorizationException>(async () =>
            await _handler.Handle(new DeleteUiThemeCommand { Id = theme.Id }, CancellationToken.None));

        await _uiThemeRepository.DidNotReceive().Delete(Arg.Any<UiTheme>());
    }

    [Test]
    public async Task Handle_WhenDeletingDefaultTheme_ThrowsBadRequest()
    {
        var theme = CreateTenantTheme(isDefault: true);
        _uiThemeRepository.GetById(theme.Id).Returns(theme);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<BadRequestException>(async () =>
            await _handler.Handle(new DeleteUiThemeCommand { Id = theme.Id }, CancellationToken.None));

        await _uiThemeRepository.DidNotReceive().Delete(Arg.Any<UiTheme>());
    }

    private static UiTheme CreateTenantTheme(bool isDefault) => CreateTheme(TestTenantId, isDefault);

    private static UiTheme CreatePlatformTheme(bool isDefault) => CreateTheme(null, isDefault);

    private static UiTheme CreateTheme(Guid? tenantId, bool isDefault) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        ThemeKey = "sample-theme",
        DisplayName = "Sample Theme",
        IsActive = true,
        IsDefault = isDefault,
        SortOrder = 10,
        LightPalette = SamplePalette(),
        DarkPalette = SamplePalette(),
    };

    private static Explore.Domain.ValueObjects.UiThemePalette SamplePalette() => new()
    {
        Primary = "#336699",
        PrimaryContrastText = "#FFFFFF",
        Secondary = "#112233",
        SecondaryContrastText = "#FFFFFF",
        Background = "#F8FAFC",
        Surface = "#FFFFFF",
        AppbarBackground = "rgba(51,102,153,0.85)",
        AppbarText = "#FFFFFF",
        DrawerBackground = "#0F172A",
        DrawerText = "#E2E8F0",
        DrawerIcon = "#CBD5E1",
        TextPrimary = "#0F172A",
        TextSecondary = "#64748B",
        Info = "#3B82F6",
        Success = "#10B981",
        Warning = "#F59E0B",
        Error = "#EF4444",
        LinesDefault = "#E2E8F0",
        Divider = "rgba(51,102,153,0.25)",
    };
}
