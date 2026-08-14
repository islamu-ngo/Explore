// ABOUTME: Authorization matrix for setup-secret authentication on canonical instance provider GET and PATCH routes.
// ABOUTME: Proves exact-route selection, fail-closed credentials, and unchanged bearer-admin authentication.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Event.Api.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

[Category(TestCategories.Security)]
[NotInParallel("SetupSecretAuthorizationMatrix")]
public class SetupSecretAuthorizationMatrixTests
{
    private const string SetupSecretHeaderName = "X-Setup-Secret";
    private const string ValidSetupSecret = "matrix-setup-secret";

    [Test]
    [Arguments("/api/instance/settings/auth-provider")]
    [Arguments("/api/instance/settings/authz-provider")]
    public async Task ExactProviderPatch_ValidSetupCredential_Authenticates(string path)
    {
        await using var factory = CreateFactory(new MatrixSetupSecretProvider());
        using var client = factory.CreateClient();
        using var request = SetupSecretPatch(path, ValidSetupSecret);

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest).Because("a valid setup secret on an exact canonical PATCH route should reach command validation");
    }

    [Test]
    [Arguments("/api/instance/settings/auth-provider")]
    [Arguments("/api/instance/settings/authz-provider")]
    public async Task ExactProviderGet_ValidSetupCredential_Authenticates(string path)
    {
        await using var factory = CreateFactory(new MatrixSetupSecretProvider());
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add(SetupSecretHeaderName, ValidSetupSecret);

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK).Because("a valid setup secret on an exact canonical GET route should reach the provider configuration response");
    }

    [Test]
    [Arguments("/api/instance/settings/auth-provider")]
    [Arguments("/api/instance/settings/authz-provider")]
    public async Task ExactProviderPatch_InvalidSetupCredential_DoesNotFallBackToBearer(string path)
    {
        await using var factory = CreateFactory(new MatrixSetupSecretProvider());
        using var client = factory.CreateClient();
        var userId = Guid.NewGuid();
        await SeedInstanceAdminAsync(factory, userId);
        using var request = BearerPatch(path, factory.CreateJwt(userId));
        request.Headers.Add(SetupSecretHeaderName, "invalid-setup-secret");

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized).Because("a present invalid setup secret must fail closed instead of falling back to bearer authentication");
    }

    [Test]
    [Arguments("/api/instance/settings/auth-provider")]
    [Arguments("/api/instance/settings/authz-provider")]
    public async Task ExactProviderPatch_MissingCredentialAndBearer_IsDenied(string path)
    {
        await using var factory = CreateFactory(new MatrixSetupSecretProvider());
        using var client = factory.CreateClient();
        using var request = SetupSecretPatch(path, secret: null);

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized).Because("the canonical PATCH routes must retain their authenticated write boundary");
    }

    [Test]
    public async Task ExactProviderPatch_InactiveSetupCredential_IsDenied()
    {
        await using var factory = CreateFactory(
            new MatrixSetupSecretProvider(isSetupModeActive: false));
        using var client = factory.CreateClient();
        using var request = SetupSecretPatch(
            "/api/instance/settings/auth-provider",
            ValidSetupSecret);

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Gone).Because("inactive setup mode should preserve Gone");
    }

    [Test]
    public async Task UnrelatedAuthenticatedRoute_SetupCredential_DoesNotAuthenticate()
    {
        await using var factory = CreateFactory(new MatrixSetupSecretProvider());
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/instance/settings/modules");
        request.Headers.Add(SetupSecretHeaderName, ValidSetupSecret);

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized).Because("setup-secret authentication must not apply outside the four canonical provider GET and PATCH routes");
    }

    [Test]
    [Arguments("/api/instance/settings/auth-provider")]
    [Arguments("/api/instance/settings/authz-provider")]
    public async Task ExactProviderPatch_BearerWithoutSetupHeader_PreservesAdminFlow(string path)
    {
        await using var factory = CreateFactory(new MatrixSetupSecretProvider());
        using var client = factory.CreateClient();
        var userId = Guid.NewGuid();
        await SeedInstanceAdminAsync(factory, userId);
        using var request = BearerPatch(path, factory.CreateJwt(userId));

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest).Because("ordinary instance-admin bearer requests should continue to reach command validation");
    }

    private static ExternalApiPhase0WebApplicationFactory CreateFactory(
        ISetupSecretProvider setupSecretProvider)
        => new()
        {
            DeploymentMode = DeploymentMode.SingleTenant,
            SetupSecretProviderOverride = setupSecretProvider
        };

    private static HttpRequestMessage SetupSecretPatch(string path, string? secret)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, path)
        {
            Content = JsonContent.Create(new { })
        };

        if (secret is not null)
        {
            request.Headers.Add(SetupSecretHeaderName, secret);
        }

        return request;
    }

    private static HttpRequestMessage BearerPatch(string path, string token)
    {
        var request = SetupSecretPatch(path, secret: null);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static async Task SeedInstanceAdminAsync(
        ExternalApiPhase0WebApplicationFactory factory,
        Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
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
        dbContext.InstanceBootstrapStates.Add(new InstanceBootstrapState
        {
            Id = Guid.NewGuid(),
            IsCompleted = true,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            CompletedByUserId = userId,
            SelectedDeploymentMode = DeploymentMode.SingleTenant.ToString()
        });
        await dbContext.SaveChangesAsync();
    }

    private sealed class MatrixSetupSecretProvider(bool isSetupModeActive = true) : ISetupSecretProvider
    {
        private bool _isLocked;

        public bool IsSetupModeActive => isSetupModeActive && !_isLocked;
        public bool IsSetupSecretRequired => true;
        public bool IsFromEnvironmentVariable => false;

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public bool ValidateSecret(string? secret)
            => IsSetupModeActive
               && string.Equals(secret, ValidSetupSecret, StringComparison.Ordinal);

        public void Lock() => _isLocked = true;
    }
}
