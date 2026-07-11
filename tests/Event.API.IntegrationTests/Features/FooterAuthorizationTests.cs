// ABOUTME: Integration tests for footer management authorization posture.
// ABOUTME: Ensures authenticated footer writes still fail closed when resource authorization denies.

using System.Net;
using System.Net.Http.Json;
using Event.Api.IntegrationTests.Fixtures;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

public sealed class FooterAuthorizationTests
{
    [Test]
    public async Task CreateLinkGroup_WhenAuthorizationProviderDenies_ReturnsForbidden()
    {
        await using var factory = new AuthenticatedWebApplicationFactory
        {
            AuthorizationProviderOverride = new StubAuthorizationProvider { AllowAll = false }
        };
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/footer/link-groups")
        {
            Content = JsonContent.Create(new { title = "Main" })
        };
        request.Headers.Add(TestAuthHandler.AuthHeaderName, TestAuthHandler.CreateAuthHeaderValue(Guid.NewGuid()));

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }
}
