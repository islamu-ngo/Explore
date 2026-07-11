// ABOUTME: Unit-style tests for BFF preference query normalization rules.
// ABOUTME: Protects endpoint decomposition from drifting validation/defaulting behavior.

using Explore.Blazor.Services.Preferences;
using FluentAssertions;

namespace Explore.Blazor.IntegrationTests.Services;

public sealed class BffPreferenceValidationServiceTests
{
    [Test]
    public async Task NormalizeThemeMode_WithWhitespaceAndUppercase_ReturnsLowercaseMode()
    {
        var service = new BffPreferenceValidationService();

        var result = service.NormalizeThemeMode("  DarkHighContrast  ");

        result.Should().Be("darkhighcontrast");
        await Assert.That(service.ThemeModeValidationMessage).Contains("custom");
    }

    [Test]
    public async Task NormalizeThemeMode_WithUnknownMode_ReturnsNull()
    {
        var service = new BffPreferenceValidationService();

        var result = service.NormalizeThemeMode("sepia");

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task NormalizeLanguage_WithRegisteredCulture_ReturnsCanonicalCode()
    {
        var service = new BffPreferenceValidationService();

        var result = service.NormalizeLanguage("fr");

        result.Should().Be("fr");
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task NormalizeLanguage_WithUnknownCulture_ReturnsNull()
    {
        var service = new BffPreferenceValidationService();

        var result = service.NormalizeLanguage("zz");

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task NormalizeDirection_WithWhitespaceAndUppercase_ReturnsLowercaseDirection()
    {
        var service = new BffPreferenceValidationService();

        var result = service.NormalizeDirection(" RTL ");

        result.Should().Be("rtl");
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task NormalizeDirection_WithUnknownDirection_ReturnsNull()
    {
        var service = new BffPreferenceValidationService();

        var result = service.NormalizeDirection("sideways");

        await Assert.That(result).IsNull();
    }
}
