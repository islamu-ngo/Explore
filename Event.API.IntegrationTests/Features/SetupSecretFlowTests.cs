// ABOUTME: Integration tests for the setup-secret validation flow.
// ABOUTME: Covers correct/wrong secret validation, tenant-exempt path behavior, and 410 after completion.

using System.Net;
using System.Net.Http.Json;
using Event.Api.IntegrationTests.Fixtures;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Event.Api.IntegrationTests.Features;

public class SetupSecretFlowTests
{
    private const string BaseUrl = "/api/instanceonboarding";
    private const string SetupSecret = "integration-test-secret-flow";

    [Test]
    public async Task ValidateSecret_WithCorrectSecret_ShouldReturn200WithValidTrue()
    {
        using var factory = CreateFactoryWithSetupSecret();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync($"{BaseUrl}/validate-secret", new { secret = SetupSecret });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ValidateSecretResponse>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.Valid).IsTrue();
    }

    [Test]
    public async Task ValidateSecret_WithWrongSecret_ShouldReturn200WithValidFalse()
    {
        using var factory = CreateFactoryWithSetupSecret();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync($"{BaseUrl}/validate-secret", new { secret = "wrong-secret" });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ValidateSecretResponse>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.Valid).IsFalse();
    }

    [Test]
    public async Task ValidateSecret_WithoutTenantHeader_ShouldSucceed()
    {
        // The validate-secret endpoint is tenant-exempt so it works before any tenant exists.
        // No tenant header is set — the middleware should skip tenant resolution for this path.
        using var factory = CreateFactoryWithSetupSecret();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync($"{BaseUrl}/validate-secret", new { secret = SetupSecret });

        // Should NOT return 404 (tenant not found) — path is exempt
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.NotFound);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task ValidateSecret_AfterBootstrapComplete_ShouldReturn410Gone()
    {
        using var factory = CreateFactoryWithSetupSecret();
        using var client = factory.CreateClient();

        var userId = Guid.NewGuid();
        await EnsureUserExistsAsync(factory, userId);

        // Complete onboarding to end setup mode
        var completePayload = CreateValidSettings();
        using var completeRequest = CreateInstanceAdminRequest(
            HttpMethod.Post, $"{BaseUrl}/complete", userId, completePayload, includeSetupSecret: true);
        var completeResponse = await client.SendAsync(completeRequest);
        await Assert.That(completeResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Now validate-secret should return 410 Gone
        var response = await client.PostAsJsonAsync($"{BaseUrl}/validate-secret", new { secret = SetupSecret });
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Gone);
    }

    [Test]
    public async Task GetOnboardingStatus_WithoutTenantHeader_ShouldSucceed()
    {
        // The status endpoint is also under /api/InstanceOnboarding — tenant-exempt
        using var factory = CreateFactoryWithSetupSecret();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"{BaseUrl}/status");

        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.NotFound);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Complete_WithValidSecretHeader_ShouldSucceed()
    {
        using var factory = CreateFactoryWithSetupSecret();
        using var client = factory.CreateClient();

        var userId = Guid.NewGuid();
        await EnsureUserExistsAsync(factory, userId);

        var completePayload = CreateValidSettings();
        using var completeRequest = CreateInstanceAdminRequest(
            HttpMethod.Post, $"{BaseUrl}/complete", userId, completePayload, includeSetupSecret: true);
        var completeResponse = await client.SendAsync(completeRequest);

        await Assert.That(completeResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await completeResponse.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.Success).IsTrue();
    }

    [Test]
    public async Task Complete_WithInvalidSecretHeader_ShouldReturnForbidden()
    {
        using var factory = CreateFactoryWithSetupSecret();
        using var client = factory.CreateClient();

        var userId = Guid.NewGuid();
        await EnsureUserExistsAsync(factory, userId);

        var completePayload = CreateValidSettings();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/complete");
        request.Headers.Add(TestAuthHandler.AuthHeaderName, TestAuthHandler.CreateInstanceAdminHeaderValue(userId));
        request.Headers.Add("X-Setup-Secret", "wrong-secret");
        request.Content = JsonContent.Create(completePayload);

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    #region Helpers

    private static AuthenticatedWebApplicationFactory CreateFactoryWithSetupSecret()
    {
        Environment.SetEnvironmentVariable("SETUP_SECRET", SetupSecret);
        return new AuthenticatedWebApplicationFactory();
    }

    private static async Task EnsureUserExistsAsync(AuthenticatedWebApplicationFactory factory, Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();

        var exists = await dbContext.Users.AnyAsync(x => x.Id == userId);
        if (exists) return;

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
                Email = $"{userId:N}@integration.test",
                FirstName = "Test",
                LastName = "User"
            }
        });

        await dbContext.SaveChangesAsync();
    }

    private static HttpRequestMessage CreateInstanceAdminRequest(
        HttpMethod method, string url, Guid userId, object? body, bool includeSetupSecret)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add(TestAuthHandler.AuthHeaderName, TestAuthHandler.CreateInstanceAdminHeaderValue(userId));

        if (includeSetupSecret)
            request.Headers.Add("X-Setup-Secret", SetupSecret);

        if (body is not null)
            request.Content = JsonContent.Create(body);

        return request;
    }

    private static InstanceGovernanceSettingsDto CreateValidSettings()
    {
        return new InstanceGovernanceSettingsDto
        {
            DeploymentMode = "SingleTenant",
            AllowTenantSelfServiceRegistration = false,
            DefaultPublicHomePage = "EventList",
            LockTenantHomePagePreference = false,
            RenderPolicyVersion = 1,
            RenderPolicyPreset = "SeoBalanced",
            EnableAdvancedRenderPolicyOverrides = false,
            GlobalRenderMode = "InteractiveAuto",
            GlobalPrerenderEnabled = false,
            PublicSeoRenderMode = "InteractiveAuto",
            PublicSeoPrerenderEnabled = true,
            OperationalRenderMode = "InteractiveAuto",
            OperationalPrerenderEnabled = false,
            AdminRenderMode = "InteractiveAuto",
            AdminPrerenderEnabled = false,
            OnboardingRenderMode = "InteractiveAuto",
            OnboardingPrerenderEnabled = false,
            DisallowInteractiveServerOnOnboarding = true
        };
    }

    private sealed class ValidateSecretResponse
    {
        public bool Valid { get; set; }
    }

    #endregion
}
