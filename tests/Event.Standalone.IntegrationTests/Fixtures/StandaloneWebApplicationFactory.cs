// ABOUTME: Hosts the real standalone entry assembly with deterministic in-memory test dependencies.
// ABOUTME: Preserves the combined middleware and endpoint graph while removing external startup I/O.

using System.Collections.Concurrent;
using System.Net;
using Event.Web.BffHosting.Abstractions;
using Explore.Application.Contracts.Services;
using Explore.Application.Contracts.Infrastructure;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Services;
using Explore.Domain.Constants;
using Explore.Persistence;
using Event.Standalone.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Event.Standalone.IntegrationTests.Fixtures;

public sealed class StandaloneWebApplicationFactory : WebApplicationFactory<StandaloneHostMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseStaticWebAssets();
        builder.UseSetting("HttpsRedirection:Enabled", "false");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "PostgreSql",
                ["ExploreApi:BaseUrl"] = "http://127.0.0.1:7039/",
                ["Keycloak:Authority"] = "https://auth.example.com",
                ["Keycloak:Realm"] = "explore",
                ["Keycloak:Audience"] = "islamu-event-api",
                ["Keycloak:RequireHttpsMetadata"] = "false",
                ["Deployment:Mode"] = "SingleTenant",
                ["Deployment:DefaultTenantId"] = PlatformDefaults.DefaultTenantId.ToString(),
                ["Mcp:Enabled"] = "false",
                ["HttpsRedirection:Enabled"] = "false",
                ["Testing:SkipJwtAuthorityWarmup"] = "true",
                ["ForwardedHeadersTrust:TrustLoopbackProxy"] = "true",
                ["Bff:AdminHosts:0"] = "admin.proxy.test",
                ["Bff:AdminHostAllowedIpRanges:0"] = "203.0.113.0/24"
            }));

        builder.ConfigureServices(services =>
        {
            AddInMemoryExploreDbContext(services);
            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();
            services.RemoveAll<ISetupSecretProvider>();
            services.AddSingleton<ISetupSecretProvider, TestSetupSecretProvider>();
            services.RemoveAll<IDynamicAuthSchemeManager>();
            services.AddSingleton<DynamicAuthInitializationProbe>();
            services.AddSingleton<IDynamicAuthSchemeManager>(services =>
                services.GetRequiredService<DynamicAuthInitializationProbe>());
            services.RemoveAll<IEventBffTenantHintProvider>();
            services.AddSingleton<ForwardedRequestProbe>();
            services.AddSingleton<IEventBffTenantHintProvider>(serviceProvider =>
                serviceProvider.GetRequiredService<ForwardedRequestProbe>());

            for (var index = services.Count - 1; index >= 0; index--)
            {
                var implementationName = services[index].ImplementationType?.FullName;
                if (implementationName is "OpenFeature.Hosting.HostedFeatureLifecycleService"
                    or "Explore.API.Authentication.JwtAuthorityWarmupHostedService")
                {
                    services.RemoveAt(index);
                }
            }
        });
    }

    private static void AddInMemoryExploreDbContext(IServiceCollection services)
    {
        services.AddDbContextFactory<ExploreDbContext>(options =>
        {
            options.UseInMemoryDatabase("StandaloneGraphTests");
            options.ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning));
        });
        services.AddScoped(serviceProvider =>
        {
            var factory = serviceProvider.GetRequiredService<IDbContextFactory<ExploreDbContext>>();
            var context = factory.CreateDbContext();
            context.ClearTenantFilterBypass();
            context.TenantContext = serviceProvider.GetService<ITenantContext>();
            context.CurrentUserService = serviceProvider.GetService<ICurrentUserService>();
            return context;
        });
    }

    private sealed class TestSetupSecretProvider : ISetupSecretProvider
    {
        public bool IsSetupModeActive => false;
        public bool IsSetupSecretRequired => true;
        public bool IsFromEnvironmentVariable => false;
        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public bool ValidateSecret(string? secret) => false;
        public void Lock() { }
    }

}

public sealed class DynamicAuthInitializationProbe(IServiceScopeFactory scopeFactory) : IDynamicAuthSchemeManager
{
    private int _initializationCount;

    public int InitializationCount => Volatile.Read(ref _initializationCount);

    public async Task InitializeAsync()
    {
        Interlocked.Increment(ref _initializationCount);
        await Task.Delay(50);
        await using var scope = scopeFactory.CreateAsyncScope();
        var apiClient = scope.ServiceProvider.GetRequiredService<IEventApiClient>();
        _ = await apiClient.GetEventTypesAsync(cancellationToken: CancellationToken.None);
    }

    public Task RefreshSchemesAsync(string? setupSecret = null) => Task.CompletedTask;

    public Task<IReadOnlyList<string>> GetRegisteredProviderSchemesAsync() =>
        Task.FromResult<IReadOnlyList<string>>([]);
}

public sealed class ForwardedRequestProbe : IEventBffTenantHintProvider
{
    private readonly ConcurrentQueue<ForwardedRequestObservation> _observations = [];

    public IReadOnlyCollection<ForwardedRequestObservation> Observations => _observations.ToArray();

    public string? ResolveTenantSlug(HttpContext httpContext)
    {
        _observations.Enqueue(new ForwardedRequestObservation(
            httpContext.Request.Scheme,
            httpContext.Request.Host.Value ?? string.Empty,
            httpContext.Connection.RemoteIpAddress));
        return null;
    }
}

public sealed record ForwardedRequestObservation(
    string Scheme,
    string Host,
    IPAddress? RemoteIpAddress);
