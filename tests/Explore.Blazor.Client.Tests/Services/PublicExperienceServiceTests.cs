// ABOUTME: Unit tests for PublicExperienceService generated-client delegation and home route resolution.
// ABOUTME: Verifies public settings and shell fallback, caching, and generated enum route rules.

namespace Explore.Blazor.Client.Tests.Services;

public class PublicExperienceServiceTests
{
    private readonly IEventApiClient _apiClient;
    private readonly PublicExperienceService _service;

    public PublicExperienceServiceTests()
    {
        _apiClient = Substitute.For<IEventApiClient>();
        _service = new PublicExperienceService(
            _apiClient,
            Substitute.For<ILogger<PublicExperienceService>>());
    }

    [Test]
    public async Task GetSettingsAsync_ReturnsGeneratedSettings_WhenApiSucceeds()
    {
        var expected = new PublicExperienceSettingsDto
        {
            TenantId = Guid.NewGuid(),
            Mode = PublicExperienceMode.OrganizationCentric,
            PreferredHomePage = "LandingPage"
        };
        _apiClient.GetPublicExperienceSettingsAsync(
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _service.GetSettingsAsync();

        await Assert.That(result).IsSameReferenceAs(expected);
        await Assert.That(result!.Mode).IsEqualTo(PublicExperienceMode.OrganizationCentric);
        await Assert.That(result.PreferredHomePage).IsEqualTo("LandingPage");
    }

    [Test]
    public async Task GetSettingsAsync_ReturnsNull_WhenApiThrows()
    {
        _apiClient.GetPublicExperienceSettingsAsync(
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<PublicExperienceSettingsDto>>(_ => throw new HttpRequestException("factory failure"));

        var result = await _service.GetSettingsAsync();

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetShellAsync_ReturnsGeneratedShell_WhenApiSucceeds()
    {
        var expected = new PublicExperienceShellDto
        {
            Mode = PublicExperienceMode.OrganizationCentric,
            PrimaryOrganization = new PublicExperiencePrimaryOrganizationDto
            {
                State = PublicExperiencePrimaryOrganizationState.Available,
                DisplayName = "Community Center"
            },
            EventCatalog = new PublicExperienceEventCatalogDto
            {
                Label = "Programs",
                Url = "/events?ActorId=11111111-1111-1111-1111-111111111111"
            }
        };
        _apiClient.GetPublicExperienceShellAsync(
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _service.GetShellAsync();

        await Assert.That(result).IsSameReferenceAs(expected);
        await Assert.That(result!.PrimaryOrganization!.State)
            .IsEqualTo(PublicExperiencePrimaryOrganizationState.Available);
        await Assert.That(result.EventCatalog!.Label).IsEqualTo("Programs");
    }

    [Test]
    public async Task GetShellAsync_ReturnsNull_WhenApiRejectsRequest()
    {
        _apiClient.GetPublicExperienceShellAsync(
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<PublicExperienceShellDto>>(_ => throw new ApiException(
                "Server error",
                500,
                string.Empty,
                new Dictionary<string, IEnumerable<string>>(),
                null));

        var result = await _service.GetShellAsync();

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetCachedSettingsAsync_ReusesSuccessfulResponse()
    {
        var expected = new PublicExperienceSettingsDto { PreferredHomePage = "LandingPage" };
        _apiClient.GetPublicExperienceSettingsAsync(
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var first = await _service.GetCachedSettingsAsync();
        var second = await _service.GetCachedSettingsAsync();

        await Assert.That(first).IsSameReferenceAs(expected);
        await Assert.That(second).IsSameReferenceAs(expected);
        await _apiClient.Received(1).GetPublicExperienceSettingsAsync(
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments("LandingPage", "/home")]
    [Arguments("landingpage", "/home")]
    [Arguments("EventList", "/events")]
    public async Task ResolveHomeRoute_UsesPreferredHomePage(string preferredHomePage, string expectedRoute)
    {
        var route = _service.ResolveHomeRoute(new PublicExperienceSettingsDto
        {
            PreferredHomePage = preferredHomePage
        });

        await Assert.That(route).IsEqualTo(expectedRoute);
    }

    [Test]
    public async Task ResolveHomeRoute_ReturnsHome_ForAvailableOrganizationCentricShell()
    {
        var shell = new PublicExperienceShellDto
        {
            Mode = PublicExperienceMode.OrganizationCentric,
            PrimaryOrganization = new PublicExperiencePrimaryOrganizationDto
            {
                State = PublicExperiencePrimaryOrganizationState.Available
            }
        };

        var route = _service.ResolveHomeRoute(shell);

        await Assert.That(route).IsEqualTo("/home");
    }

    [Test]
    public async Task ResolveHomeRoute_ReturnsHome_ForShellLandingPagePreference()
    {
        var shell = new PublicExperienceShellDto
        {
            Mode = PublicExperienceMode.DiscoveryCentric,
            Home = new PublicExperienceHomeDto { PreferredHomePage = "LandingPage" }
        };

        var route = _service.ResolveHomeRoute(shell);

        await Assert.That(route).IsEqualTo("/home");
    }

    [Test]
    public async Task ResolveHomeRoute_ReturnsEvents_ForMissingOrganizationOrNullShell()
    {
        var shell = new PublicExperienceShellDto
        {
            Mode = PublicExperienceMode.OrganizationCentric,
            PrimaryOrganization = new PublicExperiencePrimaryOrganizationDto
            {
                State = PublicExperiencePrimaryOrganizationState.Missing
            }
        };

        await Assert.That(_service.ResolveHomeRoute(shell)).IsEqualTo("/events");
        await Assert.That(_service.ResolveHomeRoute((PublicExperienceShellDto?)null)).IsEqualTo("/events");
    }
}
