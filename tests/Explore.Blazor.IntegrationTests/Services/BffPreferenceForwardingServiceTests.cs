// ABOUTME: Unit-style tests for BFF preference API forwarding route construction.
// ABOUTME: Protects endpoint decomposition from drifting authenticated BffClient API calls.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Services.Preferences;

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

        await Assert.That(response).IsSameReferenceAs(expected);
        await apiClient.Received(1)
            .GetCurrentUserAppearancePreferencesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PersistLocalizationAsync_MapsOnlySuppliedLocalizationLeaves()
    {
        var apiClient = Substitute.For<IEventApiClient>();
        UpdateUserAppearancePreferencesDto? captured = null;
        apiClient.UpdateCurrentUserAppearancePreferencesAsync(
                Arg.Do<UpdateUserAppearancePreferencesDto>(request => captured = request),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponseOfGuid());
        var service = new BffPreferenceForwardingService(apiClient);
        await service.PersistLocalizationAsync("rtl", null, CancellationToken.None);

        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.Localization).IsNotNull();
        await Assert.That(captured.Localization!.Direction).IsEqualTo("rtl");
        await Assert.That(captured.Localization.Language).IsNull();
    }

    [Test]
    public async Task SetThemeModeAsync_UsesFocusedModeOperation()
    {
        var apiClient = Substitute.For<IEventApiClient>();
        var service = new BffPreferenceForwardingService(apiClient);

        await service.SetThemeModeAsync("dark", CancellationToken.None);

        await apiClient.Received(1).SetAppearanceThemeModeAsync(
            Arg.Is<SetThemeModeRequestDto>(request => request.ThemeMode == "dark"),
            cancellationToken: Arg.Any<CancellationToken>());
        await apiClient.DidNotReceive().UpdateCurrentUserAppearancePreferencesAsync(
            Arg.Any<UpdateUserAppearancePreferencesDto>(),
            cancellationToken: Arg.Any<CancellationToken>());
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

        await Assert.That(response).IsSameReferenceAs(expected);
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

        await Assert.That(response).IsSameReferenceAs(expected);
        await apiClient.Received(1)
            .GetAvailableThemesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }
}
