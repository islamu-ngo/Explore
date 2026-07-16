// ABOUTME: Aspire-backed Playwright evidence for Stage A event-location privacy on public event surfaces.
// ABOUTME: Seeds a physical session while proving responsive list, detail, calendar, metadata, and share outputs stay redacted.

using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.E2ETests.Fixtures;
using Explore.Blazor.Client.Helpers;

namespace Explore.Blazor.Client.E2ETests.Flows;

[Category(E2ETestCategories.E2E)]
[Category(E2ETestCategories.Manual)]
[ClassDataSource<AppHostFixture, PlaywrightFixture>(Shared = [SharedType.PerClass, SharedType.PerTestSession])]
[NotInParallel("E2EAppHostDb")]
[ParallelLimiter<BrowserParallelLimit>]
public sealed class EventLocationPrivacyVisualTests(
    AppHostFixture appHost,
    PlaywrightFixture playwright)
{
    private static readonly JsonSerializerOptions ManifestJsonOptions = new() { WriteIndented = true };
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];
    private static readonly Guid PhysicalLocationId =
        Guid.Parse("018e4e5c-7f00-7000-8000-000000000302");
    private static readonly Guid PhysicalRoomId =
        Guid.Parse("018e4e5c-7f00-7000-8000-000000000320");
    private static readonly Guid PhysicalEventId =
        Guid.Parse("018e4e5c-7f00-7000-8000-000000000063");

    [Test]
    [Timeout(600_000)]
    public async Task PublicEventListAndDetail_KeepExactPhysicalLocationPrivateAcrossResponsiveSurfaces()
    {
        var tokens = await appHost.GetTestAdminTokensAsync();
        var api = appHost.CreateApiClient(tokens.AccessToken);
        var scenario = await SeedPhysicalEventAsync(api);
        var evidenceDirectory = GetEvidenceDirectory();
        PrepareEvidenceDirectory(evidenceDirectory);

        var requestedUrls = new List<string>();
        var page = await playwright.CreatePageAsync(
            nameof(PublicEventListAndDetail_KeepExactPhysicalLocationPrivateAcrossResponsiveSurfaces));
        page.Request += (_, request) => requestedUrls.Add(request.Url);
        await InstallBrowserActionCaptureAsync(page);

        var screenshots = new List<ScreenshotEvidence>();
        try
        {
            await page.SetViewportSizeAsync(1280, 900);
            await NavigateAsync(page, $"{appHost.BlazorBaseUrl}/events");
            await page.GetByPlaceholder("Search events...").FillAsync(scenario.Title);
            await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions
            {
                Name = "Search",
                Exact = true
            }).ClickAsync();
            await page.GetByText(scenario.Title, new PageGetByTextOptions { Exact = true }).First.WaitForAsync();
            await AssertSafeListSurfaceAsync(page, requestedUrls, scenario);

            await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions
            {
                Name = "Filters",
                Exact = true
            }).ClickAsync();
            var desktopFilters = page.Locator(".filter-bar__panel");
            await desktopFilters.WaitForAsync();
            await AssertNoLocationSelectorAsync(desktopFilters);
            screenshots.Add(await CaptureAsync(
                page,
                evidenceDirectory,
                "event-list-desktop-1280x900.png",
                1280,
                900));

            await page.SetViewportSizeAsync(390, 844);
            await CloseShellSidebarIfOpenAsync(page);
            await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions
            {
                Name = "Open filters",
                Exact = true
            }).ClickAsync();
            var mobileFilters = page.Locator(".filter-bar__mobile-drawer");
            await mobileFilters.WaitForAsync();
            await AssertNoLocationSelectorAsync(mobileFilters);
            await AssertSafeListSurfaceAsync(page, requestedUrls, scenario);
            screenshots.Add(await CaptureAsync(
                page,
                evidenceDirectory,
                "event-list-mobile-390x844.png",
                390,
                844));

            await page.SetViewportSizeAsync(1280, 900);
            await NavigateAsync(page, $"{appHost.BlazorBaseUrl}{scenario.PublicPath}");
            await page.GetByText(scenario.Title, new PageGetByTextOptions { Exact = true }).First.WaitForAsync();
            await AssertSafeDetailSurfaceAsync(page, scenario);
            screenshots.Add(await CaptureAsync(
                page,
                evidenceDirectory,
                "event-detail-desktop-1280x900.png",
                1280,
                900));

            await AssertSafeStructuredDataAsync(page, scenario);
            await AssertSafeGoogleCalendarAsync(page, scenario);
            await AssertSafeShareAsync(page, scenario);
            await AssertSafeIcsAsync(api, scenario);

            await page.SetViewportSizeAsync(390, 844);
            await CloseShellSidebarIfOpenAsync(page);
            await AssertSafeDetailSurfaceAsync(page, scenario);
            screenshots.Add(await CaptureAsync(
                page,
                evidenceDirectory,
                "event-detail-mobile-390x844.png",
                390,
                844));

            await WriteManifestAsync(evidenceDirectory, scenario, screenshots);
        }
        finally
        {
            await playwright.ClosePageAsync(
                page,
                nameof(PublicEventListAndDetail_KeepExactPhysicalLocationPrivateAcrossResponsiveSurfaces));
        }
    }

    private static async Task<PrivacyScenario> SeedPhysicalEventAsync(IEventApiClient api)
    {
        var locationList = await api.GetLocationsAsync(pageNumber: 1, pageSize: 100);
        var locationSummary = locationList.GetItems().Single(location => location.Id == PhysicalLocationId);
        var location = await api.GetLocationByIdAsync(PhysicalLocationId);
        var rooms = await api.GetLocationRoomsByLocationAsync(PhysicalLocationId);
        var roomSummary = rooms.GetItems().Single(room => room.Id == PhysicalRoomId);
        var room = await api.GetLocationRoomByIdAsync(PhysicalRoomId);

        var session = (await api.GetManagedEventSessionsByEventAsync(PhysicalEventId))
            .GetItems()
            .Single(item => item.LocationId == PhysicalLocationId && item.RoomId == PhysicalRoomId);
        var sessionId = session.Id
            ?? throw new InvalidOperationException("The physical ELP session did not expose an id.");
        if (sessionId == Guid.Empty)
        {
            throw new InvalidOperationException("The physical ELP session exposed an empty id.");
        }

        var published = await api.GetEventByIdAsync(PhysicalEventId);
        var publicPath = EventUrlHelper.BuildPublicPath(published.Slug, published.PublicCode)
            ?? throw new InvalidOperationException("The published ELP event did not expose a public path.");
        var secrets = new[]
        {
            locationSummary.FullName,
            location.FullName,
            location.Address,
            location.Postcode,
            roomSummary.Name,
            room.Name,
            PhysicalLocationId.ToString("D"),
            PhysicalRoomId.ToString("D"),
            location.Latitude?.ToString(CultureInfo.InvariantCulture),
            location.Longitude?.ToString(CultureInfo.InvariantCulture)
        }
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value!)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

        return new PrivacyScenario(PhysicalEventId, published.Title, publicPath, secrets);
    }

    private static async Task NavigateAsync(IPage page, string url)
    {
        var response = await page.GotoAsync(
            url,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Assert.That(response).IsNotNull();
        await Assert.That(response!.Status).IsEqualTo((int)HttpStatusCode.OK);
        await page.Locator("[data-blazor-interactive='true']")
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
    }

    private static async Task AssertSafeListSurfaceAsync(
        IPage page,
        IEnumerable<string> requestedUrls,
        PrivacyScenario scenario)
    {
        await AssertNoPhysicalValuesAsync(page, scenario);
        await Assert.That(page.Url.Contains("locationIds", StringComparison.OrdinalIgnoreCase)).IsFalse();
        await Assert.That(requestedUrls.Any(url =>
            url.Contains("locationIds", StringComparison.OrdinalIgnoreCase))).IsFalse();
        await Assert.That(await page.GetByText("Hybrid", new PageGetByTextOptions
        {
            Exact = true
        }).CountAsync()).IsGreaterThanOrEqualTo(1);
    }

    private static async Task AssertSafeDetailSurfaceAsync(IPage page, PrivacyScenario scenario)
    {
        await AssertNoPhysicalValuesAsync(page, scenario);
        var bodyText = await page.Locator("body").InnerTextAsync();
        foreach (var promise in new[]
                 {
                     "register to see private address",
                     "register to view private address",
                     "sign in to see private address",
                     "address will be shared after registration",
                     "exact location will be shared after registration"
                 })
        {
            await Assert.That(bodyText.Contains(promise, StringComparison.OrdinalIgnoreCase)).IsFalse();
        }
    }

    private static async Task AssertNoLocationSelectorAsync(ILocator filters)
    {
        var selectorCount = await filters.Locator(
            "[name*='location' i], [aria-label*='location' i], [data-testid*='location' i]").CountAsync();
        await Assert.That(selectorCount).IsEqualTo(0);
        var labels = await filters.Locator(".filter-bar__label").AllTextContentsAsync();
        await Assert.That(labels.Any(label =>
            string.Equals(label.Trim(), "Location", StringComparison.OrdinalIgnoreCase))).IsFalse();
    }

    private static async Task AssertNoPhysicalValuesAsync(IPage page, PrivacyScenario scenario)
    {
        var surface = string.Concat(await page.ContentAsync(), "\n", await page.Locator("body").InnerTextAsync());
        foreach (var secret in scenario.PhysicalSecrets)
        {
            await Assert.That(surface.Contains(secret, StringComparison.OrdinalIgnoreCase)).IsFalse();
        }
    }

    private static async Task AssertSafeStructuredDataAsync(IPage page, PrivacyScenario scenario)
    {
        var structuredData = await page.Locator("script[type='application/ld+json']").TextContentAsync();
        await Assert.That(structuredData).IsNotNull();
        await AssertNoSecretsAsync(structuredData!, scenario);
        using var document = JsonDocument.Parse(structuredData!);
        await Assert.That(ContainsProperty(document.RootElement, "location")).IsFalse();
    }

    private static async Task AssertSafeGoogleCalendarAsync(IPage page, PrivacyScenario scenario)
    {
        await page.GetByText("Add to Google Calendar", new PageGetByTextOptions { Exact = true }).ClickAsync();
        await page.WaitForFunctionAsync("() => Array.isArray(window.__elpOpenedUrls) && window.__elpOpenedUrls.length > 0");
        var openedUrl = await page.EvaluateAsync<string>("() => window.__elpOpenedUrls.at(-1)");
        await Assert.That(openedUrl).StartsWith("https://calendar.google.com/");
        await Assert.That(openedUrl.Contains("location=", StringComparison.OrdinalIgnoreCase)).IsFalse();
        await AssertNoSecretsAsync(openedUrl, scenario);
    }

    private static async Task AssertSafeShareAsync(IPage page, PrivacyScenario scenario)
    {
        await page.GetByText("Share Event", new PageGetByTextOptions { Exact = true }).ClickAsync();
        await page.WaitForFunctionAsync("() => window.__elpShareData?.url");
        var shareJson = await page.EvaluateAsync<string>("() => JSON.stringify(window.__elpShareData)");
        await AssertNoSecretsAsync(shareJson, scenario);
        using var share = JsonDocument.Parse(shareJson);
        var shareUrl = share.RootElement.GetProperty("url").GetString();
        var canonicalUrl = await page.Locator("link[rel='canonical']").GetAttributeAsync("href");
        await Assert.That(shareUrl).IsEqualTo(canonicalUrl);
        await Assert.That(share.RootElement.EnumerateObject().Select(property => property.Name))
            .IsEquivalentTo(["title", "url"]);
    }

    private static async Task AssertSafeIcsAsync(IEventApiClient api, PrivacyScenario scenario)
    {
        var calendar = await api.GetEventCalendarAsync(scenario.EventId);
        await Assert.That(calendar.FileContents).IsNotNull();
        var ics = Encoding.UTF8.GetString(calendar.FileContents!);
        await Assert.That(ics.Contains("LOCATION:", StringComparison.OrdinalIgnoreCase)).IsFalse();
        await AssertNoSecretsAsync(ics, scenario);
    }

    private static async Task AssertNoSecretsAsync(string value, PrivacyScenario scenario)
    {
        foreach (var secret in scenario.PhysicalSecrets)
        {
            await Assert.That(value.Contains(secret, StringComparison.OrdinalIgnoreCase)).IsFalse();
        }
    }

    private static bool ContainsProperty(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase)
                    || ContainsProperty(property.Value, propertyName))
                {
                    return true;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray().Any(item => ContainsProperty(item, propertyName));
        }

        return false;
    }

    private static async Task InstallBrowserActionCaptureAsync(IPage page)
    {
        await page.Context.AddInitScriptAsync(
            """
            window.__elpOpenedUrls = [];
            window.__elpShareData = null;
            window.open = url => { window.__elpOpenedUrls.push(String(url)); return null; };
            Object.defineProperty(navigator, 'share', {
                configurable: true,
                value: async data => { window.__elpShareData = data; }
            });
            """);
    }

    private static async Task CloseShellSidebarIfOpenAsync(IPage page)
    {
        var toggle = page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Toggle sidebar navigation" });
        if (await toggle.CountAsync() > 0
            && string.Equals(await toggle.GetAttributeAsync("aria-expanded"), "true", StringComparison.Ordinal))
        {
            await toggle.ClickAsync();
        }
    }

    private static async Task<ScreenshotEvidence> CaptureAsync(
        IPage page,
        string evidenceDirectory,
        string fileName,
        int width,
        int height)
    {
        await page.SetViewportSizeAsync(width, height);
        var scrollWidth = await page.EvaluateAsync<int>("() => document.documentElement.scrollWidth");
        var innerWidth = await page.EvaluateAsync<int>("() => window.innerWidth");
        await Assert.That(innerWidth).IsEqualTo(width);
        await Assert.That(scrollWidth).IsLessThanOrEqualTo(innerWidth);

        var path = Path.Combine(evidenceDirectory, fileName);
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            FullPage = false,
            Path = path
        });
        var bytes = await File.ReadAllBytesAsync(path);
        var signatureValid = bytes.Length >= 24
            && bytes.AsSpan(0, 8).SequenceEqual(PngSignature);
        var actualWidth = bytes.Length >= 24
            ? BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4))
            : 0;
        var actualHeight = bytes.Length >= 24
            ? BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4))
            : 0;
        await Assert.That(signatureValid).IsTrue();
        await Assert.That(actualWidth).IsEqualTo(width);
        await Assert.That(actualHeight).IsEqualTo(height);

        return new ScreenshotEvidence(
            fileName,
            actualWidth,
            actualHeight,
            "89504E470D0A1A0A",
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
            scrollWidth);
    }

    private static async Task WriteManifestAsync(
        string evidenceDirectory,
        PrivacyScenario scenario,
        IReadOnlyCollection<ScreenshotEvidence> screenshots)
    {
        var manifest = new
        {
            scenario = nameof(PublicEventListAndDetail_KeepExactPhysicalLocationPrivateAcrossResponsiveSurfaces),
            generatedAtUtc = DateTimeOffset.UtcNow,
            eventId = scenario.EventId,
            publicPath = scenario.PublicPath,
            screenshots,
            assertions = new[]
            {
                "No public location selector or locationIds URL fragment",
                "No exact physical identifiers, venue, address, postcode, room, or coordinates in DOM",
                "Safe TBA copy without a private-address promise",
                "No physical location fragment or exact value in JSON-LD",
                "No location query parameter or exact value in Google Calendar share URL",
                "No LOCATION property or exact value in public ICS",
                "Share payload contains only title and canonical URL",
                "PNG signature and exact viewport dimensions verified",
                "No horizontal overflow at either viewport"
            },
            result = "passed"
        };
        await File.WriteAllTextAsync(
            Path.Combine(evidenceDirectory, "manifest.json"),
            JsonSerializer.Serialize(manifest, ManifestJsonOptions));
    }

    private static string GetEvidenceDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null
               && !Directory.Exists(Path.Combine(current.FullName, ".git"))
               && !File.Exists(Path.Combine(current.FullName, ".git")))
        {
            current = current.Parent;
        }

        if (current is null)
        {
            throw new InvalidOperationException("Could not locate the repository root for ELP visual evidence.");
        }

        return Path.Combine(
            current.FullName,
            ".omo",
            "evidence",
            "event-location-privacy",
            "task-2-visual");
    }

    private static void PrepareEvidenceDirectory(string evidenceDirectory)
    {
        Directory.CreateDirectory(evidenceDirectory);
        foreach (var fileName in new[]
                 {
                     "event-list-desktop-1280x900.png",
                     "event-list-mobile-390x844.png",
                     "event-detail-desktop-1280x900.png",
                     "event-detail-mobile-390x844.png",
                     "manifest.json"
                 })
        {
            File.Delete(Path.Combine(evidenceDirectory, fileName));
        }
    }

    private sealed record PrivacyScenario(
        Guid EventId,
        string Title,
        string PublicPath,
        IReadOnlyCollection<string> PhysicalSecrets);

    private sealed record ScreenshotEvidence(
        string FileName,
        int Width,
        int Height,
        string PngSignature,
        string Sha256,
        int ScrollWidth);
}
