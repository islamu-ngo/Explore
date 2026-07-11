// ABOUTME: Focused tests for BFF auth provider readiness and scheme mapping behavior.
// ABOUTME: Keeps provider-selection logic covered after extraction from auth endpoints.

using Explore.Blazor.Constants;
using Explore.Blazor.Services;
using Explore.Blazor.Services.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Explore.Blazor.IntegrationTests.Services;

public sealed class BffProviderReadinessServiceTests
{
    [Test]
    public async Task ResolveProviderScheme_WithKnownProvider_ReturnsAuthSchemeName()
    {
        var service = CreateService();

        var scheme = service.ResolveProviderScheme("Google");

        await Assert.That(scheme).IsEqualTo(AuthSchemeNames.Google);
    }

    [Test]
    public async Task MapSchemeToProviderQueryValue_WithKeycloak_ReturnsProviderValue()
    {
        var service = CreateService();

        var provider = service.MapSchemeToProviderQueryValue(AuthSchemeNames.Keycloak);

        await Assert.That(provider).IsEqualTo("keycloak");
    }

    [Test]
    public async Task IsProviderReadyAsync_WithAtproto_ReturnsTrueWithoutOidcOptions()
    {
        var optionsMonitor = Substitute.For<IOptionsMonitor<OpenIdConnectOptions>>();
        var service = CreateService(optionsMonitor: optionsMonitor);

        var ready = await service.IsProviderReadyAsync(AuthSchemeNames.Atproto, CancellationToken.None);

        await Assert.That(ready).IsTrue();
        optionsMonitor.DidNotReceiveWithAnyArgs().Get(default!);
    }

    [Test]
    public async Task IsProviderReadyAsync_WithInvalidGoogleClientId_ReturnsFalse()
    {
        var optionsMonitor = Substitute.For<IOptionsMonitor<OpenIdConnectOptions>>();
        optionsMonitor.Get(AuthSchemeNames.Google).Returns(new OpenIdConnectOptions
        {
            ClientId = "not-a-google-client-id",
            ClientSecret = "present"
        });
        var service = CreateService(optionsMonitor: optionsMonitor);

        var ready = await service.IsProviderReadyAsync(AuthSchemeNames.Google, CancellationToken.None);

        await Assert.That(ready).IsFalse();
    }

    [Test]
    public async Task HasMinimalProviderConfig_WithAuthorityAndClientId_ReturnsTrue()
    {
        var optionsMonitor = Substitute.For<IOptionsMonitor<OpenIdConnectOptions>>();
        optionsMonitor.Get(AuthSchemeNames.Keycloak).Returns(new OpenIdConnectOptions
        {
            Authority = "https://idp.example.test/realms/islamu",
            ClientId = "islamu-event-blazor"
        });
        var service = CreateService(optionsMonitor: optionsMonitor);

        var hasConfig = service.HasMinimalProviderConfig(AuthSchemeNames.Keycloak);

        await Assert.That(hasConfig).IsTrue();
    }

    [Test]
    public async Task ResolvePreferredProviderForDirectLoginAsync_WithNoReadyButtonProvider_ReturnsNull()
    {
        var schemeManager = Substitute.For<IDynamicAuthSchemeManager>();
        schemeManager.GetRegisteredProviderSchemesAsync().Returns([AuthSchemeNames.Atproto]);
        var service = CreateService(schemeManager: schemeManager);

        var provider = await service.ResolvePreferredProviderForDirectLoginAsync(CancellationToken.None);

        provider.Should().BeNull();
    }

    private static BffProviderReadinessService CreateService(
        IDynamicAuthSchemeManager? schemeManager = null,
        IOptionsMonitor<OpenIdConnectOptions>? optionsMonitor = null)
    {
        schemeManager ??= Substitute.For<IDynamicAuthSchemeManager>();
        optionsMonitor ??= Substitute.For<IOptionsMonitor<OpenIdConnectOptions>>();
        var environment = Substitute.For<IWebHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Production);

        return new BffProviderReadinessService(
            schemeManager,
            optionsMonitor,
            environment,
            NullLogger<BffProviderReadinessService>.Instance);
    }
}
