// ABOUTME: Integration tests for the BFF registration provider embed host security boundary.
// ABOUTME: Proves descriptor-derived iframe HTML never trusts browser-supplied provider URLs or titles.

using System.Text.RegularExpressions;
using Explore.Blazor.Client.Clients;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Explore.Blazor.IntegrationTests.Endpoints;

public sealed partial class BffRegistrationProviderEmbedEndpointTests : IAsyncDisposable
{
    private static readonly Guid TenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");
    private static readonly Guid EventId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000101");
    private static readonly Guid WorkflowId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000201");
    private static readonly Guid RequirementId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000301");
    private static readonly Guid ChannelId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000401");
    private static readonly Guid BindingId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000501");

    private readonly IEventApiClient _apiClient = Substitute.For<IEventApiClient>();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public BffRegistrationProviderEmbedEndpointTests()
    {
        _factory = new BlazorBffWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEventApiClient>();
                services.AddSingleton(_apiClient);
            });
        });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Test]
    public async Task EmbedHost_AnonymousRequest_ReturnsUnauthorized()
    {
        GivenDescriptor(Descriptor());
        using var response = await _client.GetAsync(Route());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await _apiClient.DidNotReceiveWithAnyArgs().GetRegistrationProviderLaunchDescriptorAsync(
            default, default, default, default, default, default,
            null, null, default);
    }

    [Test]
    public async Task EmbedHost_ValidDescriptor_UsesRouteCspAndSameOriginFramePolicy()
    {
        GivenDescriptor(Descriptor(url: "https://forms.example.test/embed/form-1?token=server-only", title: "Provider registration"));

        using var response = await SendAuthenticatedAsync(Route());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("Content-Security-Policy").Single().Should().Be(
            "default-src 'none'; frame-src https://forms.example.test; frame-ancestors 'self'; base-uri 'none'; form-action 'none'; object-src 'none'; script-src 'none'; style-src 'none'");
        response.Headers.GetValues("X-Frame-Options").Single().Should().Be("SAMEORIGIN");
        response.Headers.GetValues("Cache-Control").Single().Should().Contain("private").And.Contain("no-store");
        response.Headers.GetValues("Pragma").Should().Contain("no-cache");
        GetRawHeader(response, "Expires").Should().Be("0");
        (await response.Content.ReadAsStringAsync()).Should().Contain("<iframe").And.Contain("sandbox=\"allow-forms allow-same-origin allow-scripts\"");
    }

    [Test]
    public async Task EmbedHost_DefaultHttpsPort_UsesCanonicalCspOrigin()
    {
        GivenDescriptor(Descriptor(url: "https://forms.example.test:443/embed/form-1"));

        using var response = await SendAuthenticatedAsync(Route());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("Content-Security-Policy").Single().Should().Contain("frame-src https://forms.example.test; ");
    }

    [Test]
    public async Task EmbedHost_MaliciousDescriptorValues_EncodesHtmlAndNeverAcceptsClientUrl()
    {
        GivenDescriptor(Descriptor(
            url: "https://forms.example.test/embed?next=\"&label=<script>alert(1)</script>",
            title: "Bad <img src=x onerror=alert(1)> title"));

        using var response = await SendAuthenticatedAsync(Route());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Bad &lt;img src=x onerror=alert(1)&gt; title");
        html.Should().NotContain("<script>");
        html.Should().NotContain("onload=");
        DangerousAttributeBreakout().IsMatch(html).Should().BeFalse();

        using var rejected = await SendAuthenticatedAsync(Route() + "?url=https://attacker.example/embed");
        rejected.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task EmbedHost_UnavailableNonEmbedOrTamperedLineage_ReturnsNotFound()
    {
        GivenDescriptor(Descriptor(available: false));
        using var unavailable = await SendAuthenticatedAsync(Route());
        unavailable.StatusCode.Should().Be(HttpStatusCode.NotFound);

        GivenDescriptor(Descriptor(mode: "redirect"));
        using var nonEmbed = await SendAuthenticatedAsync(Route());
        nonEmbed.StatusCode.Should().Be(HttpStatusCode.NotFound);

        GivenDescriptor(Descriptor(bindingId: Guid.Parse("018e4e5c-7f00-7000-8000-000000000502")));
        using var tampered = await SendAuthenticatedAsync(Route());
        tampered.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task EmbedHost_CrossTenantOrHttpDescriptor_ReturnsNotFound()
    {
        GivenDescriptor(Descriptor(tenantId: Guid.Parse("018e4e5c-7f00-7000-8000-000000000002")));
        using var crossTenant = await SendAuthenticatedAsync(Route());
        crossTenant.StatusCode.Should().Be(HttpStatusCode.NotFound);

        GivenDescriptor(Descriptor(url: "http://forms.example.test/embed"));
        using var httpDescriptor = await SendAuthenticatedAsync(Route());
        httpDescriptor.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task EmbedHost_UserInfoOrBlockedLiteralHost_ReturnsNotFound()
    {
        GivenDescriptor(Descriptor(url: "https://user:pass@forms.example.test/embed"));
        using var userInfo = await SendAuthenticatedAsync(Route());
        userInfo.StatusCode.Should().Be(HttpStatusCode.NotFound);

        GivenDescriptor(Descriptor(url: "https://127.0.0.1/embed"));
        using var loopback = await SendAuthenticatedAsync(Route());
        loopback.StatusCode.Should().Be(HttpStatusCode.NotFound);

        GivenDescriptor(Descriptor(url: "https://[fc00::1]/embed"));
        using var uniqueLocal = await SendAuthenticatedAsync(Route());
        uniqueLocal.StatusCode.Should().Be(HttpStatusCode.NotFound);

        GivenDescriptor(Descriptor(url: "https://[::ffff:127.0.0.1]/embed"));
        using var mappedLoopback = await SendAuthenticatedAsync(Route());
        mappedLoopback.StatusCode.Should().Be(HttpStatusCode.NotFound);

        GivenDescriptor(Descriptor(url: "https://[::ffff:192.168.1.1]/embed"));
        using var mappedPrivate = await SendAuthenticatedAsync(Route());
        mappedPrivate.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task NormalBffResponse_RetainsGlobalXFrameOptionsDeny()
    {
        using var response = await _client.GetAsync("/bff/theme");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("X-Frame-Options").Single().Should().Be("DENY");
    }

    [Test]
    public async Task EmbedHost_ValidRequest_CallsOnlyGeneratedFirstPartyApiClient()
    {
        GivenDescriptor(Descriptor());

        using var response = await SendAuthenticatedAsync(Route());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await _apiClient.Received(1).GetRegistrationProviderLaunchDescriptorAsync(
            TenantId,
            EventId,
            WorkflowId,
            RequirementId,
            ChannelId,
            BindingId,
            null,
            null,
            Arg.Any<CancellationToken>());
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<HttpResponseMessage> SendAuthenticatedAsync(string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add(TestAuthHandler.AuthHeaderName, TestAuthHandler.CreateAuthHeaderValue(Guid.NewGuid()));
        return await _client.SendAsync(request);
    }

    private void GivenDescriptor(HalResourceOfRegistrationProviderLaunchDescriptorDto descriptor)
    {
        _apiClient.GetRegistrationProviderLaunchDescriptorAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(descriptor));
    }

    private static string GetRawHeader(HttpResponseMessage response, string name)
    {
        if (response.Headers.NonValidated.TryGetValues(name, out var values) ||
            response.Content.Headers.NonValidated.TryGetValues(name, out values))
        {
            return values.Single();
        }

        return string.Empty;
    }

    private static HalResourceOfRegistrationProviderLaunchDescriptorDto Descriptor(
        Guid? tenantId = null,
        Guid? eventId = null,
        Guid? workflowId = null,
        Guid? requirementId = null,
        Guid? channelId = null,
        Guid? bindingId = null,
        string mode = "embed",
        bool available = true,
        string url = "https://forms.example.test/embed/form-1",
        string title = "Provider registration") => new()
        {
            AdditionalProperties =
            {
                ["tenantId"] = tenantId ?? TenantId,
                ["eventId"] = eventId ?? EventId,
                ["workflowId"] = workflowId ?? WorkflowId,
                ["requirementId"] = requirementId ?? RequirementId,
                ["channelId"] = channelId ?? ChannelId,
                ["bindingId"] = bindingId ?? BindingId,
                ["mode"] = mode,
                ["available"] = available,
                ["url"] = url,
                ["title"] = title
            }
        };

    private static string Route() =>
        $"/bff/registration-provider-embed/tenants/{TenantId}/events/{EventId}/workflows/{WorkflowId}/requirements/{RequirementId}/channels/{ChannelId}/bindings/{BindingId}";

    [GeneratedRegex("src=\\\"[^\\\"]*\\\"[^>]*<", RegexOptions.CultureInvariant)]
    private static partial Regex DangerousAttributeBreakout();
}
