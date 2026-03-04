// ABOUTME: Integration tests for instance onboarding governance endpoints and render-policy flows.
// ABOUTME: Verifies save/retrieve behavior with setup-secret gating and preset validation rules.

using System.Net;
using System.Net.Http.Json;
using Event.Api.IntegrationTests.Fixtures;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Constants;
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

    [Test]
    public async Task UpdateAuthProviderConfiguration_AdminEndpoint_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        using var factory = CreateFactoryWithSetupSecret();
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync($"{BaseUrl}/admin/auth-provider-configuration", CreateGoogleOnlyAuthProviderConfiguration());

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UpdateAuthProviderConfiguration_WhenUserIsNotInstanceAdmin_ShouldReturnForbidden()
    {
        using var factory = CreateFactoryWithSetupSecret();
        using var client = factory.CreateClient();

        var userId = Guid.NewGuid();
        await EnsureUserExistsAsync(factory, userId);

        using var request = CreateInstanceAdminRequest(
            HttpMethod.Put,
            $"{BaseUrl}/admin/auth-provider-configuration",
            userId,
            CreateGoogleOnlyAuthProviderConfiguration(),
            includeSetupSecret: false);

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task UpdateAuthProviderConfiguration_WhenItWouldDisableAllLinkedAdminProviders_ShouldReturnBadRequest()
    {
        using var factory = CreateFactoryWithSetupSecret();
        using var client = factory.CreateClient();

        var userId = Guid.NewGuid();
        await EnsureUserExistsAsync(factory, userId);
        await EnsureInstanceAdminRoleAsync(factory, userId);

        using var request = CreateInstanceAdminRequest(
            HttpMethod.Put,
            $"{BaseUrl}/admin/auth-provider-configuration",
            userId,
            CreateGoogleOnlyAuthProviderConfiguration(),
            includeSetupSecret: false);

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

        var responseBody = await response.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
        await Assert.That(responseBody).IsNotNull();
        await Assert.That(responseBody!.Success).IsFalse();
        await Assert.That(responseBody.Message).Contains("Cannot disable all authentication providers linked");
    }

    [Test]
    public async Task UpdateAuthProviderConfiguration_WhenAdminHasLinkedEnabledProvider_ShouldUpdateAndReturnConfiguration()
    {
        using var factory = CreateFactoryWithSetupSecret();
        using var client = factory.CreateClient();

        var userId = Guid.NewGuid();
        await EnsureUserExistsAsync(factory, userId);
        await EnsureInstanceAdminRoleAsync(factory, userId);
        await EnsureUserExternalLoginAsync(factory, userId, "google", $"google-{userId:N}");

        using var updateRequest = CreateInstanceAdminRequest(
            HttpMethod.Put,
            $"{BaseUrl}/admin/auth-provider-configuration",
            userId,
            CreateGoogleOnlyAuthProviderConfiguration(),
            includeSetupSecret: false);

        var updateResponse = await client.SendAsync(updateRequest);
        await Assert.That(updateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var updateBody = await updateResponse.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
        await Assert.That(updateBody).IsNotNull();
        await Assert.That(updateBody!.Success).IsTrue();

        using var getRequest = CreateInstanceAdminRequest(
            HttpMethod.Get,
            $"{BaseUrl}/auth-provider-configuration",
            userId,
            body: null,
            includeSetupSecret: false);

        var getResponse = await client.SendAsync(getRequest);
        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var config = await getResponse.Content.ReadFromJsonAsync<AuthProviderConfigurationDto>();
        await Assert.That(config).IsNotNull();
        await Assert.That(config!.KeycloakEnabled).IsFalse();
        await Assert.That(config.GoogleSsoEnabled).IsTrue();
    }

    private static async Task EnsureInstanceAdminRoleAsync(AuthenticatedWebApplicationFactory factory, Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();

        var bootstrap = await dbContext.InstanceBootstrapStates
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (bootstrap == null)
        {
            dbContext.InstanceBootstrapStates.Add(new InstanceBootstrapState
            {
                Id = Guid.NewGuid(),
                IsCompleted = true,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                CompletedByUserId = userId,
                SelectedDeploymentMode = "SingleTenant"
            });
        }
        else
        {
            bootstrap.IsCompleted = true;
            bootstrap.CompletedAt = DateTime.UtcNow;
            bootstrap.CompletedByUserId = userId;
            bootstrap.SelectedDeploymentMode ??= "SingleTenant";
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsureUserExternalLoginAsync(
        AuthenticatedWebApplicationFactory factory,
        Guid userId,
        string provider,
        string providerKey)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();

        var exists = await dbContext.UserExternalLogins
            .AnyAsync(x => x.UserId == userId && x.Provider == provider && x.ProviderKey == providerKey);

        if (exists)
        {
            return;
        }

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
            AuthProvider = "keycloak",
            AuthProviderId = userId.ToString(),
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

    private static AuthProviderConfigurationDto CreateGoogleOnlyAuthProviderConfiguration()
    {
        return new AuthProviderConfigurationDto
        {
            KeycloakEnabled = false,
            KeycloakAuthority = string.Empty,
            KeycloakClientId = string.Empty,
            KeycloakClientSecret = string.Empty,
            AtprotoLoginEnabled = false,
            AtprotoPublicUrl = string.Empty,
            GoogleSsoEnabled = true,
            GoogleClientId = "google-client-id",
            GoogleClientSecret = "google-client-secret",
            LockKeycloakEnabled = false,
            LockAtprotoLoginEnabled = false,
            LockGoogleSsoEnabled = false
        };
    }
}
