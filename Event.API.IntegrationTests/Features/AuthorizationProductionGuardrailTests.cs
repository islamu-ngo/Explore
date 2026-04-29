// ABOUTME: Enterprise-grade production guardrail tests proving that test-only authorization
// ABOUTME: infrastructure (StubAuthorizationProvider, AuthorizationProviderOverride) cannot
// ABOUTME: accidentally run in production environments.

using System.Net.Security;
using System.Net.Sockets;
using Event.Api.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Infrastructure;
using Explore.Domain.Constants;
using Explore.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

/// <summary>
/// Production safety rail tests. These verify that test-only authorization shortcuts
/// (StubAuthorizationProvider, allow-all bypasses) cannot be accidentally used in production.
///
/// These are architecture/governance tests, not runtime tests. They prove that the DI
/// container wiring is safe: the production RuntimeAuthorizationProvider is always used
/// outside the Testing environment, and no allow-all stub can be injected accidentally.
/// </summary>
[Category(TestCategories.Fast)]
public class AuthorizationProductionGuardrailTests
{
    [Test]
    public async Task AllowAllStub_NotRegisteredInNonTestingEnvironment()
    {
        using var factory = new ProductionLikeWebApplicationFactory();
        using var scope = factory.Services.CreateScope();

        var provider = scope.ServiceProvider.GetService<IAuthorizationProvider>();

        provider.Should().NotBeNull("IAuthorizationProvider must always be registered");
        provider.Should().NotBeOfType<StubAuthorizationProvider>(
            "StubAuthorizationProvider must never be registered in a non-Testing environment");
        provider!.GetType().Name.Should().NotBe("StubAuthorizationProvider");
    }

    [Test]
    public async Task MissingCerbosEndpoint_DoesNotDefaultToAllowAll()
    {
        using var factory = new ProductionLikeWebApplicationFactory();
        using var scope = factory.Services.CreateScope();

        var provider = scope.ServiceProvider.GetRequiredService<IAuthorizationProvider>();

        var result = await provider.IsAllowedAsync("event", "test-resource", "create");

        result.Should().BeFalse(
            "with no Cerbos endpoint and authorization.provider unset, " +
            "the provider must default to denying, not allowing");
    }

    [Test]
    public async Task MissingKeycloakAuthority_DoesNotAllowAnonymousSuccess()
    {
        using var factory = new NoAuthConfigWebApplicationFactory();
        var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/events");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized,
            "without Keycloak authority configured, no requests should succeed");
    }

    [Test]
    public async Task SecurityWebApplicationFactory_AlwaysUsesTestingEnvironment()
    {
        var factory = new SecurityWebApplicationFactory(
            "http://localhost:1",
            "http://localhost:1/.well-known/openid-configuration",
            "http://localhost:19999");

        try
        {
            factory.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>()
                .EnvironmentName.Should().Be("Testing",
                    "SecurityWebApplicationFactory must always set the Testing environment " +
                    "to prevent test infrastructure from leaking into production behavior");
        }
        finally
        {
            await factory.DisposeAsync();
        }
    }

    /// <summary>
    /// Factory that mimics production as closely as possible (no Testing env overrides).
    /// Uses Development to avoid HTTPS redirect issues in test.
    /// </summary>
    private sealed class ProductionLikeWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                var testConfig = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test_guardrails;Username=postgres;Password=postgres",
                    ["S3Settings:Region"] = "us-east-1",
                    ["S3Settings:BucketName"] = "test-bucket",
                    ["S3Settings:AccessKeyId"] = "test-key",
                    ["S3Settings:SecretAccessKey"] = "test-secret",
                    ["S3Settings:Endpoint"] = "https://s3.example.com",
                    ["Deployment:Mode"] = "SingleTenant",
                    ["Deployment:DefaultTenantId"] = PlatformDefaults.DefaultTenantId.ToString(),
                    ["Cerbos:GrpcEndpoint"] = "http://localhost:19999",
                    ["Cerbos:PlaintextMode"] = "true",
                };

                config.AddInMemoryCollection(testConfig);
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ExploreDbContext>>();

                services.AddDbContext<ExploreDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"GuardrailDb_{Guid.NewGuid():N}");
                    options.ConfigureWarnings(x =>
                        x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                });

                services.RemoveAll<IDistributedCache>();
                services.AddDistributedMemoryCache();
            });

            builder.ConfigureTestServices(services =>
            {
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.BackchannelHttpHandler = new SocketsHttpHandler
                    {
                        SslOptions = new SslClientAuthenticationOptions
                        {
                            RemoteCertificateValidationCallback = (_, _, _, _) => true
                        }
                    };
                });
            });
        }
    }

    /// <summary>
    /// Factory with zero authentication/authorization configuration.
    /// No Keycloak, no Cerbos, no test auth overrides.
    /// </summary>
    private sealed class NoAuthConfigWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                var testConfig = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test_no_auth;Username=postgres;Password=postgres",
                    ["S3Settings:Region"] = "us-east-1",
                    ["S3Settings:BucketName"] = "test-bucket",
                    ["S3Settings:AccessKeyId"] = "test-key",
                    ["S3Settings:SecretAccessKey"] = "test-secret",
                    ["S3Settings:Endpoint"] = "https://s3.example.com",
                    ["Deployment:Mode"] = "SingleTenant",
                    ["Deployment:DefaultTenantId"] = PlatformDefaults.DefaultTenantId.ToString(),
                    ["Cerbos:GrpcEndpoint"] = "http://localhost:19999",
                    ["Cerbos:PlaintextMode"] = "true",
                };

                config.AddInMemoryCollection(testConfig);
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ExploreDbContext>>();

                services.AddDbContext<ExploreDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"NoAuthDb_{Guid.NewGuid():N}");
                    options.ConfigureWarnings(x =>
                        x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                });

                services.RemoveAll<IDistributedCache>();
                services.AddDistributedMemoryCache();
            });

            builder.ConfigureTestServices(services =>
            {
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.BackchannelHttpHandler = new SocketsHttpHandler
                    {
                        SslOptions = new SslClientAuthenticationOptions
                        {
                            RemoteCertificateValidationCallback = (_, _, _, _) => true
                        }
                    };
                });
            });
        }
    }
}
