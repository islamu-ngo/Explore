// ABOUTME: Browser E2E coverage for the single-tenant webhook management surface.
// ABOUTME: Exercises LocalProvider UI actions, HAL affordances, and responsive screenshot evidence.

using System.Net.Http.Headers;
using System.Text.Json;
using Explore.Blazor.Client.E2ETests.Fixtures;
using Explore.Blazor.Client.E2ETests.Seeds;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Explore.Blazor.Client.E2ETests.Flows.Webhooks;

[Category(E2ETestCategories.E2E)]
[ClassDataSource<AppHostFixture, PlaywrightFixture>(Shared = [SharedType.PerTestSession, SharedType.PerTestSession])]
[NotInParallel("E2EAppHostDb")]
[ParallelLimiter<BrowserParallelLimit>]
public sealed class WebhookManagementFlowTests(
    AppHostFixture appHost,
    PlaywrightFixture playwright)
{
    private static readonly float BrowserTimeoutMilliseconds = (float)TimeSpan.FromSeconds(90).TotalMilliseconds;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Test]
    [Timeout(600_000)]
    public async Task InstanceAdminWebhooks_ManagesLocalProviderAndCapturesResponsiveScreenshots()
    {
        await appHost.ResetDatabaseAsync();
        var adminTokens = await appHost.GetTestAdminTokensAsync();
        var adminProviderSubjects = ResolveJwtProviderSubjects(adminTokens.IdToken)
            .Concat(ResolveJwtProviderSubjects(adminTokens.AccessToken))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var adminUserId = ResolveJwtCurrentUserId(adminTokens.AccessToken) ??
            ResolveJwtCurrentUserId(adminTokens.IdToken);

        WebhookManagementScenarioSeed.Result seed;
        await using (var context = appHost.CreateDbContext())
        {
            seed = await WebhookManagementScenarioSeed.SeedAsync(context, adminProviderSubjects, adminUserId);
        }

        await AssertDirectAdminAuthorityAsync(adminTokens.AccessToken, seed, adminProviderSubjects);

        var page = await playwright.CreatePageAsync(nameof(InstanceAdminWebhooks_ManagesLocalProviderAndCapturesResponsiveScreenshots));
        try
        {
            await BffCookieAuthHelper.LoginAsTestAdminAsync(page, appHost, "/auth/status");
            var syncedBrowserUserId = await SyncBrowserUserAsync(page);
            var currentUserSnapshot = await SnapshotApiCurrentUserAsync(page);
            var apiResolvedUserId = currentUserSnapshot.ResolvedUserId ??
                throw new InvalidOperationException(
                    $"API current-user snapshot did not resolve a user id. Snapshot={currentUserSnapshot}");
            await using (var context = appHost.CreateDbContext())
            {
                await WebhookManagementScenarioSeed.GrantInstanceAdminAsync(context, syncedBrowserUserId);
                if (apiResolvedUserId != syncedBrowserUserId)
                {
                    await WebhookManagementScenarioSeed.GrantInstanceAdminAsync(context, apiResolvedUserId);
                }
            }

            await InvalidateApiAdminCacheAsync(page, syncedBrowserUserId);
            if (apiResolvedUserId != syncedBrowserUserId)
            {
                await InvalidateApiAdminCacheAsync(page, apiResolvedUserId);
            }

            await AssertAdminAuthorityAsync(page, seed, adminProviderSubjects, syncedBrowserUserId, currentUserSnapshot);
            await OpenWebhookSettingsAsync(page);

            await AssertInitialWebhookSurfaceAsync(page, seed.DryRunEndpointUrl);
            await CreateLocalEndpointAsync(page);
            await RotateExistingEndpointSecretAsync(page, seed.ExistingEndpointUrl);
            await RetrySeededFailedAttemptAsync(page, seed.FailedAttemptId);
            await TestExistingEndpointAsync(page, "https://hooks.example.test/islamu-created");
            await AssertDryRunEndpointHasNoDeliveryAttemptsAsync(seed.DryRunEndpointId);
            await CaptureResponsiveScreenshotsAsync(page);
            await OpenSvixProviderPortalAsync(page, seed.SvixConsumerId);
        }
        finally
        {
            await playwright.ClosePageAsync(
                page,
                nameof(InstanceAdminWebhooks_ManagesLocalProviderAndCapturesResponsiveScreenshots));
        }
    }

    private async Task<Guid> SyncBrowserUserAsync(IPage page)
    {
        var response = await page.Context.APIRequest.PostAsync($"{appHost.BlazorBaseUrl}/api/user/sync");
        var content = await response.TextAsync();
        if (response.Status != (int)HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"Expected browser user sync to return OK. Status={response.Status}. Body={content}");
        }

        using var payload = JsonDocument.Parse(content);
        if (TryGetGuidProperty(payload.RootElement, "id", out var userId) ||
            TryGetGuidProperty(payload.RootElement, "Id", out userId))
        {
            return userId;
        }

        throw new InvalidOperationException($"Browser user sync response did not include a user id. Body={content}");
    }

    private async Task InvalidateApiAdminCacheAsync(IPage page, Guid userId)
    {
        var response = await page.Context.APIRequest.PostAsync(
            $"{appHost.BlazorBaseUrl}/api/_internal/admin-cache/users/{userId:D}/invalidate");
        var content = await response.TextAsync();
        if (response.Status != (int)HttpStatusCode.NoContent)
        {
            throw new InvalidOperationException(
                $"Expected admin cache invalidation to return NoContent. Status={response.Status}. Body={content}");
        }
    }

    private async Task<ApiCurrentUserSnapshot> SnapshotApiCurrentUserAsync(IPage page)
    {
        var response = await page.Context.APIRequest.PostAsync(
            $"{appHost.BlazorBaseUrl}/api/_internal/admin-cache/current-user/snapshot");
        var content = await response.TextAsync();
        if (response.Status != (int)HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"Expected API current-user snapshot to return OK. Status={response.Status}. Body={content}");
        }

        return JsonSerializer.Deserialize<ApiCurrentUserSnapshot>(content, JsonOptions) ??
            throw new InvalidOperationException($"API current-user snapshot could not be deserialized. Body={content}");
    }

    private async Task OpenWebhookSettingsAsync(IPage page)
    {
        await page.GotoAsync($"{appHost.BlazorBaseUrl}/admin/instance/settings?section=webhooks", new PageGotoOptions
        {
            Timeout = BrowserTimeoutMilliseconds,
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions
        {
            NameRegex = new Regex("^(Administration|Instance Administration)$")
        }).WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });

        await WaitForWebhookPanelAsync(page);
    }

    private async Task AssertAdminAuthorityAsync(
        IPage page,
        WebhookManagementScenarioSeed.Result seed,
        IReadOnlyCollection<string> adminProviderSubjects,
        Guid syncedBrowserUserId,
        ApiCurrentUserSnapshot currentUserSnapshot)
    {
        var response = await page.Context.APIRequest.GetAsync($"{appHost.BlazorBaseUrl}/api/user/admin-authority");
        var content = await response.TextAsync();
        if (response.Status != (int)HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"Expected admin-authority to return OK. Status={response.Status}. Body={content}");
        }

        using var payload = JsonDocument.Parse(content);
        var isInstanceAdmin = payload.RootElement.TryGetProperty("isInstanceAdmin", out var value)
            && value.ValueKind == JsonValueKind.True
            && value.GetBoolean();

        if (!isInstanceAdmin)
        {
            throw new InvalidOperationException(
                "Expected admin-authority to report instance admin. " +
                $"SeedAdminUserId={seed.AdminUserId}. " +
                $"SyncedBrowserUserId={syncedBrowserUserId}. " +
                $"ApiCurrentUserSnapshot={currentUserSnapshot}. " +
                $"SeedProviderSubjects=[{string.Join(", ", adminProviderSubjects)}]. " +
                $"Body={content}");
        }
    }

    private async Task AssertDirectAdminAuthorityAsync(
        string accessToken,
        WebhookManagementScenarioSeed.Result seed,
        IReadOnlyCollection<string> adminProviderSubjects)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.GetAsync($"{appHost.ApiBaseUrl}/api/user/admin-authority");
        var content = await response.Content.ReadAsStringAsync();
        var tokenClaims = ResolveJwtClaimValues(
            accessToken,
            ["sub", "sid", "internal_user_id", "preferred_username", "email"]);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                "Expected direct API admin-authority to return OK. " +
                $"Status={(int)response.StatusCode}. " +
                $"SeedAdminUserId={seed.AdminUserId}. " +
                $"SeedProviderSubjects=[{string.Join(", ", adminProviderSubjects)}]. " +
                $"AccessTokenClaims={tokenClaims}. " +
                $"Body={content}");
        }

        using var payload = JsonDocument.Parse(content);
        var isInstanceAdmin = payload.RootElement.TryGetProperty("isInstanceAdmin", out var value)
            && value.ValueKind == JsonValueKind.True
            && value.GetBoolean();

        if (!isInstanceAdmin)
        {
            throw new InvalidOperationException(
                "Expected direct API admin-authority to report instance admin. " +
                $"SeedAdminUserId={seed.AdminUserId}. " +
                $"SeedProviderSubjects=[{string.Join(", ", adminProviderSubjects)}]. " +
                $"AccessTokenClaims={tokenClaims}. " +
                $"Body={content}");
        }
    }

    private static async Task AssertInitialWebhookSurfaceAsync(IPage page, string dryRunEndpointUrl)
    {
        await page.GetByRole(AriaRole.Cell, new PageGetByRoleOptions { NameString = "Operations local bridge" })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByRole(AriaRole.Cell, new PageGetByRoleOptions { NameString = "Enterprise Svix bridge" })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByRole(AriaRole.Cell, new PageGetByRoleOptions { NameString = "DryRun verification bridge" })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { NameString = "Open provider portal" })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });

        var pageText = await page.Locator("body").InnerTextAsync();
        await Assert.That(pageText).DoesNotContain("secrets/webhooks/e2e-local-v1");

        await page.GetByRole(AriaRole.Tab, new PageGetByRoleOptions { NameString = "Endpoints" })
            .ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });
        var dryRunEndpointRow = EndpointRow(page, dryRunEndpointUrl);
        await dryRunEndpointRow.WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
        await Assert.That(await dryRunEndpointRow.InnerTextAsync()).Contains("DryRun");
        await Assert.That(await dryRunEndpointRow.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { NameString = "Send test webhook" }).CountAsync()).IsEqualTo(0);
    }

    private static async Task CreateLocalEndpointAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { NameString = "Create Endpoint" })
            .ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });

        await page.GetByLabel("Endpoint URL")
            .FillAsync("https://hooks.example.test/islamu-created", new LocatorFillOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByLabel("Signing secret reference")
            .FillAsync("secrets/webhooks/e2e-created-v1", new LocatorFillOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { NameString = "Save" })
            .ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });

        await page.GetByText("Webhook endpoint created.", new PageGetByTextOptions { Exact = true })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByText("https://hooks.example.test/islamu-created", new PageGetByTextOptions { Exact = true })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });

        var pageText = await page.Locator("body").InnerTextAsync();
        await Assert.That(pageText).DoesNotContain("secrets/webhooks/e2e-created-v1");
    }

    private static async Task RotateExistingEndpointSecretAsync(IPage page, string endpointUrl)
    {
        var endpointRow = EndpointRow(page, endpointUrl);
        await endpointRow.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { NameString = "Rotate signing secret" })
            .ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });

        await page.GetByLabel("New secret reference")
            .FillAsync("secrets/webhooks/e2e-local-v2", new LocatorFillOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { NameString = "Rotate", Exact = true })
            .ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });

        await page.GetByText("Webhook endpoint secret rotated.", new PageGetByTextOptions { Exact = true })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });

        var pageText = await page.Locator("body").InnerTextAsync();
        await Assert.That(pageText).DoesNotContain("secrets/webhooks/e2e-local-v1");
        await Assert.That(pageText).DoesNotContain("secrets/webhooks/e2e-local-v2");
    }

    private static async Task TestExistingEndpointAsync(IPage page, string endpointUrl)
    {
        await page.GetByRole(AriaRole.Tab, new PageGetByRoleOptions { NameString = "Endpoints" })
            .ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });

        await EndpointRow(page, endpointUrl)
            .GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { NameString = "Send test webhook" })
            .ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });

        await page.GetByText("Webhook endpoint test scheduled.", new PageGetByTextOptions { Exact = true })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });

        await page.GetByRole(AriaRole.Tab, new PageGetByRoleOptions { NameString = "Deliveries" })
            .ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByLabel("Deliveries")
            .GetByText("webhook.test", new LocatorGetByTextOptions { Exact = true })
            .First
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
    }

    private async Task RetrySeededFailedAttemptAsync(IPage page, Guid failedAttemptId)
    {
        await page.GetByRole(AriaRole.Tab, new PageGetByRoleOptions { NameString = "Deliveries" })
            .ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });

        var deliveries = page.GetByLabel("Deliveries");
        await deliveries.GetByText("http_non_success", new LocatorGetByTextOptions { Exact = true })
            .First
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });

        Guid failedMessageId;
        Guid failedEndpointId;
        await using (var context = appHost.CreateDbContext())
        {
            var attempts = context.WebhookDeliveryAttempts.IgnoreQueryFilters();
            var failedAttempt = attempts.Single(attempt => attempt.Id == failedAttemptId);
            failedMessageId = failedAttempt.MessageId;
            failedEndpointId = failedAttempt.EndpointId;
        }

        var retryButton = page.GetByTestId($"webhook-retry-attempt-{failedAttemptId:D}").First;
        await retryButton.ScrollIntoViewIfNeededAsync(new LocatorScrollIntoViewIfNeededOptions
        {
            Timeout = BrowserTimeoutMilliseconds
        });
        await retryButton.WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });

        var response = await page.Context.APIRequest.PostAsync(
            $"{appHost.BlazorBaseUrl}/api/webhooks/delivery-attempts/{failedAttemptId:D}/retry");
        var content = await response.TextAsync();
        if (response.Status != (int)HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"Expected webhook manual retry to return OK. Status={response.Status}. Body={content}");
        }

        using var payload = JsonDocument.Parse(content);
        if (!payload.RootElement.TryGetProperty("success", out var successProperty) ||
            successProperty.ValueKind != JsonValueKind.True ||
            !successProperty.GetBoolean())
        {
            throw new InvalidOperationException($"Webhook manual retry response was not successful. Body={content}");
        }

        if (!TryGetGuidProperty(payload.RootElement, "id", out var retryAttemptId) &&
            !TryGetGuidProperty(payload.RootElement, "Id", out retryAttemptId))
        {
            throw new InvalidOperationException($"Webhook manual retry response did not include an id. Body={content}");
        }

        await WaitForManualRetryAttemptAsync(retryAttemptId, failedMessageId, failedEndpointId);
    }

    private async Task WaitForManualRetryAttemptAsync(Guid retryAttemptId, Guid failedMessageId, Guid failedEndpointId)
    {
        var deadline = DateTimeOffset.UtcNow.AddMilliseconds(BrowserTimeoutMilliseconds);
        while (DateTimeOffset.UtcNow < deadline)
        {
            await using var context = appHost.CreateDbContext();
            var retryAttempt = context.WebhookDeliveryAttempts.IgnoreQueryFilters().FirstOrDefault(attempt =>
                attempt.Id == retryAttemptId &&
                attempt.MessageId == failedMessageId &&
                attempt.EndpointId == failedEndpointId);
            if (retryAttempt is not null)
            {
                return;
            }

            await Task.Delay(250);
        }

        Assert.Fail("Expected manual retry to create a persisted webhook delivery attempt.");
    }

    private async Task AssertDryRunEndpointHasNoDeliveryAttemptsAsync(Guid dryRunEndpointId)
    {
        await using var context = appHost.CreateDbContext();
        var attemptCount = context.WebhookDeliveryAttempts
            .IgnoreQueryFilters()
            .Count(attempt => attempt.EndpointId == dryRunEndpointId);
        await Assert.That(attemptCount).IsEqualTo(0);
    }

    private async Task OpenSvixProviderPortalAsync(IPage page, Guid svixConsumerId)
    {
        await page.SetViewportSizeAsync(1280, 900);
        await PrepareForVisualCaptureAsync(page);
        await WaitForWebhookPanelAsync(page);

        await page.GetByRole(AriaRole.Tab, new PageGetByRoleOptions { NameString = "Consumers" })
            .ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });

        var button = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { NameString = "Open provider portal" }).First;
        await button.WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });

        var response = await page.Context.APIRequest.PostAsync(
            $"{appHost.BlazorBaseUrl}/api/webhooks/svix/app-portal",
            new APIRequestContextOptions
            {
                Timeout = BrowserTimeoutMilliseconds,
                DataObject = new
                {
                    consumerId = svixConsumerId,
                    readOnly = false,
                    expiresInSeconds = 3600
                }
            });
        var content = await response.TextAsync();
        if (response.Status != (int)HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"Expected Svix app portal access to return OK. Status={response.Status}. Body={content}");
        }

        using var payload = JsonDocument.Parse(content);
        if (!TryGetStringProperty(payload.RootElement, "url", out var portalUrl) &&
            !TryGetStringProperty(payload.RootElement, "Url", out portalUrl))
        {
            throw new InvalidOperationException($"Svix app portal response did not include a URL. Body={content}");
        }

        await Assert.That(portalUrl).Contains("app-portal");
    }

    private static async Task CaptureResponsiveScreenshotsAsync(IPage page)
    {
        await CaptureViewportAsync(page, "desktop", 1280, 900);
        await CaptureViewportAsync(page, "tablet", 768, 1024);
        await CaptureViewportAsync(page, "mobile", 375, 900);
    }

    private static async Task CaptureViewportAsync(IPage page, string name, int width, int height)
    {
        await page.SetViewportSizeAsync(width, height);
        await PrepareForVisualCaptureAsync(page);
        await WaitForWebhookPanelAsync(page);

        var hasHorizontalOverflow = await page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");
        await Assert.That(hasHorizontalOverflow).IsFalse();

        var screenshotDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "TestResults",
            "playwright-artifacts",
            "webhooks");
        Directory.CreateDirectory(screenshotDirectory);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(screenshotDirectory, $"webhook-management-{name}-{width}x{height}.png"),
            FullPage = true,
            Animations = ScreenshotAnimations.Disabled
        });
    }

    private static async Task PrepareForVisualCaptureAsync(IPage page)
    {
        await page.WaitForTimeoutAsync(300);
        await CloseShellSidebarIfOpenAsync(page);
        await DismissSnackbarsAsync(page);
        await page.WaitForTimeoutAsync(300);
    }

    private static async Task CloseShellSidebarIfOpenAsync(IPage page)
    {
        var sidebarToggle = page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { NameString = "Toggle sidebar navigation" }).First;

        if (await sidebarToggle.CountAsync() == 0)
        {
            return;
        }

        var isExpanded = string.Equals(
            await sidebarToggle.GetAttributeAsync("aria-expanded"),
            "true",
            StringComparison.OrdinalIgnoreCase);
        if (!isExpanded)
        {
            return;
        }

        await sidebarToggle.ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.WaitForTimeoutAsync(150);
    }

    private static async Task DismissSnackbarsAsync(IPage page)
    {
        await page.EvaluateAsync(
            "() => document.querySelectorAll('.mud-snackbar button').forEach(button => button.click())");

        try
        {
            await page.Locator(".mud-snackbar").First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Hidden,
                Timeout = 5_000
            });
        }
        catch (PlaywrightException)
        {
            await page.WaitForTimeoutAsync(250);
        }
    }

    private static async Task WaitForWebhookPanelAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { NameString = "Webhooks" })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { NameString = "Create Endpoint" })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
    }

    private static ILocator EndpointRow(IPage page, string endpointUrl) =>
        page.GetByRole(AriaRole.Row)
            .Filter(new LocatorFilterOptions { HasTextString = endpointUrl })
            .First;

    private static IReadOnlyCollection<string> ResolveJwtProviderSubjects(string token)
    {
        using var document = ReadJwtPayload(token);
        return new[] { "sub", "sid" }
            .Select(claim => document.RootElement.TryGetProperty(claim, out var value) ? value.GetString() : null)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static Guid? ResolveJwtCurrentUserId(string token)
    {
        using var document = ReadJwtPayload(token);
        foreach (var claim in new[] { "internal_user_id", "sub", "sid" })
        {
            if (document.RootElement.TryGetProperty(claim, out var value) &&
                Guid.TryParse(value.GetString(), out var userId))
            {
                return userId;
            }
        }

        return null;
    }

    private static string ResolveJwtClaimValues(string token, IReadOnlyCollection<string> claimNames)
    {
        using var document = ReadJwtPayload(token);
        return claimNames
            .Select(claim => document.RootElement.TryGetProperty(claim, out var value) ? $"{claim}={value.GetString()}" : null)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .DefaultIfEmpty("(none)")
            .Aggregate((left, right) => $"{left}, {right}");
    }

    private static JsonDocument ReadJwtPayload(string token)
    {
        var parts = token.Split('.');
        if (parts.Length < 2)
        {
            throw new InvalidOperationException("Token is not a JWT.");
        }

        var payload = parts[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

        return JsonDocument.Parse(Convert.FromBase64String(payload));
    }

    private static bool TryGetGuidProperty(JsonElement element, string propertyName, out Guid value)
    {
        value = default;
        return element.TryGetProperty(propertyName, out var property) &&
            Guid.TryParse(property.GetString(), out value);
    }

    private static bool TryGetStringProperty(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        var stringValue = property.GetString();
        if (string.IsNullOrWhiteSpace(stringValue))
        {
            return false;
        }

        value = stringValue;
        return true;
    }

    private sealed record ApiCurrentUserSnapshot(
        string? AuthenticationType,
        string? InternalUserIdClaim,
        string? SubjectClaim,
        string? SessionIdClaim,
        string? NameIdentifierClaim,
        string? Provider,
        string? ProviderId,
        Guid? ResolvedUserId);
}
