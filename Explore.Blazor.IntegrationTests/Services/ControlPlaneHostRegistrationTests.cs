// ABOUTME: Verifies the dedicated Control Plane Blazor host wires real server-authoritative services.
// ABOUTME: Prevents the shared RCL fail-closed fallback client from leaking into the dedicated host.

extern alias ControlPlaneBlazor;

using ControlPlaneBlazor::Event.ControlPlane.Blazor.Services;
using ControlPlaneProgram = ControlPlaneBlazor::Program;
using Event.ControlPlane.Client.Services;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Explore.Blazor.IntegrationTests.Services;

public sealed class ControlPlaneHostRegistrationTests
{
    [Test]
    public void DedicatedControlPlaneHost_OverridesRclFallbackServicesWithApiAdapter()
    {
        using var factory = new ControlPlaneBlazorWebApplicationFactory();
        using var scope = factory.Services.CreateScope();

        var services = scope.ServiceProvider;
        object overview = services.GetRequiredService<IControlPlaneOverviewService>();
        object tenants = services.GetRequiredService<IControlPlaneTenantService>();
        object domains = services.GetRequiredService<IControlPlaneDomainService>();
        object operations = services.GetRequiredService<IControlPlaneOperationsService>();

        overview.Should().BeOfType<ControlPlaneApiAdapter>();
        tenants.Should().BeSameAs(overview);
        domains.Should().BeSameAs(overview);
        operations.Should().BeSameAs(overview);
    }

    private sealed class ControlPlaneBlazorWebApplicationFactory : WebApplicationFactory<ControlPlaneProgram>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Bff:Authentication:Authority", "https://auth.example.com/realms/ISLAMU");
            builder.UseSetting("Bff:Authentication:ClientSecret", "test-control-plane-secret");
            builder.UseSetting("ExploreApi:BaseUrl", "https://api.example.test/");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Bff:Authentication:Authority"] = "https://auth.example.com/realms/ISLAMU",
                    ["Bff:Authentication:ClientSecret"] = "test-control-plane-secret",
                    ["ExploreApi:BaseUrl"] = "https://api.example.test/"
                });
            });

            builder.ConfigureTestServices(services =>
                services.AddDataProtection().UseEphemeralDataProtectionProvider());
        }
    }
}
