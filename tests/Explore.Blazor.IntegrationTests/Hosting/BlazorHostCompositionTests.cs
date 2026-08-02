// ABOUTME: Characterizes the reusable Explore.Blazor host composition and its transport profiles.
// ABOUTME: Proves the real BFF HTTP surface and Split-versus-Combined registration boundary.

using System.Net;
using Explore.Blazor.Extensions;
using Explore.Blazor.Hosting;
using Explore.Blazor.IntegrationTests.Fixtures;
using FluentAssertions;
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

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/css");
    }

    [Test]
    public void HostCompositionTypes_ArePublicAndProfilesAreExhaustive()
    {
        typeof(BlazorHostProfile).IsPublic.Should().BeTrue();
        typeof(BlazorHostServiceCollectionExtensions).IsPublic.Should().BeTrue();
        typeof(BlazorHostApplicationExtensions).IsPublic.Should().BeTrue();
        Enum.GetValues<BlazorHostProfile>().Should().Equal(
            BlazorHostProfile.Split,
            BlazorHostProfile.Combined);
    }

    [Test]
    public async Task AddBlazorHostServices_Split_RegistersProxyAndRemoteApiReadiness()
    {
        var builder = CreateBuilder();

        builder.AddBlazorHostServices(BlazorHostProfile.Split, new GracefulShutdownState());

        builder.Services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IProxyConfigProvider));
        await using var provider = builder.Services.BuildServiceProvider();
        var healthChecks = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value.Registrations;
        healthChecks.Should().Contain(registration => registration.Name == "self");
        healthChecks.Should().Contain(registration => registration.Name == "shutdown");
        healthChecks.Should().Contain(registration => registration.Name == "distributed-cache");
        healthChecks.Should().Contain(registration => registration.Name == "oidc-discovery");
        healthChecks.Should().Contain(registration => registration.Name == "explore-api");
        healthChecks.Should().Contain(registration => registration.Name == "data-protection-keys");
        healthChecks.Should().Contain(registration => registration.Name == "atproto-authentication");
    }

    [Test]
    public async Task AddBlazorHostServices_Combined_ExcludesProxyAndRemoteApiReadiness()
    {
        var builder = CreateBuilder();

        builder.AddBlazorHostServices(BlazorHostProfile.Combined, new GracefulShutdownState());

        builder.Services.Should().NotContain(descriptor =>
            descriptor.ServiceType == typeof(IProxyConfigProvider));
        await using var provider = builder.Services.BuildServiceProvider();
        var healthChecks = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value.Registrations;
        healthChecks.Should().NotContain(registration => registration.Name == "self");
        healthChecks.Should().NotContain(registration => registration.Name == "shutdown");
        healthChecks.Should().NotContain(registration => registration.Name == "distributed-cache");
        healthChecks.Should().NotContain(registration => registration.Name == "oidc-discovery");
        healthChecks.Should().NotContain(registration => registration.Name == "explore-api");
        healthChecks.Should().Contain(registration => registration.Name == "data-protection-keys");
        healthChecks.Should().Contain(registration => registration.Name == "atproto-authentication");
    }

    [Test]
    public void AddBlazorHostServices_ConflictingProfiles_Throws()
    {
        var builder = CreateBuilder();
        builder.AddBlazorHostServices(BlazorHostProfile.Split, new GracefulShutdownState());

        var act = () => builder.AddBlazorHostServices(
            BlazorHostProfile.Combined,
            new GracefulShutdownState());

        act.Should().Throw<InvalidOperationException>();
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
