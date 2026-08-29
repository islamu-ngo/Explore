// ABOUTME: Defines prospective browser trust-boundary contracts for event add-on operations.
// ABOUTME: Pins generated-client forwarding, antiforgery, cookie authority, capability isolation, and no-store.

using System.Reflection;

namespace Explore.Blazor.IntegrationTests;

public sealed class EventAddOnBffTests
{
    private const string EndpointTypeName =
        "Explore.Blazor.Extensions.BffEventAddOnEndpoints";
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly string EndpointPath = Path.Combine(
        RepositoryRoot,
        "src",
        "Explore.Blazor",
        "Extensions",
        "BffEventAddOnEndpoints.cs");

    [Test]
    public async Task BffEndpointFamilyExistsAndMapsCatalogOrderAndLifecycleRoutes()
    {
        Type? endpoints = typeof(Program).Assembly.GetType(EndpointTypeName);
        await Assert.That(endpoints).IsNotNull();
        await Assert.That(File.Exists(EndpointPath)).IsTrue();
        if (endpoints is null || !File.Exists(EndpointPath))
        {
            return;
        }

        await Assert.That(endpoints.GetMethod(
                "MapEventAddOnBff",
                BindingFlags.Public | BindingFlags.Static))
            .IsNotNull();
        string source = await File.ReadAllTextAsync(EndpointPath);
        foreach (string route in new[]
                 {
                     "/bff/events/{eventId:guid}/add-ons",
                     "/bff/events/{eventId:guid}/add-ons/management",
                     "/bff/events/{eventId:guid}/registration-orders/{registrationOrderId:guid}/add-ons",
                     "/fulfillment",
                     "/refunds",
                 })
        {
            await Assert.That(source).Contains(route);
        }
    }

    [Test]
    public async Task StateChangingRoutesRequireAntiforgeryAndSameOriginCookieAuthority()
    {
        await Assert.That(File.Exists(EndpointPath)).IsTrue();
        if (!File.Exists(EndpointPath))
        {
            return;
        }

        string source = await File.ReadAllTextAsync(EndpointPath);
        await Assert.That(Count(
                source,
                ".ValidateAntiforgeryBeforeRateLimiting()"))
            .IsGreaterThanOrEqualTo(7);
        await Assert.That(source).DoesNotContain("AllowAnonymous");
        await Assert.That(source).DoesNotContain("access_token");
        await Assert.That(source).DoesNotContain("AccessToken");
        await Assert.That(source).DoesNotContain("Bearer ");
        await Assert.That(source).DoesNotContain("Headers.Authorization");
        await Assert.That(source).DoesNotContain("\"Authorization\"");
    }

    [Test]
    public async Task BffUsesGeneratedClientAndNeverBuildsDirectDownstreamHttpRequests()
    {
        await Assert.That(File.Exists(EndpointPath)).IsTrue();
        if (!File.Exists(EndpointPath))
        {
            return;
        }

        string source = await File.ReadAllTextAsync(EndpointPath);
        await Assert.That(source).Contains("IEventApiClient");
        await Assert.That(source).Contains("GetEventAddOnCatalogAsync");
        await Assert.That(source).Contains("GetRegistrationOrderAddOnsAsync");
        await Assert.That(source).Contains("ReserveRegistrationOrderAddOnsAsync");
        await Assert.That(source).Contains("FulfillRegistrationOrderAddOnAsync");
        await Assert.That(source).Contains("RefundRegistrationOrderAddOnAsync");
        await Assert.That(source).DoesNotContain("new HttpClient");
        await Assert.That(source).DoesNotContain("HttpRequestMessage");
    }

    [Test]
    public async Task CapabilityIsForwardedOnlyAsAnOpaqueHeaderAndNeverReturnedOrLogged()
    {
        await Assert.That(File.Exists(EndpointPath)).IsTrue();
        if (!File.Exists(EndpointPath))
        {
            return;
        }

        string source = await File.ReadAllTextAsync(EndpointPath);
        await Assert.That(source).Contains("X-Registration-Order-Capability");
        await Assert.That(source).DoesNotContain("LogInformation");
        await Assert.That(source).DoesNotContain("LogDebug");
        await Assert.That(source).DoesNotContain("RawToken");
        await Assert.That(source).DoesNotContain("Response.Headers.Append");
    }

    [Test]
    public async Task EveryResponseIsNoStoreAndErrorsRemainGeneric()
    {
        await Assert.That(File.Exists(EndpointPath)).IsTrue();
        if (!File.Exists(EndpointPath))
        {
            return;
        }

        string source = await File.ReadAllTextAsync(EndpointPath);
        await Assert.That(source).Contains("SetPrivateNoStore");
        await Assert.That(source).Contains("event_add_on_unavailable");
        await Assert.That(source).DoesNotContain("inventory capacity");
        await Assert.That(source).DoesNotContain("tenant mismatch");
        await Assert.That(source).DoesNotContain("admission");
    }

    private static int Count(string value, string fragment) =>
        value.Split(fragment, StringSplitOptions.None).Length - 1;

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Explore.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
