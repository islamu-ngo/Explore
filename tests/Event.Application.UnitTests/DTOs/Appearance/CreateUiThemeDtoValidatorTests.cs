// ABOUTME: Tests UI theme DTO validation rules for palette format and scoped theme-key uniqueness.
// ABOUTME: Keeps input validation deterministic before create/update handlers persist themes.

namespace Event.Application.UnitTests.DTOs.Appearance;

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Appearance;
using Explore.Application.DTOs.Appearance.Validators;
using NSubstitute;

public class CreateUiThemeDtoValidatorTests
{
    private readonly IUiThemeRepository _uiThemeRepository;
    private readonly CreateUiThemeDtoValidator _validator;

    public CreateUiThemeDtoValidatorTests()
    {
        _uiThemeRepository = Substitute.For<IUiThemeRepository>();
        _uiThemeRepository.ThemeKeyExistsAsync(Arg.Any<Guid?>(), Arg.Any<string>(), Arg.Any<Guid?>()).Returns(false);
        _validator = new CreateUiThemeDtoValidator(_uiThemeRepository, Guid.NewGuid());
    }

    [Test]
    public async Task Validate_WhenPaletteContainsInvalidHex_ReturnsFailure()
    {
        var dto = CreateValidDto();
        dto.LightPalette.Primary = "blue";

        var result = await _validator.ValidateAsync(dto);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.ErrorMessage)).Contains("Primary must be a #RRGGBB color.");
    }

    [Test]
    public async Task Validate_WhenThemeKeyAlreadyExists_ReturnsFailure()
    {
        _uiThemeRepository.ThemeKeyExistsAsync(Arg.Any<Guid?>(), Arg.Any<string>(), Arg.Any<Guid?>()).Returns(true);
        var dto = CreateValidDto();

        var result = await _validator.ValidateAsync(dto);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.ErrorMessage)).Contains("A theme with the same key already exists for this catalog.");
    }

    private static CreateUiThemeDto CreateValidDto() => new()
    {
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
        PrimaryContrastText = "#FFFFFF",
        Secondary = "#112233",
        SecondaryContrastText = "#FFFFFF",
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
