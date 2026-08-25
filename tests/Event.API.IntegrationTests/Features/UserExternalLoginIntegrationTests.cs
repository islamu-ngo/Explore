// ABOUTME: Integration tests for verified authentication flows that resolve internal external-login links.
// ABOUTME: Proves provider identity resolution remains intact after public external-login CRUD removal.

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Event.Api.IntegrationTests.Features;

[NotInParallel("AuthenticatedApiFixture")]
[ClassDataSource<AuthenticatedApiTestFixture>(Shared = SharedType.PerAssembly)]
public class UserExternalLoginIntegrationTests
{
    private readonly AuthenticatedApiTestFixture _fixture;

    public UserExternalLoginIntegrationTests(AuthenticatedApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task SyncUser_GoogleVerifiedEmail_ShouldAutoMatchExistingUserAndCreateGoogleLink()
    {
        var existingUserId = Guid.NewGuid();
        await EnsureUserExistsAsync(existingUserId, "shared@example.com");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/user/sync");
        request.Headers.Add(TestAuthHandler.AuthHeaderName,
            CreateCustomAuthHeader(
                ("sub", "google-sub-123"),
                ("name", "Shared User"),
                ("email", "shared@example.com"),
                ("given_name", "Shared"),
                ("family_name", "User"),
                ("preferred_username", "shared.user"),
                ("email_verified", "true"),
                ("idp", "google")));

        var response = await _fixture.Client.SendAsync(request);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.IsSuccess).IsTrue();
        await Assert.That(body.Id).IsEqualTo(existingUserId);

        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var googleLink = await dbContext.UserExternalLogins
            .SingleOrDefaultAsync(x => x.Provider == "google" && x.ProviderKey == "google-sub-123");
        await Assert.That(googleLink).IsNotNull();
        await Assert.That(googleLink!.UserId).IsEqualTo(existingUserId);
    }

    [Test]
    public async Task SyncUser_AtprotoWithoutEmailWithoutExplicitLink_ShouldReturnBadRequest()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/user/sync");
        request.Headers.Add(TestAuthHandler.AuthHeaderName,
            CreateCustomAuthHeader(
                ("sub", "did:plc:abc123"),
                ("name", "ATProto User"),
                ("preferred_username", "did:plc:abc123"),
                ("idp", "atproto")));

        var response = await _fixture.Client.SendAsync(request);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        await Assert.That(problemDetails).IsNotNull();
        await Assert.That(problemDetails!.Detail).Contains("explicitly linked");
    }

    [Test]
    public async Task SyncUser_AtprotoWithoutEmailWithExplicitLink_ShouldResolveExistingUser()
    {
        var existingUserId = Guid.NewGuid();
        await EnsureUserExistsAsync(existingUserId, "linked@example.com");
        await SeedExternalLoginAsync(existingUserId, "atproto", "did:plc:linked123");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/user/sync");
        request.Headers.Add(TestAuthHandler.AuthHeaderName,
            CreateCustomAuthHeader(
                ("sub", "did:plc:linked123"),
                ("name", "AT Linked"),
                ("preferred_username", "did:plc:linked123"),
                ("idp", "atproto")));

        var response = await _fixture.Client.SendAsync(request);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.IsSuccess).IsTrue();
        await Assert.That(body.Id).IsEqualTo(existingUserId);
    }

    private async Task EnsureUserExistsAsync(Guid userId, string? email = null)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();

        if (await dbContext.Users.AnyAsync(x => x.Id == userId))
        {
            return;
        }

        dbContext.Users.Add(new User
        {
            Id = userId,
            AuthProvider = "keycloak",
            AuthProviderId = userId.ToString(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            Pii = new UserPii
            {
                UserId = userId,
                Email = email ?? $"{userId:N}@integration.test",
                FirstName = "Integration",
                LastName = "User"
            }
        });

        await dbContext.SaveChangesAsync();
    }

    private async Task SeedExternalLoginAsync(Guid userId, string provider, string providerKey)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();

        dbContext.UserExternalLogins.Add(new UserExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            User = null!,
            TenantId = PlatformDefaults.DefaultTenantId,
            Tenant = null!,
            Provider = provider,
            ProviderKey = providerKey,
            ProviderDisplayName = provider,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        });
        await dbContext.SaveChangesAsync();
    }

    private static string CreateCustomAuthHeader(params (string Type, string Value)[] claims)
    {
        var payload = claims.Select(claim => new TestClaimPayload { Type = claim.Type, Value = claim.Value }).ToList();
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
    }

    private sealed class TestClaimPayload
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
