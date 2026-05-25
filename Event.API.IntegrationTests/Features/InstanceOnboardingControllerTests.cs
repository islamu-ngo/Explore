// ABOUTME: Integration tests for instance onboarding governance endpoints and render-policy flows.
// ABOUTME: Verifies save/retrieve behavior with setup-secret gating and preset validation rules.

using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using Explore.Application.DTOs.Instance;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Event.Api.IntegrationTests.Features;

public class InstanceOnboardingControllerTests
{
    private const string BaseUrl = "/api/instanceonboarding";
    private const string SettingsBaseUrl = "/api/instance/settings";
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
    public async Task GetSystemOnboardingStatus_WithConfiguredMultiTenant_ShouldReturnPublicMode()
    {
        using var factory = CreateFactoryWithSetupSecret(new Dictionary<string, string?>
        {
            ["Deployment:Mode"] = "MultiTenant"
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/system/onboarding-status");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var status = await response.Content.ReadFromJsonAsync<SystemOnboardingStatusDto>();
        await Assert.That(status).IsNotNull();
        await Assert.That(status!.RequiresOnboarding).IsTrue();
        await Assert.That(status.DeploymentMode).IsEqualTo("MultiTenant");
    }

    [Test]
    public async Task Complete_WithValidPayload_ShouldSucceedAndPersistDeploymentMode()
    {
        using var factory = CreateFactoryWithSetupSecret();
        using var client = factory.CreateClient();

        var userId = Guid.NewGuid();
        await EnsureUserExistsAsync(factory, userId);

        var completePayload = CreateValidOnboardingRequest();

        using var completeRequest = CreateInstanceAdminRequest(HttpMethod.Post, $"{BaseUrl}/complete", userId, completePayload, includeSetupSecret: true);
        var completeResponse = await client.SendAsync(completeRequest);

        await Assert.That(completeResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var completeBody = await completeResponse.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
        await Assert.That(completeBody).IsNotNull();
        await Assert.That(completeBody!.Success).IsTrue();

        using var getRequest = CreateInstanceAdminRequest(HttpMethod.Get, $"{SettingsBaseUrl}/deployment-mode", userId, body: null, includeSetupSecret: false);
        var getResponse = await client.SendAsync(getRequest);

        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var deploymentMode = await getResponse.Content.ReadFromJsonAsync<DeploymentModeDto>(TestJsonOptions.Default);
        await Assert.That(deploymentMode).IsNotNull();
        await Assert.That(deploymentMode!.Mode).IsEqualTo(DeploymentMode.SingleTenant);
    }

    [Test]
    public async Task Complete_WithSidOnlyPrincipalAndExternalLogin_ShouldResolveCurrentUserIdWithoutClaimsTransformation()
    {
        using var factory = CreateFactoryWithSetupSecretWithoutClaimsTransformation();
        using var client = factory.CreateClient();

        var internalUserId = Guid.NewGuid();
        const string providerId = "keycloak-external-subject";

        await EnsureUserExistsAsync(factory, internalUserId);
        await EnsureUserExternalLoginAsync(factory, internalUserId, "keycloak", providerId);

        using var request = CreateCustomAuthRequest(
            HttpMethod.Post,
            $"{BaseUrl}/complete",
            CreateValidOnboardingRequest(),
            includeSetupSecret: true,
            new(ClaimTypes.Name, "Sid Only User"),
            new("sid", providerId),
            new("idp", "keycloak"),
            new("email", $"{internalUserId:N}@integration.test"));

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Complete_WithSidOnlyPrincipal_ShouldPersistSidAsAuthProviderId()
    {
        using var factory = CreateFactoryWithSetupSecret();
        using var client = factory.CreateClient();

        var internalUserId = Guid.NewGuid();
        const string providerId = "keycloak-sid-only-subject";
        var email = $"{internalUserId:N}@integration.test";

        using var request = CreateCustomAuthRequest(
            HttpMethod.Post,
            $"{BaseUrl}/complete",
            CreateValidOnboardingRequest(),
            includeSetupSecret: true,
            new(ClaimTypes.Name, "Sid Only User"),
            new("internal_user_id", internalUserId.ToString()),
            new("sid", providerId),
            new("idp", "keycloak"),
            new("email", email));

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var createdUser = await dbContext.Users.SingleAsync(x => x.Id == internalUserId);
        var externalLogin = await dbContext.UserExternalLogins.SingleAsync(x => x.UserId == internalUserId);

        await Assert.That(createdUser.AuthProvider).IsEqualTo("keycloak");
        await Assert.That(createdUser.AuthProviderId).IsEqualTo(providerId);
        await Assert.That(createdUser.Pii.Email).IsEqualTo(email);
        await Assert.That(externalLogin.Provider).IsEqualTo("keycloak");
        await Assert.That(externalLogin.ProviderKey).IsEqualTo(providerId);
    }

    [Test]
    public async Task Complete_WhenPreflightHasBlockers_ShouldReturnBadRequest()
    {
        using var factory = CreateFactoryWithSetupSecret(new Dictionary<string, string?>
        {
            ["Keycloak:Authority"] = string.Empty,
            ["Keycloak:Audience"] = string.Empty,
            ["Keycloak:ClientId"] = string.Empty,
            ["PublicBaseUrl"] = "https://integration.test"
        });
        using var client = factory.CreateClient();

        var userId = Guid.NewGuid();
        await EnsureUserExistsAsync(factory, userId);

        using var request = CreateInstanceAdminRequest(
            HttpMethod.Post,
            $"{BaseUrl}/complete",
            userId,
            CreateValidOnboardingRequest(),
            includeSetupSecret: true);

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

        var responseBody = await response.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
        await Assert.That(responseBody).IsNotNull();
        await Assert.That(responseBody!.Success).IsFalse();
        await Assert.That(responseBody.Message).IsEqualTo("Instance cannot be launched because critical launch requirements are not met. Please review the blocking issues and try again.");
    }

    [Test]
    public async Task UpdateModuleSettings_WhenUserIsNotInstanceAdmin_ShouldReturnForbidden()
    {
        using var factory = CreateFactoryWithSetupSecret();
        using var client = factory.CreateClient();

        var nonAdminUserId = Guid.NewGuid();
        await EnsureUserExistsAsync(factory, nonAdminUserId);

        using var request = CreateInstanceAdminRequest(HttpMethod.Put, $"{SettingsBaseUrl}/modules", nonAdminUserId,
            new ModuleSettingsDto { EnableIslamicModule = true, EnableTechModule = true }, includeSetupSecret: false);
        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Complete_IgnoresClientDeploymentMode_WhenNoDeploymentModeSecret_ShouldPersistSingleTenant()
    {
        using var factory = CreateFactoryWithSetupSecret();
        using var client = factory.CreateClient();

        var userId = Guid.NewGuid();
        await EnsureUserExistsAsync(factory, userId);

        var clientPayload = new CompleteInstanceOnboardingRequest
        {
            DeploymentMode = DeploymentMode.MultiTenant,
            SiteProfile = new SelfHostOnboardingProfileDto { SiteName = "Integration Test Instance" }
        };

        using var completeRequest = CreateInstanceAdminRequest(HttpMethod.Post, $"{BaseUrl}/complete", userId, clientPayload, includeSetupSecret: true);
        var completeResponse = await client.SendAsync(completeRequest);

        await Assert.That(completeResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var bootstrap = await dbContext.InstanceBootstrapStates.SingleAsync();
        await Assert.That(bootstrap.SelectedDeploymentMode).IsEqualTo("SingleTenant");
    }

    [Test]
    public async Task Complete_UsesConfiguredMultiTenantMode_WhenClientPayloadSaysSingleTenant()
    {
        using var factory = CreateFactoryWithSetupSecret(new Dictionary<string, string?>
        {
            ["Deployment:Mode"] = "MultiTenant"
        });
        using var client = factory.CreateClient();

        var userId = Guid.NewGuid();
        await EnsureUserExistsAsync(factory, userId);

        var clientPayload = new CompleteInstanceOnboardingRequest
        {
            DeploymentMode = DeploymentMode.SingleTenant,
            SiteProfile = new SelfHostOnboardingProfileDto { SiteName = "Integration Test Instance" }
        };

        using var completeRequest = CreateInstanceAdminRequest(HttpMethod.Post, $"{BaseUrl}/complete", userId, clientPayload, includeSetupSecret: true);
        var completeResponse = await client.SendAsync(completeRequest);

        await Assert.That(completeResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var bootstrap = await dbContext.InstanceBootstrapStates.SingleAsync();
        await Assert.That(bootstrap.SelectedDeploymentMode).IsEqualTo("MultiTenant");
    }

    [Test]
    public async Task UpdateAuthProviderConfiguration_AdminEndpoint_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        using var factory = CreateFactoryWithSetupSecret();
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync($"{SettingsBaseUrl}/auth-provider", CreateGoogleOnlyAuthProviderConfiguration());

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
            $"{SettingsBaseUrl}/auth-provider",
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
            $"{SettingsBaseUrl}/auth-provider",
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
            $"{SettingsBaseUrl}/auth-provider",
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
            $"{SettingsBaseUrl}/auth-provider",
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

    [Skip("Category: API integration. Removal: enable when OpenFeature SDK shutdown no longer throws ChannelClosedException during WebApplicationFactory disposal.")]
    [Test]
    public async Task SetupAuthProviderConfigurationFlow_SaveThenComplete_ShouldExposeConfiguredAndProtectPublicReadAfterCompletion()
    {
        using var factory = CreateFactoryWithSetupSecret();
        using var client = factory.CreateClient();

        var userId = Guid.NewGuid();
        await EnsureUserExistsAsync(factory, userId);

        using var saveRequest = CreateInstanceAdminRequest(
            HttpMethod.Put,
            $"{BaseUrl}/auth-provider-configuration",
            userId,
            CreateGoogleOnlyAuthProviderConfiguration(),
            includeSetupSecret: true);
        var saveResponse = await client.SendAsync(saveRequest);
        await Assert.That(saveResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var configuredResponse = await client.GetAsync($"{SettingsBaseUrl}/auth-provider/status");
        await Assert.That(configuredResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var configuredPayload = await configuredResponse.Content.ReadFromJsonAsync<AuthProviderConfiguredResponse>();
        await Assert.That(configuredPayload).IsNotNull();
        await Assert.That(configuredPayload!.Configured).IsTrue();

        var completePayload = CreateValidOnboardingRequest();
        using var completeRequest = CreateInstanceAdminRequest(
            HttpMethod.Post,
            $"{BaseUrl}/complete",
            userId,
            completePayload,
            includeSetupSecret: true);
        var completeResponse = await client.SendAsync(completeRequest);
        await Assert.That(completeResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var anonymousGetResponse = await client.GetAsync($"{SettingsBaseUrl}/auth-provider");
        await Assert.That(anonymousGetResponse.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);

        using var adminGetRequest = CreateInstanceAdminRequest(
            HttpMethod.Get,
            $"{SettingsBaseUrl}/auth-provider",
            userId,
            body: null,
            includeSetupSecret: false);
        var adminGetResponse = await client.SendAsync(adminGetRequest);
        await Assert.That(adminGetResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var adminConfig = await adminGetResponse.Content.ReadFromJsonAsync<AuthProviderConfigurationDto>();
        await Assert.That(adminConfig).IsNotNull();
        await Assert.That(adminConfig!.GoogleSsoEnabled).IsTrue();
    }

    [Test]
    public async Task GetAuthProviderConfigurationInternal_WithSetupSecret_ShouldReturnSecretsWhileAdminEndpointRedacts()
    {
        using var factory = CreateFactoryWithSetupSecret();
        using var client = factory.CreateClient();

        var userId = Guid.NewGuid();
        await EnsureUserExistsAsync(factory, userId);
        await EnsureInstanceAdminRoleAsync(factory, userId);

        using var saveRequest = CreateInstanceAdminRequest(
            HttpMethod.Put,
            $"{BaseUrl}/auth-provider-configuration",
            userId,
            CreateGoogleOnlyAuthProviderConfiguration(),
            includeSetupSecret: true);
        var saveResponse = await client.SendAsync(saveRequest);
        await Assert.That(saveResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        using var adminGetRequest = CreateInstanceAdminRequest(
            HttpMethod.Get,
            $"{SettingsBaseUrl}/auth-provider",
            userId,
            body: null,
            includeSetupSecret: false);
        var adminResponse = await client.SendAsync(adminGetRequest);
        await Assert.That(adminResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var adminConfig = await adminResponse.Content.ReadFromJsonAsync<AuthProviderConfigurationDto>();
        await Assert.That(adminConfig).IsNotNull();
        await Assert.That(adminConfig!.GoogleClientSecret).IsEqualTo(string.Empty);

        using var internalWithoutSecretRequest = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/auth-provider-configuration/internal");
        var internalWithoutSecretResponse = await client.SendAsync(internalWithoutSecretRequest);
        await Assert.That(internalWithoutSecretResponse.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);

        using var internalWithSecretRequest = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/auth-provider-configuration/internal");
        internalWithSecretRequest.Headers.Add("X-Setup-Secret", SetupSecret);
        var internalWithSecretResponse = await client.SendAsync(internalWithSecretRequest);
        await Assert.That(internalWithSecretResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var internalConfig = await internalWithSecretResponse.Content.ReadFromJsonAsync<AuthProviderConfigurationDto>();
        await Assert.That(internalConfig).IsNotNull();
        await Assert.That(internalConfig!.GoogleSsoEnabled).IsTrue();
        await Assert.That(internalConfig.GoogleClientSecret).IsEqualTo("google-client-secret");
    }

    [Test]
    public async Task UpdateAuthorizationProviderConfiguration_AdminEndpoint_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        using var factory = CreateFactoryWithSetupSecret();
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync($"{SettingsBaseUrl}/authz-provider", CreateLocalAuthorizationProviderConfiguration());

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UpdateAuthorizationProviderConfiguration_WhenUserIsNotInstanceAdmin_ShouldReturnForbidden()
    {
        using var factory = CreateFactoryWithSetupSecret();
        using var client = factory.CreateClient();

        var userId = Guid.NewGuid();
        await EnsureUserExistsAsync(factory, userId);

        using var request = CreateInstanceAdminRequest(
            HttpMethod.Put,
            $"{SettingsBaseUrl}/authz-provider",
            userId,
            CreateLocalAuthorizationProviderConfiguration(),
            includeSetupSecret: false);

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task UpdateAuthorizationProviderConfiguration_WhenUserIsInstanceAdmin_ShouldUpdateAndReturnConfiguration()
    {
        using var factory = CreateFactoryWithSetupSecret();
        using var client = factory.CreateClient();

        var userId = Guid.NewGuid();
        await EnsureUserExistsAsync(factory, userId);
        await EnsureInstanceAdminRoleAsync(factory, userId);

        using var updateRequest = CreateInstanceAdminRequest(
            HttpMethod.Put,
            $"{SettingsBaseUrl}/authz-provider",
            userId,
            CreateLocalAuthorizationProviderConfiguration(),
            includeSetupSecret: false);

        var updateResponse = await client.SendAsync(updateRequest);
        await Assert.That(updateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var updateBody = await updateResponse.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
        await Assert.That(updateBody).IsNotNull();
        await Assert.That(updateBody!.Success).IsTrue();

        using var getRequest = CreateInstanceAdminRequest(
            HttpMethod.Get,
            $"{SettingsBaseUrl}/authz-provider",
            userId,
            body: null,
            includeSetupSecret: false);

        var getResponse = await client.SendAsync(getRequest);
        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var config = await getResponse.Content.ReadFromJsonAsync<AuthorizationProviderConfigurationDto>();
        await Assert.That(config).IsNotNull();
        await Assert.That(config!.Provider).IsEqualTo("local");
        await Assert.That(config.CerbosGrpcEndpoint).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task SetupAuthorizationProviderConfigurationFlow_SaveThenComplete_ShouldExposeConfiguredAndProtectPublicReadAfterCompletion()
    {
        using var factory = CreateFactoryWithSetupSecret();
        using var client = factory.CreateClient();

        var userId = Guid.NewGuid();
        await EnsureUserExistsAsync(factory, userId);

        using var saveRequest = CreateInstanceAdminRequest(
            HttpMethod.Put,
            $"{BaseUrl}/authz-provider-configuration",
            userId,
            CreateLocalAuthorizationProviderConfiguration(),
            includeSetupSecret: true);
        var saveResponse = await client.SendAsync(saveRequest);
        await Assert.That(saveResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var configuredResponse = await client.GetAsync($"{SettingsBaseUrl}/authz-provider/status");
        await Assert.That(configuredResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var configuredPayload = await configuredResponse.Content.ReadFromJsonAsync<AuthProviderConfiguredResponse>();
        await Assert.That(configuredPayload).IsNotNull();
        await Assert.That(configuredPayload!.Configured).IsTrue();

        var completePayload = CreateValidOnboardingRequest();
        using var completeRequest = CreateInstanceAdminRequest(
            HttpMethod.Post,
            $"{BaseUrl}/complete",
            userId,
            completePayload,
            includeSetupSecret: true);
        var completeResponse = await client.SendAsync(completeRequest);
        await Assert.That(completeResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var anonymousGetResponse = await client.GetAsync($"{SettingsBaseUrl}/authz-provider");
        await Assert.That(anonymousGetResponse.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);

        using var adminGetRequest = CreateInstanceAdminRequest(
            HttpMethod.Get,
            $"{SettingsBaseUrl}/authz-provider",
            userId,
            body: null,
            includeSetupSecret: false);
        var adminGetResponse = await client.SendAsync(adminGetRequest);
        await Assert.That(adminGetResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var adminConfig = await adminGetResponse.Content.ReadFromJsonAsync<AuthorizationProviderConfigurationDto>();
        await Assert.That(adminConfig).IsNotNull();
        await Assert.That(adminConfig!.Provider).IsEqualTo("local");
    }

    [Test]
    public async Task GetAuthorizationProviderConfigurationInternal_WithSetupSecret_ShouldReturnConfiguration()
    {
        using var factory = CreateFactoryWithSetupSecret();
        using var client = factory.CreateClient();

        var userId = Guid.NewGuid();
        await EnsureUserExistsAsync(factory, userId);
        await EnsureInstanceAdminRoleAsync(factory, userId);

        using var saveRequest = CreateInstanceAdminRequest(
            HttpMethod.Put,
            $"{BaseUrl}/authz-provider-configuration",
            userId,
            CreateLocalAuthorizationProviderConfiguration(),
            includeSetupSecret: true);
        var saveResponse = await client.SendAsync(saveRequest);
        await Assert.That(saveResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        using var internalWithoutSecretRequest = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/authz-provider-configuration/internal");
        var internalWithoutSecretResponse = await client.SendAsync(internalWithoutSecretRequest);
        await Assert.That(internalWithoutSecretResponse.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);

        using var internalWithSecretRequest = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/authz-provider-configuration/internal");
        internalWithSecretRequest.Headers.Add("X-Setup-Secret", SetupSecret);
        var internalWithSecretResponse = await client.SendAsync(internalWithSecretRequest);
        await Assert.That(internalWithSecretResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var internalConfig = await internalWithSecretResponse.Content.ReadFromJsonAsync<AuthorizationProviderConfigurationDto>();
        await Assert.That(internalConfig).IsNotNull();
        await Assert.That(internalConfig!.Provider).IsEqualTo("local");
        await Assert.That(internalConfig.CerbosGrpcEndpoint).IsEqualTo(string.Empty);
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

    private static AuthenticatedWebApplicationFactory CreateFactoryWithSetupSecret(
        IReadOnlyDictionary<string, string?>? configurationOverrides = null)
    {
        Environment.SetEnvironmentVariable("SETUP_SECRET", SetupSecret);
        return configurationOverrides is null
            ? new AuthenticatedWebApplicationFactory()
            : new ConfigurableAuthenticatedWebApplicationFactory(configurationOverrides);
    }

    private static AuthenticatedWebApplicationFactory CreateFactoryWithSetupSecretWithoutClaimsTransformation()
    {
        Environment.SetEnvironmentVariable("SETUP_SECRET", SetupSecret);
        return new PassthroughClaimsTransformationFactory();
    }

    private sealed class ConfigurableAuthenticatedWebApplicationFactory : AuthenticatedWebApplicationFactory
    {
        private readonly IReadOnlyDictionary<string, string?> _configurationOverrides;

        public ConfigurableAuthenticatedWebApplicationFactory(IReadOnlyDictionary<string, string?> configurationOverrides)
        {
            _configurationOverrides = configurationOverrides;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(_configurationOverrides);
            });
        }
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

    private static HttpRequestMessage CreateCustomAuthRequest(
        HttpMethod method,
        string url,
        object? body,
        bool includeSetupSecret,
        params TestAuthHandler.TestClaimDto[] claims)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add(TestAuthHandler.AuthHeaderName, EncodeClaims(claims));

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

    private static string EncodeClaims(params TestAuthHandler.TestClaimDto[] claims)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(claims)));
    }

    private static CompleteInstanceOnboardingRequest CreateValidOnboardingRequest()
    {
        return new CompleteInstanceOnboardingRequest
        {
            DeploymentMode = DeploymentMode.SingleTenant,
            SiteProfile = new SelfHostOnboardingProfileDto { SiteName = "Integration Test Instance" },
            InstanceName = "Integration Test Instance"
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

    private static AuthorizationProviderConfigurationDto CreateLocalAuthorizationProviderConfiguration()
    {
        return new AuthorizationProviderConfigurationDto
        {
            Provider = "local",
            CerbosGrpcEndpoint = string.Empty,
            CerbosDetectedFromEnvironment = false,
            CerbosEndpointVerified = false
        };
    }

    private sealed class AuthProviderConfiguredResponse
    {
        public bool Configured { get; set; }
    }

    private sealed class PassthroughClaimsTransformationFactory : AuthenticatedWebApplicationFactory
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IClaimsTransformation>();
                services.AddSingleton<IClaimsTransformation, PassthroughClaimsTransformation>();
            });
        }
    }

    private sealed class PassthroughClaimsTransformation : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            return Task.FromResult(principal);
        }
    }
}
