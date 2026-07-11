// ABOUTME: Unit tests for UI theme creation authorization and default-handling behavior.
// ABOUTME: Verifies tenant/platform admin rules and transactional clearing of existing defaults.

namespace Event.Application.UnitTests.Features.Appearance.Commands;

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Appearance;
using Explore.Application.Features.Appearance.Handlers.Commands;
using Explore.Application.Features.Appearance.Requests.Commands;
using Explore.Domain;
using NSubstitute;

public class CreateUiThemeCommandHandlerTests
{
    private static readonly Guid TestTenantId = Guid.NewGuid();
    private static readonly Guid TestUserId = Guid.NewGuid();

    private readonly IUiThemeRepository _uiThemeRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IAdminContext _adminContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CreateUiThemeCommandHandler _handler;

    public CreateUiThemeCommandHandlerTests()
    {
        _uiThemeRepository = Substitute.For<IUiThemeRepository>();
        _tenantContext = Substitute.For<ITenantContext>();
        _adminContext = Substitute.For<IAdminContext>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _tenantContext.TenantId.Returns(TestTenantId);
        _currentUserService.UserId.Returns(TestUserId);
        _currentUserService.IsAuthenticated.Returns(true);
        _uiThemeRepository.ThemeKeyExistsAsync(Arg.Any<Guid?>(), Arg.Any<string>(), Arg.Any<Guid?>()).Returns(false);

        _unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<UiTheme>>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var operation = callInfo.Arg<Func<CancellationToken, Task<UiTheme>>>();
                return operation(CancellationToken.None);
            });

        _uiThemeRepository.Create(Arg.Any<UiTheme>())
            .Returns(callInfo =>
            {
                var theme = callInfo.Arg<UiTheme>();
                theme.Id = Guid.NewGuid();
                return theme;
            });

        _handler = new CreateUiThemeCommandHandler(
            _uiThemeRepository,
            _tenantContext,
            _adminContext,
            _currentUserService,
            _unitOfWork);
    }

    [Test]
    public async Task Handle_WhenTenantAdminCreatingDefaultTenantTheme_ClearsExistingDefaultAndCreatesTheme()
    {
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(CreateTenantThemeCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _uiThemeRepository.Received(1).ClearDefaultAsync(TestTenantId, null);
        await _uiThemeRepository.Received(1).Create(Arg.Is<UiTheme>(theme =>
            theme.TenantId == TestTenantId
            && theme.IsDefault
            && theme.ThemeKey == "community-blue"));
    }

    [Test]
    public async Task Handle_WhenRegularUserCreatingPlatformTheme_ReturnsUnauthorized()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(CreatePlatformThemeCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await _uiThemeRepository.DidNotReceive().Create(Arg.Any<UiTheme>());
    }

    private static CreateUiThemeCommand CreateTenantThemeCommand() => new()
    {
        UiThemeDto = CreateDto(isPlatformTheme: false)
    };

    private static CreateUiThemeCommand CreatePlatformThemeCommand() => new()
    {
        UiThemeDto = CreateDto(isPlatformTheme: true)
    };

    private static CreateUiThemeDto CreateDto(bool isPlatformTheme) => new()
    {
        IsPlatformTheme = isPlatformTheme,
        ThemeKey = "community-blue",
        DisplayName = "Community Blue",
        IsActive = true,
        IsDefault = true,
        LightPalette = CreatePalette("#336699", "rgba(51,102,153,0.85)"),
        DarkPalette = CreatePalette("#6699CC", "rgba(15,23,42,0.85)")
    };

    private static UiThemePaletteDto CreatePalette(string primary, string appbarBackground) => new()
    {
        Primary = primary,
        Secondary = "#112233",
        Background = "#F8FAFC",
        Surface = "#FFFFFF",
        AppbarBackground = appbarBackground,
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
        Divider = "rgba(51,102,153,0.25)"
    };
}
