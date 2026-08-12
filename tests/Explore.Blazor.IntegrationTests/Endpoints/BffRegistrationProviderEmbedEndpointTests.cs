// ABOUTME: Integration tests for the BFF registration provider embed host security boundary.
// ABOUTME: Proves descriptor-derived iframe HTML never trusts browser-supplied provider URLs or titles.

using System.Text.RegularExpressions;
using System.Text.Json;
using System.Net.Http.Json;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Components.Registration.ProviderLaunch;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Explore.Blazor.IntegrationTests.Endpoints;

public sealed partial class BffRegistrationProviderEmbedEndpointTests : IAsyncDisposable
{
    private static readonly Guid EventId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000101");
    private static readonly Guid OrderId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000201");
    private static readonly Guid RequirementId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000301");
    private static readonly Guid ChannelId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000401");
    private static readonly Guid BindingId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000501");
    private static readonly Guid FormId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000601");
    private static readonly Guid FormVersionId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000701");

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
    public async Task Launch_WithoutAntiforgery_ReturnsBadRequest()
    {
        GivenDescriptor(Descriptor());
        using var response = await _client.PostAsJsonAsync(Route(), LaunchRequest());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await _apiClient.DidNotReceiveWithAnyArgs().LaunchAuthenticatedRegistrationProviderAttemptAsync(
            default, default, default!, null, null, default);
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
    public async Task EmbedHost_TamperedFormOrHttpDescriptor_ReturnsNotFound()
    {
        GivenDescriptor(Descriptor(formId: Guid.Parse("018e4e5c-7f00-7000-8000-000000000602")));
        using var tamperedForm = await SendAuthenticatedAsync(Route());
        tamperedForm.StatusCode.Should().Be(HttpStatusCode.NotFound);

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
        await _apiClient.Received(1).LaunchAuthenticatedRegistrationProviderAttemptAsync(
            EventId,
            OrderId,
            Arg.Is<LaunchRegistrationProviderAttemptRequest>(request =>
                request.RequirementId == RequirementId && request.ChannelId == ChannelId &&
                request.BindingId == BindingId && request.FormId == FormId && request.FormVersionId == FormVersionId),
            null,
            null,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GuestLaunch_ForwardsCapabilityWithoutExposingItInOpaqueEmbedUrl()
    {
        const string capability = "guest-secret-capability";
        _apiClient.LaunchGuestRegistrationProviderAttemptAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<LaunchRegistrationProviderAttemptRequest>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Descriptor()));
        string token = await IssueAntiforgeryCookieAsync(null);
        using var request = new HttpRequestMessage(HttpMethod.Post, Route())
        {
            Content = JsonContent.Create(LaunchRequest() with { GuestCapability = capability })
        };
        request.Headers.Add("X-CSRF-TOKEN", token);

        using HttpResponseMessage response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        RegistrationProviderBffTicket? launch = await response.Content.ReadFromJsonAsync<RegistrationProviderBffTicket>();
        launch!.EmbedUrl.Should().StartWith("/bff/registration-provider-embed/launches/").And.NotContain(capability);
        await _apiClient.Received(1).LaunchGuestRegistrationProviderAttemptAsync(
            EventId, OrderId, Arg.Any<LaunchRegistrationProviderAttemptRequest>(), capability,
            null, null, Arg.Any<CancellationToken>());
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<HttpResponseMessage> SendAuthenticatedAsync(string path)
    {
        string authentication = TestAuthHandler.CreateAuthHeaderValue(Guid.NewGuid());
        string token = await IssueAntiforgeryCookieAsync(authentication);
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(LaunchRequest())
        };
        request.Headers.Add(TestAuthHandler.AuthHeaderName, authentication);
        request.Headers.Add("X-CSRF-TOKEN", token);
        HttpResponseMessage launch = await _client.SendAsync(request);
        if (!launch.IsSuccessStatusCode)
        {
            return launch;
        }

        RegistrationProviderBffTicket? result = await launch.Content.ReadFromJsonAsync<RegistrationProviderBffTicket>();
        launch.Dispose();
        return await _client.GetAsync(result!.EmbedUrl);
    }

    private void GivenDescriptor(HalResourceOfNativeRegistrationProviderLaunchDescriptorDto descriptor)
    {
        _apiClient.LaunchAuthenticatedRegistrationProviderAttemptAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<LaunchRegistrationProviderAttemptRequest>(),
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

    private static HalResourceOfNativeRegistrationProviderLaunchDescriptorDto Descriptor(
        Guid? requirementId = null,
        Guid? channelId = null,
        Guid? bindingId = null,
        Guid? formId = null,
        Guid? formVersionId = null,
        string mode = "embed",
        bool available = true,
        string url = "https://forms.example.test/embed/form-1",
        string title = "Provider registration") => new()
        {
            AdditionalProperties =
            {
                ["requirementId"] = requirementId ?? RequirementId,
                ["channelId"] = channelId ?? ChannelId,
                ["bindingId"] = bindingId ?? BindingId,
                ["formId"] = formId ?? FormId,
                ["formVersionId"] = formVersionId ?? FormVersionId,
                ["mode"] = mode,
                ["available"] = available,
                ["url"] = url,
                ["title"] = title
            }
        };

    private static RegistrationProviderBffLaunch LaunchRequest() => new(
        EventId, OrderId, RequirementId, ChannelId, BindingId, FormId, FormVersionId, null);

    private static string Route() => "/bff/registration-provider-embed/launches";

    private async Task<string> IssueAntiforgeryCookieAsync(string? authentication)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/status");
        if (authentication is not null)
        {
            request.Headers.Add(TestAuthHandler.AuthHeaderName, authentication);
        }
        using var response = await _client.SendAsync(request);
        response.Headers.TryGetValues("Set-Cookie", out var values).Should().BeTrue();
        string token = values!.Select(ReadXsrfToken).First(value => !string.IsNullOrWhiteSpace(value))!;
        return token;
    }

    private static string? ReadXsrfToken(string setCookie)
    {
        const string prefix = "XSRF-TOKEN=";
        if (!setCookie.StartsWith(prefix, StringComparison.Ordinal))
        {
            return null;
        }

        int end = setCookie.IndexOf(';', prefix.Length);
        return Uri.UnescapeDataString(end < 0 ? setCookie[prefix.Length..] : setCookie[prefix.Length..end]);
    }

    [GeneratedRegex("src=\\\"[^\\\"]*\\\"[^>]*<", RegexOptions.CultureInvariant)]
    private static partial Regex DangerousAttributeBreakout();
}
