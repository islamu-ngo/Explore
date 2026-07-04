// ABOUTME: Proves BFF cookie tickets survive a host restart when Data Protection keys persist.
// ABOUTME: Uses the real ASP.NET Core cookie middleware with the repo DataProtectionKeyContext.

using System.Net;
using System.Security.Claims;
using Explore.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ExploreDataProtection = Explore.Persistence.Extensions.DataProtectionServiceCollectionExtensions;

namespace Explore.Blazor.IntegrationTests.Endpoints;

public sealed class BffDataProtectionCookieRestartTests
{
    private const string CookieName = ".AspNetCore.RestartProof";
    private const string DatabaseName = "bff-data-protection-restart-proof";
    private static readonly InMemoryDatabaseRoot DatabaseRoot = new();

    [Test]
    public async Task CookieTicketSurvivesFreshBffHostWhenKeyRingPersists()
    {
        string cookieHeader;

        await using (var firstHost = await StartCookieHostAsync())
        {
            using var firstClient = firstHost.GetTestClient();
            using var loginResponse = await firstClient.GetAsync("/login-test");

            await Assert.That(loginResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
            cookieHeader = GetCookieHeader(loginResponse);
        }

        await using (var secondHost = await StartCookieHostAsync())
        {
            using var secondClient = secondHost.GetTestClient();
            secondClient.DefaultRequestHeaders.Add("Cookie", cookieHeader);

            using var authResponse = await secondClient.GetAsync("/auth-required");
            var body = await authResponse.Content.ReadAsStringAsync();

            await Assert.That(authResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
            await Assert.That(body).Contains("restart-user");
        }
    }

    private static async Task<WebApplication> StartCookieHostAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing"
        });

        builder.WebHost.UseTestServer();
        builder.Services.AddDbContext<DataProtectionKeyContext>(options =>
            options.UseInMemoryDatabase(DatabaseName, DatabaseRoot));
        builder.Services
            .AddDataProtection()
            .SetApplicationName(ExploreDataProtection.DefaultApplicationName)
            .PersistKeysToDbContext<DataProtectionKeyContext>();
        builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.Name = CookieName;
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
            });
        builder.Services.AddAuthorization();

        var app = builder.Build();

        await using (var scope = app.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<DataProtectionKeyContext>().Database.EnsureCreatedAsync();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/login-test", async context =>
        {
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, "restart-user")],
                CookieAuthenticationDefaults.AuthenticationScheme);

            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));
        });

        app.MapGet("/auth-required", (ClaimsPrincipal user) =>
                Results.Ok(user.Identity?.Name ?? string.Empty))
            .RequireAuthorization();

        await app.StartAsync();
        return app;
    }

    private static string GetCookieHeader(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            throw new InvalidOperationException("Login response did not set the BFF auth cookie.");
        }

        var cookie = values.FirstOrDefault(value => value.StartsWith(CookieName, StringComparison.Ordinal));
        if (string.IsNullOrWhiteSpace(cookie))
        {
            throw new InvalidOperationException("Login response did not include the expected BFF auth cookie.");
        }

        return cookie.Split(';', 2)[0];
    }
}
