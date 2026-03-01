// ABOUTME: Integration tests for instance onboarding governance endpoints and render-policy flows.
// ABOUTME: Verifies save/retrieve behavior with setup-secret gating and preset validation rules.

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

public class InstanceOnboardingControllerTests
{
    private const string BaseUrl = "/api/instanceonboarding";
    private const string SetupSecret = "integration-setup-secret";

    [Test]
    public async Task GetStatus_Anonymous_ShouldReturnOk()
    {
        using var factory = CreateFactoryWithSetupSecret();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"{BaseUrl}/status");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task CompleteThenGetSettings_WithValidSetupSecret_ShouldSaveAndRetrieve()
    {
        using var factory = CreateFactoryWithSetupSecret();
        using var client = factory.CreateClient();

        var userId = Guid.NewGuid();
        await EnsureUserExistsAsync(factory, userId);

        var completePayload = CreateValidSettings();
        completePayload.RenderPolicyPreset = "CustomAdvanced";
        completePayload.EnableAdvancedRenderPolicyOverrides = true;
        completePayload.PublicSeoPrerenderEnabled = true;

        using var completeRequest = CreateInstanceAdminRequest(HttpMethod.Post, $"{BaseUrl}/complete", userId, completePayload, includeSetupSecret: true);
        var completeResponse = await client.SendAsync(completeRequest);

        await Assert.That(completeResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var completeBody = await completeResponse.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
        await Assert.That(completeBody).IsNotNull();
        await Assert.That(completeBody!.Success).IsTrue();

        using var getRequest = CreateInstanceAdminRequest(HttpMethod.Get, $"{BaseUrl}/settings", userId, body: null, includeSetupSecret: false);
        var getResponse = await client.SendAsync(getRequest);

        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var settings = await getResponse.Content.ReadFromJsonAsync<InstanceGovernanceSettingsDto>();
        await Assert.That(settings).IsNotNull();
        await Assert.That(settings!.RenderPolicyPreset).IsEqualTo("CustomAdvanced");
        await Assert.That(settings.EnableAdvancedRenderPolicyOverrides).IsTrue();
    }

    [Test]
    public async Task UpdateSettings_WithMissingSetupSecret_ShouldReturnForbidden()
    {
        using var factory = CreateFactoryWithSetupSecret();
        using var client = factory.CreateClient();

        using var request = CreateInstanceAdminRequest(HttpMethod.Put, $"{BaseUrl}/settings", Guid.NewGuid(), CreateValidSettings(), includeSetupSecret: false);
        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Complete_WithCustomAdvancedPresetAndOverridesDisabled_ShouldReturnBadRequest()
    {
        using var factory = CreateFactoryWithSetupSecret();
        using var client = factory.CreateClient();

        var userId = Guid.NewGuid();
        await EnsureUserExistsAsync(factory, userId);

        var invalidPayload = CreateValidSettings();
        invalidPayload.RenderPolicyPreset = "CustomAdvanced";
        invalidPayload.EnableAdvancedRenderPolicyOverrides = false;

        using var completeRequest = CreateInstanceAdminRequest(HttpMethod.Post, $"{BaseUrl}/complete", userId, invalidPayload, includeSetupSecret: true);
        var completeResponse = await client.SendAsync(completeRequest);

        await Assert.That(completeResponse.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

        var responseBody = await completeResponse.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
        await Assert.That(responseBody).IsNotNull();
        await Assert.That(responseBody!.Success).IsFalse();
        await Assert.That(responseBody.Message).IsEqualTo("Invalid instance governance settings.");
        await Assert.That((responseBody.Errors ?? new List<string>()).Any(e => e.Contains("EnableAdvancedRenderPolicyOverrides must be true when RenderPolicyPreset is CustomAdvanced", StringComparison.Ordinal))).IsTrue();
    }

    private static async Task EnsureUserExistsAsync(AuthenticatedWebApplicationFactory factory, Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();

        var exists = await dbContext.Users.AnyAsync(x => x.Id == userId);
        if (exists)
        {
            return;
        }

        dbContext.Users.Add(new User
        {
            Id = userId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            Pii = new UserPii
            {
                UserId = userId,
                Email = $"{userId:N}@integration.test",
                FirstName = "Instance",
                LastName = "Admin"
            }
        });

        await dbContext.SaveChangesAsync();
    }

    private static AuthenticatedWebApplicationFactory CreateFactoryWithSetupSecret()
    {
        Environment.SetEnvironmentVariable("SETUP_SECRET", SetupSecret);
        return new AuthenticatedWebApplicationFactory();
    }

    private static HttpRequestMessage CreateInstanceAdminRequest(HttpMethod method, string url, Guid userId, object? body, bool includeSetupSecret)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add(TestAuthHandler.AuthHeaderName, TestAuthHandler.CreateInstanceAdminHeaderValue(userId));

        if (includeSetupSecret)
        {
            request.Headers.Add("X-Setup-Secret", SetupSecret);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

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
}
