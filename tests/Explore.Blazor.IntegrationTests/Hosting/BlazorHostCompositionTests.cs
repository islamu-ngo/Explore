// ABOUTME: Characterizes the reusable Explore.Blazor host composition and its transport profiles.
// ABOUTME: Proves the real BFF HTTP surface and Split-versus-Combined registration boundary.

using System.Net;
using Explore.Blazor.Extensions;
using Explore.Blazor.Hosting;
using Explore.Blazor.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Configuration;

namespace Explore.Blazor.IntegrationTests.Hosting;

public sealed class BlazorHostCompositionTests
{
    [Test]
    public async Task SplitHost_ServesExistingCssStaticAsset()
    {
        await using var factory = new BlazorBffWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        using var response = await client.GetAsync("/css/layers.css");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("text/css");
    }

    [Test]
    public async Task HostCompositionTypes_ArePublicAndProfilesAreExhaustive()
    {
        await Assert.That(typeof(BlazorHostProfile).IsPublic).IsTrue();
        await Assert.That(typeof(BlazorHostServiceCollectionExtensions).IsPublic).IsTrue();
        await Assert.That(typeof(BlazorHostApplicationExtensions).IsPublic).IsTrue();
        await Assert.That(Enum.GetValues<BlazorHostProfile>()
            .SequenceEqual([BlazorHostProfile.Split, BlazorHostProfile.Combined])).IsTrue();
    }

    [Test]
    public async Task AddBlazorHostServices_Split_RegistersProxyAndRemoteApiReadiness()
    {
        var builder = CreateBuilder();

        builder.AddBlazorHostServices(BlazorHostProfile.Split, new GracefulShutdownState());

        await Assert.That(builder.Services).Contains(descriptor =>
            descriptor.ServiceType == typeof(IProxyConfigProvider));
        await using var provider = builder.Services.BuildServiceProvider();
        var healthChecks = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value.Registrations;
        await Assert.That(healthChecks).Contains(registration => registration.Name == "self");
        await Assert.That(healthChecks).Contains(registration => registration.Name == "shutdown");
        await Assert.That(healthChecks).Contains(registration => registration.Name == "distributed-cache");
        await Assert.That(healthChecks).Contains(registration => registration.Name == "oidc-discovery");
        await Assert.That(healthChecks).Contains(registration => registration.Name == "explore-api");
        await Assert.That(healthChecks).Contains(registration => registration.Name == "data-protection-keys");
        await Assert.That(healthChecks).Contains(registration => registration.Name == "atproto-authentication");
    }

    [Test]
    public async Task AddBlazorHostServices_Combined_ExcludesProxyAndRemoteApiReadiness()
    {
        var builder = CreateBuilder();

        builder.AddBlazorHostServices(BlazorHostProfile.Combined, new GracefulShutdownState());

        await Assert.That(builder.Services).DoesNotContain(descriptor =>
            descriptor.ServiceType == typeof(IProxyConfigProvider));
        await using var provider = builder.Services.BuildServiceProvider();
        var healthChecks = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value.Registrations;
        await Assert.That(healthChecks).DoesNotContain(registration => registration.Name == "self");
        await Assert.That(healthChecks).DoesNotContain(registration => registration.Name == "shutdown");
        await Assert.That(healthChecks).DoesNotContain(registration => registration.Name == "distributed-cache");
        await Assert.That(healthChecks).DoesNotContain(registration => registration.Name == "oidc-discovery");
        await Assert.That(healthChecks).DoesNotContain(registration => registration.Name == "explore-api");
        await Assert.That(healthChecks).Contains(registration => registration.Name == "data-protection-keys");
        await Assert.That(healthChecks).Contains(registration => registration.Name == "atproto-authentication");
    }

    [Test]
    public async Task AddBlazorHostServices_ConflictingProfiles_Throws()
    {
        var builder = CreateBuilder();
        builder.AddBlazorHostServices(BlazorHostProfile.Split, new GracefulShutdownState());

        var act = () => builder.AddBlazorHostServices(
            BlazorHostProfile.Combined,
            new GracefulShutdownState());

        await Assert.That(act).Throws<InvalidOperationException>();
    }

    private static WebApplicationBuilder CreateBuilder()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(Program).Assembly.GetName().Name,
            EnvironmentName = "Testing"
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:cache"] = "localhost:6379,abortConnect=false,connectTimeout=100",
            ["Keycloak:Authority"] = "https://auth.example.com",
            ["Keycloak:Realm"] = "explore",
            ["Deployment:Mode"] = "SingleTenant",
            ["Deployment:DefaultTenantId"] = "018e4e5c-7f00-7000-8000-000000000001"
        });
        return builder;
    }
}
