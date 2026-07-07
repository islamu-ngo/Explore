// ABOUTME: Verifies tenant host resolvers skip statically configured admin hosts.
// ABOUTME: Keeps dedicated admin hostnames from being interpreted as tenant slugs or custom domains.

using Event.Web.BffHosting.Options;
using Event.Web.BffHosting.Security;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Blazor.Services.Resolvers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Explore.Blazor.IntegrationTests.Services;

public sealed class TenantHostResolverAdminHostTests
{
    [Test]
    public async Task SubdomainResolver_WithConfiguredAdminHost_DoesNotLookupTenantSlug()
    {
        var tenantSlugCache = Substitute.For<ITenantSlugCache>();
        var resolver = new SubdomainTenantResolver(tenantSlugCache, CreateClassifier("admin.example.org"));
        var httpContext = CreateHttpContext("admin.example.org");
        var configuration = new ResolverConfigurationDto
        {
            SubdomainEnabled = true,
            InstanceBaseDomain = "example.org"
        };

        var tenantId = await resolver.ResolveAsync(httpContext, configuration);

        await Assert.That(tenantId).IsNull();
        await tenantSlugCache.DidNotReceive().GetTenantIdByDomainAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CustomDomainResolver_WithConfiguredAdminHost_DoesNotLookupCustomDomain()
    {
        var tenantSlugCache = Substitute.For<ITenantSlugCache>();
        var resolver = new CustomDomainTenantResolver(tenantSlugCache, CreateClassifier("admin.example.org"));
        var httpContext = CreateHttpContext("admin.example.org");
        var configuration = new ResolverConfigurationDto
        {
            CustomDomainEnabled = true,
            AllowTenantCustomDomains = true
        };

        var tenantId = await resolver.ResolveAsync(httpContext, configuration);

        await Assert.That(tenantId).IsNull();
        await tenantSlugCache.DidNotReceive().GetTenantIdByDomainAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static DefaultHttpContext CreateHttpContext(string host)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString(host);
        return httpContext;
    }

    private static EventBffHostClassifier CreateClassifier(params string[] adminHosts)
    {
        return new EventBffHostClassifier(Options.Create(new EventBffHostingOptions
        {
            AdminHosts = adminHosts
        }));
    }
}
