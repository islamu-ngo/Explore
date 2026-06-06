// ABOUTME: Authenticated integration tests for user external login linking and unlinking behavior.
// ABOUTME: Verifies account-linking lifecycle and last-provider unlink safety at API level.

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using Event.Api.IntegrationTests.Helpers;
using Explore.Application.DTOs.UserExternalLogin;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Persistence;
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
    public async Task CreateUserExternalLogin_WithAuthenticatedRequest_ShouldCreateAndReturnSuccess()
    {
        var userId = Guid.NewGuid();
        await EnsureUserExistsAsync(userId);

        var payload = new CreateUserExternalLoginDto
        {
            UserId = userId,
            TenantId = PlatformDefaults.DefaultTenantId,
            Provider = "atproto",
            ProviderKey = $"did:plc:{Guid.NewGuid():N}",
            ProviderDisplayName = "AT Protocol"
        };

        using var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Post, "/api/userexternallogin", userId);
        request.Content = JsonContent.Create(payload);

        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.Success).IsTrue();

        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var exists = await dbContext.UserExternalLogins.AnyAsync(x => x.Id == body.Id);
        await Assert.That(exists).IsTrue();
    }

    [Test]
    public async Task DeleteUserExternalLogin_WhenLastProvider_ShouldReturnBadRequest()
    {
        var userId = Guid.NewGuid();
        await EnsureUserExistsAsync(userId);
        var loginId = await SeedExternalLoginAsync(userId, "google", $"google-{userId:N}");

        using var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Delete, $"/api/userexternallogin/{loginId}", userId);
        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task DeleteUserExternalLogin_WhenMultipleProviders_ShouldReturnNoContent()
    {
        var userId = Guid.NewGuid();
        await EnsureUserExistsAsync(userId);
        var firstLoginId = await SeedExternalLoginAsync(userId, "google", $"google-{userId:N}");
        _ = await SeedExternalLoginAsync(userId, "atproto", $"did:plc:{Guid.NewGuid():N}");

        using var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Delete, $"/api/userexternallogin/{firstLoginId}", userId);
        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var deleted = await dbContext.UserExternalLogins.AnyAsync(x => x.Id == firstLoginId);
        await Assert.That(deleted).IsFalse();
    }

    [Test]
    public async Task UpdateUserExternalLogin_WithMismatchedRouteId_ShouldReturnValidationProblemDetails()
    {
        var userId = Guid.NewGuid();
        await EnsureUserExistsAsync(userId);
        var loginId = await SeedExternalLoginAsync(userId, "google", $"google-{userId:N}");

        var payload = new UpdateUserExternalLoginDto
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = PlatformDefaults.DefaultTenantId,
            Provider = "google",
            ProviderKey = $"google-{userId:N}",
            ProviderDisplayName = "Google"
        };

        using var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Put, $"/api/userexternallogin/{loginId}", userId);
        request.Content = JsonContent.Create(payload);

        var response = await _fixture.Client.SendAsync(request);

        await AssertUserExternalLoginValidationProblemAsync(
            response,
            "User external login ID mismatch.",
            "User external login ID mismatch.");
    }

    [Test]
    public async Task UpdateUserExternalLogin_WithValidationFailure_ShouldReturnValidationProblemDetails()
    {
        var userId = Guid.NewGuid();
        await EnsureUserExistsAsync(userId);
        var loginId = await SeedExternalLoginAsync(userId, "google", $"google-{userId:N}");

        var payload = new UpdateUserExternalLoginDto
        {
            Id = loginId,
            UserId = Guid.NewGuid(),
            TenantId = PlatformDefaults.DefaultTenantId,
            Provider = "google",
            ProviderKey = $"google-{userId:N}",
            ProviderDisplayName = "Google"
        };

        using var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Put, $"/api/userexternallogin/{loginId}", userId);
        request.Content = JsonContent.Create(payload);

        var response = await _fixture.Client.SendAsync(request);

        await AssertUserExternalLoginValidationProblemAsync(
            response,
            "User External Login update failed.",
            "User does not exist");
    }

    [Test]
    public async Task UpdateUserExternalLogin_WhenLoginIsMissing_ShouldReturnNotFoundProblemDetails()
    {
        var userId = Guid.NewGuid();
        await EnsureUserExistsAsync(userId);
        var missingLoginId = Guid.NewGuid();

        var payload = new UpdateUserExternalLoginDto
        {
            Id = missingLoginId,
            UserId = userId,
            TenantId = PlatformDefaults.DefaultTenantId,
            Provider = "google",
            ProviderKey = $"google-{userId:N}",
            ProviderDisplayName = "Google"
        };

        using var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Put, $"/api/userexternallogin/{missingLoginId}", userId);
        request.Content = JsonContent.Create(payload);

        var response = await _fixture.Client.SendAsync(request);

        await AssertUserExternalLoginNotFoundProblemAsync(response);
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
        await Assert.That(body!.Success).IsTrue();
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

        var body = await response.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.Success).IsFalse();
        await Assert.That(body.Message).Contains("explicitly linked");
    }

    [Test]
    public async Task SyncUser_AtprotoWithoutEmailWithExplicitLink_ShouldResolveExistingUser()
    {
        var existingUserId = Guid.NewGuid();
        await EnsureUserExistsAsync(existingUserId, "linked@example.com");
        _ = await SeedExternalLoginAsync(existingUserId, "atproto", "did:plc:linked123");

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
        await Assert.That(body!.Success).IsTrue();
        await Assert.That(body.Id).IsEqualTo(existingUserId);
    }

    private async Task EnsureUserExistsAsync(Guid userId, string? email = null)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();

        var exists = await dbContext.Users.AnyAsync(x => x.Id == userId);
        if (exists)
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

    private async Task<Guid> SeedExternalLoginAsync(Guid userId, string provider, string providerKey)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();

        var login = new UserExternalLogin
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
        };

        dbContext.UserExternalLogins.Add(login);
        await dbContext.SaveChangesAsync();
        return login.Id;
    }

    private static async Task AssertUserExternalLoginValidationProblemAsync(
        HttpResponseMessage response,
        string expectedDetail,
        string expectedError)
    {
        await ProblemDetailsAssertions.AssertProblemDetailsAsync(
            response,
            HttpStatusCode.BadRequest,
            "User external login validation failed");
        await AssertProblemJsonContentTypeAsync(response);

        using var document = await ProblemDetailsAssertions.ReadAsJsonAsync(response);
        var root = document.RootElement;

        await Assert.That(root.GetProperty("detail").GetString()).IsEqualTo(expectedDetail);
        await Assert.That(root.GetProperty("code").GetString()).IsEqualTo("validation_failed");

        var errors = root.GetProperty("errors").GetProperty("userExternalLogin");
        await Assert.That(errors.GetArrayLength()).IsGreaterThanOrEqualTo(1);
        await Assert.That(errors[0].GetString()).IsEqualTo(expectedError);
    }

    private static async Task AssertUserExternalLoginNotFoundProblemAsync(HttpResponseMessage response)
    {
        await ProblemDetailsAssertions.AssertProblemDetailsAsync(
            response,
            HttpStatusCode.NotFound,
            "User external login not found");
        await AssertProblemJsonContentTypeAsync(response);

        using var document = await ProblemDetailsAssertions.ReadAsJsonAsync(response);
        var root = document.RootElement;

        await Assert.That(root.GetProperty("detail").GetString()).IsEqualTo("User External Login not found.");
        await Assert.That(root.GetProperty("code").GetString()).IsEqualTo("resource_not_found");
    }

    private static async Task AssertProblemJsonContentTypeAsync(HttpResponseMessage response)
    {
        var contentType = response.Content.Headers.ContentType?.MediaType;
        await Assert.That(contentType).IsEqualTo("application/problem+json");
    }

    private static string CreateCustomAuthHeader(params (string Type, string Value)[] claims)
    {
        var payload = claims.Select(claim => new TestClaimPayload { Type = claim.Type, Value = claim.Value }).ToList();
        var json = JsonSerializer.Serialize(payload);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    private sealed class TestClaimPayload
    {
        public string Type { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;
    }
}
