// ABOUTME: Minimal-host integration tests for TickerQ dashboard route exposure.
// ABOUTME: Proves dashboard routes stay disabled by default and require host authentication when enabled.

using System.Net;
using System.Text.Encodings.Web;
using Event.Api.IntegrationTests.Fixtures;
using Explore.API.Configuration;
using Explore.API.Extensions;
using Explore.API.Scheduling;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using TUnit.Core;

namespace ApiIntegrationTests.Features;

[NotInParallel("RealRuntimeDb")]
public sealed class TickerQDashboardRouteTests
{
    [Test]
    public async Task DashboardDisabledDoesNotExposeSchedulerRoute()
    {
        await using var host = await CreateTickerQHostAsync(dashboardEnabled: false);
        var response = await host.Client.GetAsync("/admin/scheduler");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DashboardEnabledRequiresHostAuthentication()
    {
        await using var host = await CreateTickerQHostAsync(dashboardEnabled: true);
        var response = await host.Client.GetAsync("/admin/scheduler");

        await Assert.That(new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden }).Contains(response.StatusCode);
    }

    private static async Task<TickerQHost> CreateTickerQHostAsync(bool dashboardEnabled)
    {
        var container = new PostgreSqlBuilder("postgres:18-alpine")
            .WithDatabase("tickerq_dashboard_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
        await container.StartAsync();

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development"
        });
        builder.WebHost.UseTestServer();
        var databaseConfiguration = new Dictionary<string, string?>
        {
            [$"{TickerQSchedulerOptions.SectionName}:Enabled"] = "true",
            [$"{TickerQSchedulerOptions.SectionName}:Schema"] = "ticker",
            [$"{TickerQSchedulerOptions.SectionName}:DashboardEnabled"] = dashboardEnabled.ToString(),
            [$"{TickerQSchedulerOptions.SectionName}:DashboardAuthorizationPolicy"] =
                TickerQSchedulerOptions.InstanceAdminPolicyName,
            [$"{TickerQSchedulerOptions.SectionName}:DashboardPath"] = "/admin/scheduler",
            [$"{TickerQSchedulerOptions.SectionName}:MaxConcurrency"] = "1",
            [$"{TickerQSchedulerOptions.SectionName}:NodeIdentifier"] = "tickerq-dashboard-test-node"
        };
        TestDatabaseConfiguration.AddPostgreSql(databaseConfiguration, container.GetConnectionString());
        builder.Configuration.AddInMemoryCollection(databaseConfiguration);

        builder.Services.AddRouting();
        builder.Services
            .AddAuthentication(RejectingAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, RejectingAuthenticationHandler>(
                RejectingAuthenticationHandler.SchemeName,
                _ => { });
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(
                TickerQSchedulerOptions.InstanceAdminPolicyName,
                policy => policy.RequireAuthenticatedUser());
        });
        builder.Services.AddApiTickerQScheduler(builder.Configuration, builder.Environment, enabled: true);

        var app = builder.Build();
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApiTickerQDbContext>>();
            await using var db = await dbFactory.CreateDbContextAsync();
            await db.Database.MigrateAsync();
        }

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseApiTickerQScheduler();

        await app.StartAsync();

        return new TickerQHost(app, container, app.GetTestClient());
    }

    private sealed class TickerQHost(
        WebApplication app,
        PostgreSqlContainer container,
        HttpClient client) : IAsyncDisposable
    {
        public HttpClient Client { get; } = client;

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.DisposeAsync();
            await container.DisposeAsync();
        }
    }

    private sealed class RejectingAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "RejectingTest";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }
    }
}
