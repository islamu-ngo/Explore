// ABOUTME: Token-boundary tests for browser-readable current-user BFF identity projection.
// ABOUTME: Ensures server-held token-shaped claims are not returned by /bff/me responses.

using FluentAssertions;

namespace Explore.Blazor.IntegrationTests.Endpoints;

public sealed class BffCurrentUserEndpointTokenBoundaryTests
{
    [Test]
    public async Task CurrentUser_WhenPrincipalContainsTokenClaims_DoesNotReturnTokens()
    {
        const string accessToken = "server-held-access-token";
        const string refreshToken = "server-held-refresh-token";
        const string idToken = "server-held-id-token";

        using var factory = new BlazorBffWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var headerValue = TestAuthHandler.CreateAuthHeaderValue(
            Guid.NewGuid(),
            "Token Boundary User",
            ("access_token", accessToken),
            ("refresh_token", refreshToken),
            ("id_token", idToken),
            ("name", "Token Boundary User"),
            ("email", "token-boundary@example.test"));
        using var request = new HttpRequestMessage(HttpMethod.Get, "/bff/me");
        request.Headers.Add(TestAuthHandler.AuthHeaderName, headerValue);

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("Token Boundary User");
        body.Should().NotContain("access_token");
        body.Should().NotContain("refresh_token");
        body.Should().NotContain("id_token");
        body.Should().NotContain(accessToken);
        body.Should().NotContain(refreshToken);
        body.Should().NotContain(idToken);
    }
}
