// ABOUTME: Blazor BFF security integration tests verifying real OIDC against containerized Keycloak.
// ABOUTME: Tests challenge redirects, provider discovery, auth status, and signout behavior.

using Explore.Blazor.IntegrationTests.Fixtures;
using FluentAssertions;
using TUnit.Core;

namespace Explore.Blazor.IntegrationTests.Endpoints;

/// <summary>
/// Security integration tests for the Blazor BFF against a containerized Keycloak.
/// Exercises the real OIDC challenge flow, provider readiness checks,
/// and auth status endpoint behavior.
///
/// These tests prove:
/// 1. The BFF correctly redirects to the containerized Keycloak for OIDC challenge.
/// 2. The redirect includes PKCE (S256), nonce, state, and correct client_id.
/// 3. Auth status endpoint correctly reports anonymous vs. authenticated state.
/// 4. Signout endpoint correctly clears the cookie session.
/// </summary>
[Category(BffTestCategories.Security)]
[ClassDataSource<BffKeycloakFixture>(Shared = SharedType.PerAssembly)]
public class BffSecurityTests : IAsyncDisposable
{
    private readonly BffKeycloakFixture _keycloak;
    private readonly SecurityBlazorBffWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public BffSecurityTests(BffKeycloakFixture keycloak)
    {
        _keycloak = keycloak;
        _factory = new SecurityBlazorBffWebApplicationFactory(
            keycloak.Authority,
            keycloak.MetadataAddress);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    #region Auth Status — Anonymous

    [Test]
    public async Task AuthStatus_Anonymous_ReturnsNotAuthenticated()
    {
        var response = await _client.GetAsync("/auth/status");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<AuthStatusPayload>();
        payload.Should().NotBeNull();
        payload!.IsAuthenticated.Should().BeFalse("anonymous requests should report not authenticated");
    }

    #endregion

    #region OIDC Challenge Redirect

    [Test]
    public async Task Challenge_KeycloakProvider_ShouldRedirectToKeycloak()
    {
        var response = await _client.GetAsync("/auth/challenge?provider=keycloak&returnUrl=/dashboard");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Redirect,
            "OIDC challenge must respond with a 302 redirect");

        var location = response.Headers.Location?.ToString();
        location.Should().NotBeNullOrEmpty("the redirect must have a Location header");

        location.Should().Contain("/realms/ISLAMU/protocol/openid-connect/auth",
            "the redirect must point to Keycloak's authorization endpoint");

        location.Should().Contain("response_type=code",
            "the redirect must request authorization code flow");

        location.Should().Contain("client_id=islamu-event-blazor",
            "the redirect must include the correct client_id");

        location.Should().Contain("redirect_uri=",
            "the redirect must include a callback URI");

        location.Should().Contain("scope=",
            "the redirect must include OIDC scopes");

        location.Should().Contain("state=",
            "the redirect must include a state parameter for CSRF protection");
    }

    [Test]
    public async Task Challenge_KeycloakProvider_ShouldIncludeNonce()
    {
        var response = await _client.GetAsync("/auth/challenge?provider=keycloak");

        var location = response.Headers.Location?.ToString();
        location.Should().Contain("nonce=",
            "the OIDC request must include a nonce for replay protection");
    }

    [Test]
    public async Task Challenge_KeycloakProvider_ShouldIncludePkce()
    {
        var response = await _client.GetAsync("/auth/challenge?provider=keycloak");

        var location = response.Headers.Location?.ToString();
        location.Should().Contain("code_challenge=",
            "PKCE must be enabled — the challenge must include a code_challenge parameter");
        location.Should().Contain("code_challenge_method=S256",
            "PKCE must use S256 code challenge method");
    }

    #endregion

    #region Provider Discovery

    [Test]
    public async Task AuthProviders_ShouldReturnKeycloakAsReady()
    {
        var response = await _client.GetAsync("/auth/providers");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Keycloak",
            "the containerized Keycloak should be listed as a provider");
        json.Should().Contain("keycloak",
            "the provider name should be 'keycloak'");
    }

    #endregion

    #region Challenge — No Provider Specified

    [Test]
    public async Task Challenge_NoProvider_WithSingleReadyProvider_ShouldRedirectToKeycloak()
    {
        var response = await _client.GetAsync("/auth/challenge?returnUrl=/");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Redirect,
            "with a single registered provider, challenge should auto-redirect");

        var location = response.Headers.Location?.ToString();
        location.Should().Contain("/realms/ISLAMU/protocol/openid-connect/auth",
            "should auto-select Keycloak when it's the only ready provider");
    }

    #endregion

    #region Login Redirect

    [Test]
    public async Task Login_ShouldRedirectToChallenge()
    {
        var response = await _client.GetAsync("/auth/login?provider=keycloak&returnUrl=/dashboard");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Redirect);

        var location = response.Headers.Location?.ToString();
        location.Should().Contain("/auth/challenge",
            "/auth/login should redirect to /auth/challenge");
        location.Should().Contain("provider=keycloak",
            "the provider should be forwarded to the challenge endpoint");
    }

    #endregion

    #region Signout

    [Test]
    public async Task Signout_Anonymous_ShouldRedirect()
    {
        var response = await _client.GetAsync("/auth/signout?returnUrl=/");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Redirect,
            "signout should redirect even for anonymous users");
    }

    [Test]
    public async Task Signout_ShouldClearCookies()
    {
        var response = await _client.GetAsync("/auth/signout?returnUrl=/");

        var setCookieHeaders = response.Headers.GetValues("Set-Cookie").ToList();

        setCookieHeaders.Should().Contain(c =>
            c.StartsWith(".AspNetCore.Cookies=") && c.Contains("expires="),
            "signout should expire the authentication cookie");
    }

    #endregion

    private sealed class AuthStatusPayload
    {
        public bool IsAuthenticated { get; set; }
        public string? Name { get; set; }
    }
}
