// ABOUTME: WCAG 2.2 AA, localization, and RTL guards for the EventLocation privacy UI surface.
// ABOUTME: Covers announced status regions, alert roles, decorative icons, and direction-neutral styling.

using System.Text.RegularExpressions;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Components.Events;
using Explore.Blazor.Client.Localization;
using Explore.Blazor.Client.Models.Events;

namespace Explore.Blazor.Client.Tests.Accessibility;

/// <summary>
/// The privacy UI is the surface where a mistake exposes someone's home address, so its accessibility and
/// localization guarantees are asserted rather than assumed.
/// </summary>
public sealed partial class EventLocationPrivacyAccessibilityTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();

    /// <summary>Components added by this workstream, relative to the Blazor client project root.</summary>
    private static readonly string[] PrivacyComponents =
    [
        Path.Combine("Components", "Events", "EventLocationDisclosureCard.razor"),
        Path.Combine("Components", "Events", "EventLocationManagementTable.razor"),
        Path.Combine("Pages", "Events", "Components", "EventLocationEditorDialog.razor"),
        Path.Combine("Pages", "Events", "Components", "ConfirmRemediationDialog.razor"),
        Path.Combine("Pages", "Events", "Components", "HomeOwnerConsentDialog.razor"),
        Path.Combine("Pages", "Events", "ManageEventLocations.razor"),
        Path.Combine("Pages", "Admin", "EventLocationReviewQueue.razor")
    ];

    [Test]
    [MethodDataSource(nameof(PrivacyComponentSources))]
    public async Task EveryPrivacyComponentResolvesItsCopyThroughTheTranslationService(string relativePath)
    {
        string source = await ReadComponentAsync(relativePath);

        await Assert.That(source).Contains("ITranslationService");
        await Assert.That(source).Contains("Translation.T(");
    }

    [Test]
    [MethodDataSource(nameof(PrivacyComponentSources))]
    public async Task NoPrivacyComponentHardcodesADirectionSpecificLayout(string relativePath)
    {
        string source = await ReadComponentAsync(relativePath);

        // Physical offsets flip incorrectly under Arabic; MudBlazor's logical helpers mirror automatically.
        foreach (Match match in DirectionSpecificCss().Matches(source))
        {
            await Assert.That(match.Value)
                .IsEqualTo(string.Empty, $"{relativePath} uses direction-specific styling: {match.Value}");
        }
    }

    [Test]
    public async Task ArabicRemainsRegisteredAsARightToLeftCulture()
    {
        var arabic = CultureRegistry.GetAll().Single(culture => culture.Code == "ar");

        await Assert.That(arabic.IsRtl).IsTrue();
    }

    [Test]
    public async Task DisclosureCard_AnnouncesTheToBeAnnouncedStateAsTextNotOnlyColor()
    {
        var cut = RenderCard(null);

        // A colour-only badge fails WCAG 1.4.1; the state must also be readable.
        await Assert.That(cut.Find("[data-testid='event-location-tba-badge']").TextContent.Trim())
            .IsNotEmpty();
        await Assert.That(cut.Find("[data-testid='event-location-headline']").TextContent.Trim())
            .IsNotEmpty();
    }

    [Test]
    public async Task DisclosureCard_ExposesTheMapLinkWithSafeExternalRelAttributes()
    {
        var cut = RenderCard(EventLocationDisclosureView.FromAttendee(new EventLocationAttendeeDto
        {
            EventLocationId = Guid.NewGuid(),
            State = EventLocationDisclosureState.Available,
            Fields = new EventLocationAttendeeFieldsDto
            {
                StreetAddress = "Rue Neuve 1",
                MapUrl = "https://maps.example/1"
            }
        }));

        var link = cut.Find("[data-testid='event-location-map']");
        await Assert.That(link.GetAttribute("target")).IsEqualTo("_blank");
        await Assert.That(link.GetAttribute("rel")).IsEqualTo("noopener noreferrer");
    }

    [Test]
    public async Task ManagementTable_LabelsEveryDataCellForSmallViewports()
    {
        var cut = _ctx.Render<EventLocationManagementTable>(parameters => parameters
            .Add(component => component.Rows, ManagementRows())
            .Add(component => component.EmptyMessage, "Nothing to review"));

        // MudTable renders DataLabel as the stacked-view header; a missing label leaves an orphan cell.
        var cells = cut.FindAll("td[data-label]");
        await Assert.That(cells.Count).IsGreaterThanOrEqualTo(5);
        foreach (var cell in cells)
        {
            await Assert.That(cell.GetAttribute("data-label")?.Trim()).IsNotEmpty();
        }
    }

    [Test]
    public async Task ManagementTable_MarksTheEmptyStateAsAStatusRegion()
    {
        var cut = _ctx.Render<EventLocationManagementTable>(parameters => parameters
            .Add(component => component.Rows, Array.Empty<HalResourceOfEventLocationManagementDto>())
            .Add(component => component.EmptyMessage, "Nothing is waiting for privacy review."));

        await Assert.That(cut.FindAll("[role='status']").Count).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    [MethodDataSource(nameof(PrivacyComponentSources))]
    public async Task DecorativeIconsAreHiddenFromAssistiveTechnology(string relativePath)
    {
        string source = await ReadComponentAsync(relativePath);
        if (!source.Contains("<MudIcon", StringComparison.Ordinal))
        {
            return;
        }

        foreach (Match match in MudIconTags().Matches(source))
        {
            await Assert.That(match.Value).Contains("aria-hidden");
        }
    }

    [Test]
    [MethodDataSource(nameof(PrivacyComponentSources))]
    public async Task ErrorsUseAlertRoleAndBusyRegionsAreMarked(string relativePath)
    {
        string source = await ReadComponentAsync(relativePath);

        if (source.Contains("Severity.Error", StringComparison.Ordinal))
        {
            await Assert.That(source).Contains("role=\"alert\"");
        }

        if (source.Contains("_loading", StringComparison.Ordinal) || source.Contains("_busy", StringComparison.Ordinal))
        {
            await Assert.That(source.Contains("aria-busy", StringComparison.Ordinal)
                    || source.Contains("Loading=", StringComparison.Ordinal)
                    || source.Contains("Disabled=", StringComparison.Ordinal))
                .IsTrue();
        }
    }

    [Test]
    [Arguments("Pages/Events/ManageEventLocations.razor")]
    [Arguments("Pages/Admin/EventLocationReviewQueue.razor")]
    public async Task PrivacyPagesExposeOneLabelledLandmarkHeading(string relativePath)
    {
        string source = await ReadComponentAsync(relativePath);

        await Assert.That(source).Contains("aria-labelledby");
        await Assert.That(source).Contains("HtmlTag=\"h1\"");
        await Assert.That(source).Contains("<PageTitle>");
    }

    public void Dispose() => _ctx.Dispose();

    public static IEnumerable<Func<string>> PrivacyComponentSources() =>
        PrivacyComponents.Select<string, Func<string>>(path => () => path);

    private IRenderedComponent<EventLocationDisclosureCard> RenderCard(EventLocationDisclosureView? view) =>
        _ctx.Render<EventLocationDisclosureCard>(parameters =>
            parameters.Add(component => component.View, view));

    private static IReadOnlyList<HalResourceOfEventLocationManagementDto> ManagementRows() =>
    [
        new HalResourceOfEventLocationManagementDto
        {
            EventLocationId = Guid.NewGuid(),
            State = EventLocationDisclosureState.Available,
            NeedsPrivacyReview = false,
            Policy = new EventLocationDisclosurePolicyDto { FullDetailsAudienceId = 3 },
            Fields = new EventLocationManagementFieldsDto { VenueName = "Community Centre" },
            _links = new Dictionary<string, HalLink>()
        }
    ];

    private static Task<string> ReadComponentAsync(string relativePath) =>
        File.ReadAllTextAsync(Path.Combine(ClientRoot, relativePath));

    private static string ClientRoot { get; } = FindClientRoot();

    private static string FindClientRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "src", "Explore.Blazor.Client");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Blazor client project root was not found.");
    }

    [GeneratedRegex(@"(?<![\w-])(margin-left|margin-right|padding-left|padding-right|text-align:\s*(left|right)|float:\s*(left|right))")]
    private static partial Regex DirectionSpecificCss();

    [GeneratedRegex(@"<MudIcon\b[^>]*/>", RegexOptions.Singleline)]
    private static partial Regex MudIconTags();
}
