// ABOUTME: Tests shared BFF control-plane authorization defaults without starting a full browser host.
// ABOUTME: Verifies the dedicated host profile uses the confidential client and instance-admin policy.

using Event.Web.BffHosting.Authentication;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Explore.Blazor.IntegrationTests.Services;

public sealed class EventBffControlPlaneAuthorizationTests
{
    [Test]
    public async Task ControlPlaneProfile_UsesDedicatedClientCookieAndInstanceAdminRequirement()
    {
        var options = EventBffKeycloakAuthenticationOptions.FromConfiguration(
            BuildConfiguration(),
            EventBffHostProfile.ControlPlane,
            new TestHostEnvironment());

        await Assert.That(options.ClientId).IsEqualTo("islamu-event-control-plane");
        await Assert.That(options.CookieName).IsEqualTo("__Host-islamu-event-control-plane");
        await Assert.That(options.RequireInstanceAdminClaim).IsTrue();
        await Assert.That(options.InstanceAdminClaimType).IsEqualTo("explore:admin:instance");
        await Assert.That(options.InstanceAdminClaimValue).IsEqualTo("true");
    }

    [Test]
    public async Task ControlPlaneAccessPolicy_DeniesRegularUsersAndAllowsInstanceAdmins()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEventBffKeycloakAuthentication(
            BuildConfiguration(),
            new TestHostEnvironment(),
            EventBffHostProfile.ControlPlane);
        using var provider = services.BuildServiceProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();
        var policy = await policyProvider.GetPolicyAsync(EventBffAuthorizationPolicies.ControlPlaneAccess);
        policy.Should().NotBeNull();

        var regularUser = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
            TestAuthHandler.SchemeName));
        var instanceAdmin = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim("explore:admin:instance", "true")
            ],
            TestAuthHandler.SchemeName));

        var regularResult = await authorization.AuthorizeAsync(regularUser, resource: null, policy!);
        var adminResult = await authorization.AuthorizeAsync(instanceAdmin, resource: null, policy!);

        await Assert.That(regularResult.Succeeded).IsFalse();
        await Assert.That(adminResult.Succeeded).IsTrue();
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Bff:Authentication:Authority"] = "https://auth.example.test/realms/islamu",
                ["Bff:Authentication:ClientSecret"] = "test-control-plane-secret"
            })
            .Build();
    }

    private sealed class TestHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = "Event.ControlPlane.Blazor.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

        public string WebRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
