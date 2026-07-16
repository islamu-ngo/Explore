// ABOUTME: Aspire-backed Playwright coverage for the public home discovery route and privacy boundary.
// ABOUTME: Exercises responsive rendering, manual hero, area/online actions, geolocation, and origin containment.

using System.Diagnostics;
using System.Text.Json;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.E2ETests.Fixtures;
using Explore.Blazor.Client.E2ETests.Seeds;

namespace Explore.Blazor.Client.E2ETests.Flows;

[Category(E2ETestCategories.E2E)]
[Category(E2ETestCategories.Slow)]
[ClassDataSource<AppHostFixture, PlaywrightFixture>(Shared = [SharedType.PerClass, SharedType.PerTestSession])]
[NotInParallel("E2EAppHostDb")]
[ParallelLimiter<BrowserParallelLimit>]
public sealed class HomeDiscoveryFlowTests(
    AppHostFixture appHost,
    PlaywrightFixture playwright)
{
    private const int PerformanceSampleCount = 20;
    private const double UncachedP95BudgetMilliseconds = 800;
    private const double CachedP95BudgetMilliseconds = 200;
    private const double MobileLcpBudgetMilliseconds = 2_500;
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    [Test]
    [Timeout(600_000)]
    public async Task PublicHomeRendersOnceSwitchesContextAndKeepsOriginTransient()
    {
        var adminTokens = await appHost.GetTestAdminTokensAsync();
        var adminApi = appHost.CreateApiClient(adminTokens.AccessToken);
        var areaId = Guid.NewGuid();
        await ConfigureDiscoveryAsync(adminApi, areaId);
        try
        {
            await SeedHeroEventsAsync(adminApi);
        }
        catch (ApiException<ValidationProblemDetails> exception)
        {
            throw CreateValidationDiagnostic("Seeding Home Discovery events", exception);
        }

        var page = await playwright.CreatePageAsync(nameof(PublicHomeRendersOnceSwitchesContextAndKeepsOriginTransient));
        var consoleErrors = new List<string>();
        var discoveryRequests = new List<string>();
        var homeDocumentRequests = new List<string>();
        await InstallLcpObserverAsync(page);
        page.Console += (_, message) =>
        {
            if (string.Equals(message.Type, "error", StringComparison.OrdinalIgnoreCase))
                consoleErrors.Add(message.Text);
        };
        page.Request += (_, request) =>
        {
            if (request.Url.Contains("/api/public-experience/home", StringComparison.Ordinal))
                discoveryRequests.Add(request.Url);

            if (string.Equals(request.ResourceType, "document", StringComparison.Ordinal) &&
                string.Equals(new Uri(request.Url).AbsolutePath, "/home", StringComparison.Ordinal))
            {
                homeDocumentRequests.Add(request.Url);
            }
        };

        try
        {
            await page.SetViewportSizeAsync(1280, 900);
            var response = await page.GotoAsync(
                $"{appHost.BlazorBaseUrl}/home?areaId={areaId:D}&mode=all",
                new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await Assert.That(response).IsNotNull();
            await Assert.That(response!.Status).IsEqualTo((int)HttpStatusCode.OK);
            await page.Locator("[data-blazor-interactive='true']")
                .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
            await page.Locator("[data-testid='home-discovery-context']").WaitForAsync();

            await Assert.That(await page.Locator("h1").CountAsync()).IsEqualTo(1);
            await Assert.That(await page.Locator("[data-testid='hero-carousel']").CountAsync()).IsEqualTo(1);
            await Assert.That(await page.Locator(".event-card--DetailedList").CountAsync()).IsGreaterThanOrEqualTo(1);
            await Assert.That(await page.Locator("[data-testid='home-discovery-context']").TextContentAsync())
                .Contains("all areas");
            await AssertInitialHeroTransferAsync(page);
            await AssertManualHeroAsync(page);
            await CaptureResponsiveMatrixAsync(page);
            await AssertKeyboardSurfacesAsync(page);

            var origin = new Uri(appHost.BlazorBaseUrl).GetLeftPart(UriPartial.Authority);
            await page.Context.GrantPermissionsAsync(
                ["geolocation"],
                new BrowserContextGrantPermissionsOptions { Origin = origin });
            await page.Context.SetGeolocationAsync(new Geolocation
            {
                Latitude = 50.8466f,
                Longitude = 4.3528f,
                Accuracy = 100
            });
            await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Use my current location" }).ClickAsync();
            await page.WaitForURLAsync($"**/home?areaId={areaId:D}&mode=area");
            await page.Locator("[data-testid='home-discovery-context']")
                .Filter(new LocatorFilterOptions { HasText = "Brussels" })
                .WaitForAsync();
            await Assert.That(homeDocumentRequests.Count).IsEqualTo(1);

            await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Browse online events" }).ClickAsync();
            await page.WaitForURLAsync($"**/home?areaId={areaId:D}&mode=online");
            await page.Locator("[data-testid='home-discovery-context']")
                .Filter(new LocatorFilterOptions { HasText = "online events" })
                .WaitForAsync();
            await Assert.That(homeDocumentRequests.Count).IsEqualTo(1);

            await page.EvaluateAsync(
                """
                () => Object.defineProperty(navigator.geolocation, "getCurrentPosition", {
                    configurable: true,
                    value: (_, onError) => onError({ code: 1, PERMISSION_DENIED: 1 })
                })
                """);
            await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Use my current location" }).ClickAsync();
            await page.GetByText("Location permission was denied. Your previous area is unchanged.").WaitForAsync();
            await Assert.That(page.Url).Contains($"areaId={areaId:D}");
            await Assert.That(page.Url).Contains("mode=online");
            await Assert.That(homeDocumentRequests.Count).IsEqualTo(1);

            var localStorage = await page.EvaluateAsync<string>(
                "() => Object.entries(localStorage).map(([key, value]) => `${key}=${value}`).join('|')");
            foreach (var sink in discoveryRequests.Append(page.Url).Append(localStorage))
            {
                await Assert.That(sink).DoesNotContain("50.8466");
                await Assert.That(sink).DoesNotContain("4.3528");
                await Assert.That(sink).DoesNotContain("latitude");
                await Assert.That(sink).DoesNotContain("longitude");
                await Assert.That(sink).DoesNotContain("origin");
            }

            var apiPerformance = await AssertHomeDiscoveryApiPerformanceAsync(appHost.ApiBaseUrl, areaId);
            var mobileLcp = await AssertMobileLcpAsync(
                page,
                $"{appHost.BlazorBaseUrl}/home?areaId={areaId:D}&mode=all");
            await WritePerformanceArtifactAsync(apiPerformance, mobileLcp);
            await Assert.That(consoleErrors).IsEmpty();
        }
        finally
        {
            await playwright.ClosePageAsync(page, nameof(PublicHomeRendersOnceSwitchesContextAndKeepsOriginTransient));
        }
    }

    private static async Task ConfigureDiscoveryAsync(
        IEventApiClient api,
        Guid areaId)
    {
        var config = JsonSerializer.Serialize(
            new
            {
                schemaVersion = 1,
                areas = new[]
                {
                    new
                    {
                        id = areaId,
                        displayName = "Brussels",
                        city = "Brussels",
                        countryCode = "BE",
                        centroidLatitude = 50.85m,
                        centroidLongitude = 4.35m,
                        locationIds = Array.Empty<Guid>(),
                        isActive = true,
                        isDefault = true,
                        sortOrder = 0
                    }
                }
            },
            WebJsonOptions);
        BatchUpdateResponseDto result;
        try
        {
            result = await api.UpdateTenantSettingsBatchAsync(
                "PublicExperience",
                new UpdateSettingBatchDto
                {
                    Values = new Dictionary<string, string>
                    {
                        ["public_experience.discovery_areas"] = config
                    },
                    Mode = BatchUpdateMode.Strict
                });
        }
        catch (ApiException<ValidationProblemDetails> exception)
        {
            throw CreateValidationDiagnostic("Configuring the public discovery area", exception);
        }
        if (result.Success != true)
        {
            throw new InvalidOperationException(
                $"Configuring the public discovery area failed: {result.Message}");
        }
    }

    private static async Task SeedHeroEventsAsync(IEventApiClient api)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        await EventApiScenario.CreatePublishedEventAsync(
            api,
            $"A long public discovery event title that proves wrapping remains bounded across responsive cards {suffix}",
            $"home-discovery-long-{suffix}",
            startsInDays: 3);
        await EventApiScenario.CreatePublishedEventAsync(
            api,
            $"Public discovery event without an uploaded image {suffix}",
            $"home-discovery-fallback-{suffix}",
            startsInDays: 3);
        for (var index = 0; index < 24; index++)
        {
            await EventApiScenario.CreatePublishedEventAsync(
                api,
                $"Public discovery fixture {index + 1} {suffix}",
                $"home-discovery-{index + 1}-{suffix}",
                startsInDays: 3);
        }
    }

    private static InvalidOperationException CreateValidationDiagnostic(
        string operation,
        ApiException<ValidationProblemDetails> exception)
    {
        var errors = exception.Result.Errors is { Count: > 0 }
            ? string.Join(
                " | ",
                exception.Result.Errors.SelectMany(entry =>
                    entry.Value.Select(message => $"{entry.Key}: {message}")))
            : "none";

        return new InvalidOperationException(
            $"{operation} returned HTTP {exception.StatusCode}. " +
            $"Title={exception.Result.Title}; Detail={exception.Result.Detail}; Errors={errors}",
            exception);
    }

    private static async Task AssertManualHeroAsync(IPage page)
    {
        var counter = page.Locator("[data-testid='hero-counter']");
        await Assert.That(await counter.CountAsync()).IsEqualTo(1);

        var initial = await counter.TextContentAsync();
        await page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Next featured event" }).ClickAsync();
        await Assert.That(await counter.TextContentAsync()).IsNotEqualTo(initial);

        var activeImage = page.Locator("[data-testid='hero-slide']:not([hidden]) img");
        for (var attempt = 0; attempt < 20; attempt++)
        {
            if (await activeImage.EvaluateAsync<int>("image => image.naturalWidth") > 0)
            {
                return;
            }

            await Task.Delay(100);
        }

        await Assert.That(await activeImage.EvaluateAsync<int>("image => image.naturalWidth"))
            .IsGreaterThan(0);
    }

    private static async Task AssertInitialHeroTransferAsync(IPage page)
    {
        var encodedBodySizes = await page.EvaluateAsync<long[]>(
            """
            () => performance.getEntriesByType("resource")
                .filter(entry => new URL(entry.name).pathname === "/image/landing_image_nonuser.png")
                .map(entry => entry.encodedBodySize)
            """);

        await Assert.That(encodedBodySizes.Length).IsEqualTo(1);
        await Assert.That(encodedBodySizes.Single()).IsLessThanOrEqualTo(500L * 1024L);
    }

    private static async Task AssertKeyboardSurfacesAsync(IPage page)
    {
        var card = page.Locator(".event-card--DetailedList[role='button']").First;
        await card.FocusAsync();
        await Assert.That(await card.EvaluateAsync<bool>("element => element === document.activeElement"))
            .IsTrue();

        var rail = page.Locator("[data-testid='event-horizontal-rail']").First;
        await Assert.That(await rail.CountAsync()).IsEqualTo(1);
        await Assert.That(await rail.EvaluateAsync<int>("element => element.scrollWidth"))
            .IsGreaterThan(await rail.EvaluateAsync<int>("element => element.clientWidth"));
        await rail.FocusAsync();
        await rail.PressAsync("ArrowRight");
        await Task.Delay(250);
        await Assert.That(Math.Abs(await rail.EvaluateAsync<int>("element => element.scrollLeft")))
            .IsGreaterThan(0);
    }

    private static async Task<(double UncachedP95Milliseconds, double CachedP95Milliseconds)>
        AssertHomeDiscoveryApiPerformanceAsync(
        string apiBaseUrl,
        Guid areaId)
    {
        using var client = new HttpClient
        {
            BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/", UriKind.Absolute)
        };
        client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("br");

        var uncachedSamples = new List<double>(PerformanceSampleCount);
        for (var index = 0; index < PerformanceSampleCount; index++)
        {
            var sample = await MeasureHomeDiscoveryRequestAsync(client, Guid.NewGuid());
            uncachedSamples.Add(sample.Duration.TotalMilliseconds);
            await Assert.That(sample.EncodedBodyBytes).IsLessThanOrEqualTo(120L * 1024L);
        }

        _ = await MeasureHomeDiscoveryRequestAsync(client, areaId);
        var cachedSamples = new List<double>(PerformanceSampleCount);
        for (var index = 0; index < PerformanceSampleCount; index++)
        {
            var sample = await MeasureHomeDiscoveryRequestAsync(client, areaId);
            cachedSamples.Add(sample.Duration.TotalMilliseconds);
        }

        var uncachedP95 = P95(uncachedSamples);
        var cachedP95 = P95(cachedSamples);
        await Assert.That(uncachedP95).IsLessThanOrEqualTo(UncachedP95BudgetMilliseconds);
        await Assert.That(cachedP95).IsLessThanOrEqualTo(CachedP95BudgetMilliseconds);
        return (uncachedP95, cachedP95);
    }

    private static async Task<(TimeSpan Duration, long EncodedBodyBytes)> MeasureHomeDiscoveryRequestAsync(
        HttpClient client,
        Guid areaId)
    {
        var startedAt = Stopwatch.GetTimestamp();
        using var response = await client.GetAsync(
            $"api/public-experience/home?areaId={areaId:D}&mode=all",
            HttpCompletionOption.ResponseContentRead);
        var duration = Stopwatch.GetElapsedTime(startedAt);
        var body = await response.Content.ReadAsByteArrayAsync();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentEncoding).Contains("br");
        return (duration, body.LongLength);
    }

    private static double P95(List<double> samples)
    {
        samples.Sort();
        var index = (int)Math.Ceiling(samples.Count * 0.95) - 1;
        return samples[Math.Max(index, 0)];
    }

    private static Task InstallLcpObserverAsync(IPage page) =>
        page.AddInitScriptAsync(
            """
            window.__homeDiscoveryLcp = 0;
            new PerformanceObserver(list => {
                const entries = list.getEntries();
                const latest = entries[entries.length - 1];
                if (latest) window.__homeDiscoveryLcp = latest.startTime;
            }).observe({ type: "largest-contentful-paint", buffered: true });
            """);

    private static async Task<double> AssertMobileLcpAsync(IPage page, string url)
    {
        var session = await page.Context.NewCDPSessionAsync(page);
        double lcp;
        try
        {
            await session.SendAsync("Network.enable");
            await session.SendAsync(
                "Network.setCacheDisabled",
                new Dictionary<string, object> { ["cacheDisabled"] = true });
            await session.SendAsync(
                "Network.emulateNetworkConditions",
                new Dictionary<string, object>
                {
                    ["offline"] = false,
                    ["latency"] = 100,
                    ["downloadThroughput"] = 4_000_000 / 8,
                    ["uploadThroughput"] = 1_500_000 / 8,
                    ["connectionType"] = "cellular4g"
                });
            await session.SendAsync(
                "Emulation.setCPUThrottlingRate",
                new Dictionary<string, object> { ["rate"] = 4 });
            await page.SetViewportSizeAsync(375, 844);

            var response = await page.GotoAsync(
                url,
                new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await Assert.That(response).IsNotNull();
            await Assert.That(response!.Status).IsEqualTo((int)HttpStatusCode.OK);
            await page.Locator("[data-testid='home-discovery-context']").WaitForAsync();
            await page.WaitForFunctionAsync("() => window.__homeDiscoveryLcp > 0");
            lcp = await page.EvaluateAsync<double>("() => window.__homeDiscoveryLcp");

            await Assert.That(lcp).IsLessThanOrEqualTo(MobileLcpBudgetMilliseconds);
        }
        finally
        {
            await session.SendAsync(
                "Network.emulateNetworkConditions",
                new Dictionary<string, object>
                {
                    ["offline"] = false,
                    ["latency"] = 0,
                    ["downloadThroughput"] = -1,
                    ["uploadThroughput"] = -1
                });
            await session.SendAsync(
                "Emulation.setCPUThrottlingRate",
                new Dictionary<string, object> { ["rate"] = 1 });
            await session.SendAsync(
                "Network.setCacheDisabled",
                new Dictionary<string, object> { ["cacheDisabled"] = false });
            await session.DetachAsync();
        }

        return lcp;
    }

    private static async Task WritePerformanceArtifactAsync(
        (double UncachedP95Milliseconds, double CachedP95Milliseconds) apiPerformance,
        double mobileLcpMilliseconds)
    {
        var artifactDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "TestResults",
            "playwright-artifacts",
            "home-discovery");
        Directory.CreateDirectory(artifactDirectory);
        var artifact = JsonSerializer.Serialize(
            new
            {
                api = new
                {
                    sampleCount = PerformanceSampleCount,
                    uncachedP95Milliseconds = apiPerformance.UncachedP95Milliseconds,
                    uncachedBudgetMilliseconds = UncachedP95BudgetMilliseconds,
                    cachedP95Milliseconds = apiPerformance.CachedP95Milliseconds,
                    cachedBudgetMilliseconds = CachedP95BudgetMilliseconds
                },
                lcp = new
                {
                    milliseconds = mobileLcpMilliseconds,
                    budgetMilliseconds = MobileLcpBudgetMilliseconds,
                    viewport = "375x844",
                    latencyMilliseconds = 100,
                    downloadKilobitsPerSecond = 4_000,
                    uploadKilobitsPerSecond = 1_500,
                    cpuSlowdownMultiplier = 4
                }
            },
            WebJsonOptions);
        await File.WriteAllTextAsync(
            Path.Combine(artifactDirectory, "performance.json"),
            artifact);
    }

    private static async Task CaptureResponsiveMatrixAsync(IPage page)
    {
        var artifactDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "TestResults",
            "playwright-artifacts",
            "home-discovery");
        Directory.CreateDirectory(artifactDirectory);

        await CaptureAsync(page, artifactDirectory, "375-light-ltr", 375, 844);
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Dark });
        await CaptureAsync(page, artifactDirectory, "768-dark-ltr", 768, 1024);
        await page.EmulateMediaAsync(new PageEmulateMediaOptions
        {
            ColorScheme = ColorScheme.Light,
            ReducedMotion = ReducedMotion.Reduce
        });
        await page.EvaluateAsync("() => document.documentElement.setAttribute('dir', 'rtl')");
        await CaptureAsync(page, artifactDirectory, "1280-light-rtl-reduced", 1280, 900);
        await page.EvaluateAsync("() => document.documentElement.setAttribute('dir', 'ltr')");
        await page.EmulateMediaAsync(new PageEmulateMediaOptions
        {
            ColorScheme = ColorScheme.Light,
            ReducedMotion = ReducedMotion.NoPreference
        });
    }

    private static async Task CaptureAsync(
        IPage page,
        string artifactDirectory,
        string name,
        int width,
        int height)
    {
        await page.SetViewportSizeAsync(width, height);
        if (width <= 760)
        {
            await page.Locator(
                    "[data-dock-scope='shell']:not(.dock-layout-host--has-start)")
                .WaitForAsync();
            var sidebarToggle = page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Toggle sidebar navigation" });
            if (await sidebarToggle.GetAttributeAsync("aria-expanded") == "true")
            {
                var overlay = page.Locator(
                    "[data-testid='dock-overlay-host'][data-dock-scope='shell']");
                await overlay.WaitForAsync();
                await page.GetByRole(
                    AriaRole.Button,
                    new PageGetByRoleOptions { Name = "Close sidebar navigation" }).ClickAsync();
                await overlay.WaitForAsync(
                    new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
            }
        }

        await page.EvaluateAsync(
            "() => { document.activeElement?.blur(); window.scrollTo(0, 0); }");
        await page.WaitForFunctionAsync("() => window.scrollY === 0");
        var headerBox = await page.Locator(".main-layout__header").BoundingBoxAsync();
        var headingBox = await page.Locator("h1").BoundingBoxAsync();
        await Assert.That(headerBox).IsNotNull();
        await Assert.That(headingBox).IsNotNull();
        await Assert.That(headingBox!.Y).IsGreaterThanOrEqualTo(headerBox!.Y + headerBox.Height);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            FullPage = true,
            Path = Path.Combine(artifactDirectory, $"{name}.png")
        });
        var scrollWidth = await page.EvaluateAsync<int>("() => document.documentElement.scrollWidth");
        await Assert.That(scrollWidth).IsLessThanOrEqualTo(width);
    }
}
