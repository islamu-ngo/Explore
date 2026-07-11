// ABOUTME: Unit-style tests for BFF preference API forwarding route construction.
// ABOUTME: Protects endpoint decomposition from drifting authenticated BffClient API calls.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Services.Preferences;
using FluentAssertions;

namespace Explore.Blazor.IntegrationTests.Services;

public sealed class BffPreferenceForwardingServiceTests
{
    [Test]
    public async Task GetAppearanceAsync_UsesGeneratedApiClient()
    {
        var expected = new ResolvedAppearanceDto { ThemeMode = "dark" };
        var apiClient = Substitute.For<IEventApiClient>();
        apiClient.GetCurrentUserAppearancePreferencesAsync(cancellationToken: Arg.Any<CancellationToken>())
            .Returns(expected);
        var service = new BffPreferenceForwardingService(apiClient);

        var response = await service.GetAppearanceAsync(CancellationToken.None);

        response.Should().BeSameAs(expected);
        await apiClient.Received(1)
            .GetCurrentUserAppearancePreferencesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PersistPreferencesAsync_MapsPreferenceDtoToApiUpdateRequest()
    {
        var apiClient = Substitute.For<IEventApiClient>();
        UpdateUserAppearancePreferencesDto? captured = null;
        apiClient.UpdateCurrentUserAppearancePreferencesAsync(
                Arg.Do<UpdateUserAppearancePreferencesDto>(request => captured = request),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponseOfGuid());
        var service = new BffPreferenceForwardingService(apiClient);
        var defaultThemeId = Guid.NewGuid();
        var preferences = new BffAppearancePreferences("dark", "rtl", "fr", defaultThemeId);

        await service.PersistPreferencesAsync(preferences, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.ThemeMode.Should().Be("dark");
        captured.Direction.Should().Be("rtl");
        captured.Language.Should().Be("fr");
        captured.DefaultThemeId.Should().Be(defaultThemeId);
    }

    [Test]
    public async Task GeneratePaletteAsync_ForwardsValuesToGeneratedApiClient()
    {
        var expected = new UiThemePaletteDto();
        var apiClient = Substitute.For<IEventApiClient>();
        apiClient.GenerateAppearancePaletteAsync(
                "blue green",
                "#ff/00",
                true,
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(expected);
        var service = new BffPreferenceForwardingService(apiClient);

        var response = await service.GeneratePaletteAsync("blue green", "#ff/00", isDark: true, CancellationToken.None);

        response.Should().BeSameAs(expected);
        await apiClient.Received(1).GenerateAppearancePaletteAsync(
            "blue green",
            "#ff/00",
            true,
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetAvailableThemesAsync_UsesCurrentPresetCatalogRoute()
    {
        ICollection<AvailablePresetDto> expected = [];
        var apiClient = Substitute.For<IEventApiClient>();
        apiClient.GetAvailableThemesAsync(cancellationToken: Arg.Any<CancellationToken>())
            .Returns(expected);
        var service = new BffPreferenceForwardingService(apiClient);

        var response = await service.GetAvailableThemesAsync(CancellationToken.None);

        response.Should().BeSameAs(expected);
        await apiClient.Received(1)
            .GetAvailableThemesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }
}
