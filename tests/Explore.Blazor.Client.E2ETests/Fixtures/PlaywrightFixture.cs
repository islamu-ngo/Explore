// ABOUTME: Session-scoped Playwright fixture managing browser lifecycle for E2E tests.
// ABOUTME: Installs Chromium on first use and provides page creation for tests.

namespace Explore.Blazor.Client.E2ETests.Fixtures;

public sealed class PlaywrightFixture : IAsyncInitializer, IAsyncDisposable
{
    private static readonly TimeSpan ArtifactTimeout = TimeSpan.FromSeconds(5);

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly string _artifactRoot = Path.Combine(
        AppContext.BaseDirectory,
        "TestResults",
        "playwright-artifacts");

    public IBrowser Browser => _browser ?? throw new InvalidOperationException("Browser not initialized");

    public async Task InitializeAsync()
    {
        var exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Playwright install failed: {exitCode}");
        }

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    public async Task<IPage> CreatePageAsync(string artifactName = "page")
    {
        Directory.CreateDirectory(_artifactRoot);

        var safeArtifactName = SanitizeArtifactName(artifactName);
        var artifactId = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}-{safeArtifactName}";
        var videoDirectory = Path.Combine(_artifactRoot, "videos", artifactId);
        Directory.CreateDirectory(videoDirectory);

        var context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            RecordVideoDir = videoDirectory
        });

        await context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true,
            Title = safeArtifactName
        });

        return await context.NewPageAsync();
    }

    public async Task ClosePageAsync(IPage page, string artifactName)
    {
        var context = page.Context;
        var safeArtifactName = SanitizeArtifactName(artifactName);
        var screenshotPath = Path.Combine(_artifactRoot, $"{safeArtifactName}.png");
        var tracePath = Path.Combine(_artifactRoot, $"{safeArtifactName}.zip");

        try
        {
            if (!page.IsClosed)
            {
                await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    FullPage = true,
                    Path = screenshotPath,
                    Timeout = (float)ArtifactTimeout.TotalMilliseconds
                });
            }
        }
        catch (TimeoutException)
        {
            // Artifact capture is best-effort. A slow screenshot must not mask the
            // actual E2E assertion result after the browser flow has completed.
        }
        finally
        {
            await context.Tracing.StopAsync(new TracingStopOptions { Path = tracePath });
            await context.CloseAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }

    private static string SanitizeArtifactName(string artifactName)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string(artifactName
            .Select(character => invalidCharacters.Contains(character) ? '-' : character)
            .ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? "page" : sanitized;
    }
}
