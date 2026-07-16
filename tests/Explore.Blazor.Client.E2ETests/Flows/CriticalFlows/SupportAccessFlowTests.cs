// ABOUTME: Browser E2E coverage for instance-admin support-access lifecycle UX.
// ABOUTME: Exercises Keycloak login, BFF antiforgery, HAL affordances, audit evidence, and screenshots.

using System.Text.Json;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.E2ETests.Fixtures;
using Explore.Blazor.Client.E2ETests.Seeds;

namespace Explore.Blazor.Client.E2ETests.Flows.CriticalFlows;

[Category(E2ETestCategories.E2E)]
[ClassDataSource<AppHostFixture, PlaywrightFixture>(Shared = [SharedType.PerClass, SharedType.PerTestSession])]
[NotInParallel("E2EAppHostDb")]
[ParallelLimiter<BrowserParallelLimit>]
public sealed class SupportAccessFlowTests(
    AppHostFixture appHost,
    PlaywrightFixture playwright)
{
    private static readonly string[] ProviderSubjectClaimNames = ["sub", "sid"];
    private static readonly string[] CurrentUserIdClaimNames = ["internal_user_id", "sub", "sid"];
    private static readonly string[] DiagnosticClaimNames =
    [
        "sub",
        "sid",
        "internal_user_id",
        "preferred_username",
        "email"
    ];
    private static readonly float BrowserTimeoutMilliseconds = (float)TimeSpan.FromSeconds(90).TotalMilliseconds;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Test]
    [Timeout(600_000)]
    public async Task InstanceAdminSupportAccess_StartsAuditedSessionStopsAndCapturesResponsiveScreenshots()
    {
        var adminTokens = await appHost.GetTestAdminTokensAsync();
        var adminProviderSubjects = ResolveJwtProviderSubjects(adminTokens.IdToken)
            .Concat(ResolveJwtProviderSubjects(adminTokens.AccessToken))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var adminApi = appHost.CreateApiClient(adminTokens.AccessToken);
        var seed = await SupportAccessScenarioSeed.SeedAsync(appHost, adminApi);

        await AssertDirectAdminAuthorityAsync(adminApi, adminTokens.AccessToken, seed, adminProviderSubjects);

        var page = await playwright.CreatePageAsync(nameof(InstanceAdminSupportAccess_StartsAuditedSessionStopsAndCapturesResponsiveScreenshots));
        try
        {
            await BffCookieAuthHelper.LoginAsTestAdminAsync(page, appHost, TenantPath(seed, "/auth/status"));
            var syncedBrowserUserId = await SyncBrowserUserAsync(page, seed);
            var currentUserSnapshot = await SnapshotApiCurrentUserAsync(page, seed);
            var apiResolvedUserId = currentUserSnapshot.ResolvedUserId ??
                throw new InvalidOperationException(
                    $"API current-user snapshot did not resolve a user id. Snapshot={currentUserSnapshot}");
            await InvalidateApiAdminCacheAsync(page, seed, syncedBrowserUserId);
            if (apiResolvedUserId != syncedBrowserUserId)
            {
                await InvalidateApiAdminCacheAsync(page, seed, apiResolvedUserId);
            }

            await AssertAdminAuthorityAsync(page, seed, adminProviderSubjects, syncedBrowserUserId, currentUserSnapshot);
            await AssertTenantAdminAuthorityAsync(page, seed);
            await OpenSupportAccessConsoleAsync(page, seed);
            await StartSupportAccessAsync(page);

            var activeSessionId = await AssertCurrentSupportSessionAsync(page, seed);
            await AssertSupportAccessAuditAsync(page, activeSessionId);
            await BffCookieAuthHelper.AssertBrowserStorageDoesNotContainTokensAsync(page, appHost.BlazorBaseUrl);
            await CaptureResponsiveScreenshotsAsync(page);

            await StopCurrentSupportAccessAsync(page, seed, activeSessionId);
            await AssertNoCurrentSupportSessionAsync(page, seed);
            await OpenTenantSupportEvidenceAsync(page, seed);
            await AssertTenantSupportEvidenceAsync(page, activeSessionId);
            await CaptureTenantEvidenceResponsiveScreenshotsAsync(page);
            await BffCookieAuthHelper.AssertBrowserStorageDoesNotContainTokensAsync(page, appHost.BlazorBaseUrl);
        }
        finally
        {
            await playwright.ClosePageAsync(
                page,
                nameof(InstanceAdminSupportAccess_StartsAuditedSessionStopsAndCapturesResponsiveScreenshots));
        }
    }

    private string TenantUrl(SupportAccessScenarioSeed.Result seed, string path) =>
        $"{appHost.BlazorBaseUrl}{TenantPath(seed, path)}";

    private static string TenantPath(SupportAccessScenarioSeed.Result seed, string path)
    {
        var normalizedPath = path.StartsWith('/') ? path : "/" + path;
        return normalizedPath;
    }

    private async Task AssertTenantAdminAuthorityAsync(
        IPage page,
        SupportAccessScenarioSeed.Result seed)
    {
        var response = await page.Context.APIRequest.GetAsync(TenantUrl(seed, "/api/user/admin-authority"));
        var content = await response.TextAsync();
        if (response.Status != (int)HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"Expected admin-authority to return OK for tenant-admin proof. Status={response.Status}. Body={content}");
        }

        using var payload = JsonDocument.Parse(content);
        var root = payload.RootElement;
        var isTenantAdmin = root.TryGetProperty("adminTenantIds", out var tenantIds)
            && tenantIds.ValueKind == JsonValueKind.Array
            && tenantIds.EnumerateArray().Any(value => value.ValueKind == JsonValueKind.String
                && Guid.TryParse(value.GetString(), out var tenantId)
                && tenantId == seed.TenantId);

        if (!isTenantAdmin)
        {
            throw new InvalidOperationException(
                "Expected admin-authority to prove tenant-admin authority. " +
                $"ExpectedTenantId={seed.TenantId}. Body={content}");
        }
    }

    private async Task<Guid> SyncBrowserUserAsync(IPage page, SupportAccessScenarioSeed.Result seed)
    {
        var response = await BffCookieAuthHelper.PostWithAntiforgeryAsync(
            page,
            TenantUrl(seed, "/api/user/sync"));
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

    private async Task InvalidateApiAdminCacheAsync(
        IPage page,
        SupportAccessScenarioSeed.Result seed,
        Guid userId)
    {
        var response = await BffCookieAuthHelper.PostWithAntiforgeryAsync(
            page,
            TenantUrl(seed, $"/api/_internal/admin-cache/users/{userId:D}/invalidate"));
        var content = await response.TextAsync();
        if (response.Status != (int)HttpStatusCode.NoContent)
        {
            throw new InvalidOperationException(
                $"Expected admin cache invalidation to return NoContent. Status={response.Status}. Body={content}");
        }
    }

    private async Task<ApiCurrentUserSnapshot> SnapshotApiCurrentUserAsync(
        IPage page,
        SupportAccessScenarioSeed.Result seed)
    {
        var response = await BffCookieAuthHelper.PostWithAntiforgeryAsync(
            page,
            TenantUrl(seed, "/api/_internal/admin-cache/current-user/snapshot"));
        var content = await response.TextAsync();
        if (response.Status != (int)HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"Expected API current-user snapshot to return OK. Status={response.Status}. Body={content}");
        }

        return JsonSerializer.Deserialize<ApiCurrentUserSnapshot>(content, JsonOptions) ??
            throw new InvalidOperationException($"API current-user snapshot could not be deserialized. Body={content}");
    }

    private async Task AssertAdminAuthorityAsync(
        IPage page,
        SupportAccessScenarioSeed.Result seed,
        IReadOnlyCollection<string> adminProviderSubjects,
        Guid syncedBrowserUserId,
        ApiCurrentUserSnapshot currentUserSnapshot)
    {
        var response = await page.Context.APIRequest.GetAsync(TenantUrl(seed, "/api/user/admin-authority"));
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

    private static async Task AssertDirectAdminAuthorityAsync(
        IEventApiClient api,
        string accessToken,
        SupportAccessScenarioSeed.Result seed,
        IReadOnlyCollection<string> adminProviderSubjects)
    {
        var authority = await api.GetCurrentUserAdminAuthorityAsync();
        var tokenClaims = ResolveJwtClaimValues(
            accessToken,
            DiagnosticClaimNames);
        if (authority.IsInstanceAdmin != true)
        {
            throw new InvalidOperationException(
                "Expected direct API admin-authority to report instance admin. " +
                $"SeedAdminUserId={seed.AdminUserId}. " +
                $"SeedProviderSubjects=[{string.Join(", ", adminProviderSubjects)}]. " +
                $"AccessTokenClaims={tokenClaims}.");
        }
    }

    private async Task OpenSupportAccessConsoleAsync(IPage page, SupportAccessScenarioSeed.Result seed)
    {
        await page.GotoAsync(TenantUrl(seed, "/admin/instance/settings"), new PageGotoOptions
        {
            Timeout = BrowserTimeoutMilliseconds,
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions
        {
            NameRegex = new Regex("^(Administration|Instance Administration)$")
        }).WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });

        await ClickSupportAccessSettingsNavItemAsync(page);
        await WaitForSupportAccessConsoleAsync(page);
        await CloseShellSidebarIfOpenAsync(page);
    }

    private static async Task ClickSupportAccessSettingsNavItemAsync(IPage page)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var supportAccessLink = page.Locator(".settings-sidebar .mud-list-item")
                    .Filter(new LocatorFilterOptions { HasTextString = "Support Access" })
                    .First;

                await supportAccessLink.ScrollIntoViewIfNeededAsync(new LocatorScrollIntoViewIfNeededOptions
                {
                    Timeout = BrowserTimeoutMilliseconds
                });
                await supportAccessLink.ClickAsync(new LocatorClickOptions
                {
                    Timeout = BrowserTimeoutMilliseconds,
                    Force = attempt > 1
                });

                if (await TryWaitForSupportAccessConsoleAsync(page, 5_000))
                {
                    return;
                }
            }
            catch (PlaywrightException) when (attempt < maxAttempts)
            {
                await page.WaitForTimeoutAsync(250);
            }
        }

        await WaitForSupportAccessConsoleAsync(page);
    }

    private static async Task<bool> TryWaitForSupportAccessConsoleAsync(IPage page, float timeoutMilliseconds)
    {
        try
        {
            await page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { NameString = "Support Access" })
                .WaitForAsync(new LocatorWaitForOptions { Timeout = timeoutMilliseconds });
            return true;
        }
        catch (Exception ex) when (ex is PlaywrightException or TimeoutException)
        {
            return false;
        }
    }

    private static async Task WaitForSupportAccessConsoleAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { NameString = "Support Access" })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { NameString = "Start Session" })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { NameString = "Start Support Access" })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
    }

    private static async Task StartSupportAccessAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { NameRegex = new Regex("^Reason code\\*?$") })
            .FillAsync("customer_support_e2e", new LocatorFillOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { NameRegex = new Regex("^Ticket reference$") })
            .FillAsync("SUP-E2E-001", new LocatorFillOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { NameRegex = new Regex("^Reason\\*?$") })
            .FillAsync("E2E support window validates audited tenant support access.", new LocatorFillOptions
            {
                Timeout = BrowserTimeoutMilliseconds
            });

        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { NameString = "Start Support Access" })
            .ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });

        await page.GetByText("Current support session", new PageGetByTextOptions { Exact = true })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
        await SupportSessionRow(page, "SUP-E2E-001")
            .GetByText("customer_support_e2e", new LocatorGetByTextOptions { Exact = true })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
    }

    private async Task<Guid> AssertCurrentSupportSessionAsync(
        IPage page,
        SupportAccessScenarioSeed.Result seed)
    {
        var response = await page.Context.APIRequest.GetAsync(TenantUrl(seed, "/bff/support-access/current"));
        var content = await response.TextAsync();
        if (response.Status != (int)HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"Expected BFF current support-access endpoint to return OK. Status={response.Status}. Body={content}");
        }

        using var payload = JsonDocument.Parse(content);
        var root = payload.RootElement;
        await Assert.That(root.GetProperty("isActive").GetBoolean()).IsTrue();
        var session = root.GetProperty("session");
        var sessionId = session.GetProperty("id").GetGuid();
        await Assert.That(sessionId).IsNotEqualTo(Guid.Empty);
        await Assert.That(session.GetProperty("targetTenantId").GetGuid()).IsEqualTo(seed.TenantId);
        await Assert.That(session.GetProperty("modeName").GetString()).IsEqualTo("ReadOnly");
        await Assert.That(session.GetProperty("allowsWrites").GetBoolean()).IsFalse();
        await Assert.That(session.GetProperty("isActive").GetBoolean()).IsTrue();

        return sessionId;
    }

    private static async Task AssertSupportAccessAuditAsync(IPage page, Guid sessionId)
    {
        var shortSessionId = sessionId.ToString("N")[..8];
        var auditButton = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions
        {
            NameRegex = new Regex($"^View audit events for support session {Regex.Escape(shortSessionId)}")
        }).First;

        if (await auditButton.CountAsync() == 0)
        {
            var fallbackAuditButton = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions
            {
                NameRegex = new Regex("^View audit events for support session")
            }).First;
            await fallbackAuditButton.ScrollIntoViewIfNeededAsync(new LocatorScrollIntoViewIfNeededOptions
            {
                Timeout = BrowserTimeoutMilliseconds
            });
            await fallbackAuditButton.ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });
        }
        else
        {
            await auditButton.ScrollIntoViewIfNeededAsync(new LocatorScrollIntoViewIfNeededOptions
            {
                Timeout = BrowserTimeoutMilliseconds
            });
            await auditButton.ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });
        }

        await page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { NameString = "Audit Evidence" })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByText("Started", new PageGetByTextOptions { Exact = true })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
    }

    private async Task OpenTenantSupportEvidenceAsync(IPage page, SupportAccessScenarioSeed.Result seed)
    {
        await page.SetViewportSizeAsync(1280, 900);
        await page.GotoAsync(TenantUrl(seed, "/admin/instance/settings"), new PageGotoOptions
        {
            Timeout = BrowserTimeoutMilliseconds,
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions
        {
            NameRegex = new Regex("^(Administration|Instance Administration|Tenant Administration)$")
        }).WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });

        await ClickTenantSupportEvidenceNavItemAsync(page);
        await WaitForTenantSupportEvidenceAsync(page);
        await CloseShellSidebarIfOpenAsync(page);
    }

    private static async Task ClickTenantSupportEvidenceNavItemAsync(IPage page)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var supportEvidenceLink = page.Locator(".settings-sidebar .mud-list-item")
                    .Filter(new LocatorFilterOptions { HasTextString = "Support Evidence" })
                    .First;

                await supportEvidenceLink.ScrollIntoViewIfNeededAsync(new LocatorScrollIntoViewIfNeededOptions
                {
                    Timeout = BrowserTimeoutMilliseconds
                });
                await supportEvidenceLink.ClickAsync(new LocatorClickOptions
                {
                    Timeout = BrowserTimeoutMilliseconds,
                    Force = attempt > 1
                });

                if (await TryWaitForTenantSupportEvidenceAsync(page, 5_000))
                {
                    return;
                }
            }
            catch (PlaywrightException) when (attempt < maxAttempts)
            {
                await page.WaitForTimeoutAsync(250);
            }
        }

        await WaitForTenantSupportEvidenceAsync(page);
    }

    private static async Task<bool> TryWaitForTenantSupportEvidenceAsync(IPage page, float timeoutMilliseconds)
    {
        try
        {
            await page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { NameString = "Support Access Evidence" })
                .WaitForAsync(new LocatorWaitForOptions { Timeout = timeoutMilliseconds });
            return true;
        }
        catch (Exception ex) when (ex is PlaywrightException or TimeoutException)
        {
            return false;
        }
    }

    private static async Task WaitForTenantSupportEvidenceAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { NameString = "Support Access Evidence" })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { NameString = "Support Sessions" })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
    }

    private static async Task AssertTenantSupportEvidenceAsync(IPage page, Guid sessionId)
    {
        await page.GetByText("SUP-E2E-001", new PageGetByTextOptions { Exact = true })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByText("customer_support_e2e", new PageGetByTextOptions { Exact = true })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByText("Stopped", new PageGetByTextOptions { Exact = true })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });

        var shortSessionId = sessionId.ToString("N")[..8];
        var auditButton = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions
        {
            NameRegex = new Regex($"^View audit events for support session {Regex.Escape(shortSessionId)}")
        }).First;

        if (await auditButton.CountAsync() == 0)
        {
            auditButton = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions
            {
                NameRegex = new Regex("^View audit events for support session")
            }).First;
        }

        await auditButton.ScrollIntoViewIfNeededAsync(new LocatorScrollIntoViewIfNeededOptions
        {
            Timeout = BrowserTimeoutMilliseconds
        });
        await auditButton.ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });

        await page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { NameString = "Audit Evidence" })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByText("Started", new PageGetByTextOptions { Exact = true })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByText("Stopped", new PageGetByTextOptions { Exact = true })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
    }

    private async Task StopCurrentSupportAccessAsync(
        IPage page,
        SupportAccessScenarioSeed.Result seed,
        Guid activeSessionId)
    {
        await page.GetByLabel("Current support access end reason")
            .FillAsync("E2E support verification complete.", new LocatorFillOptions
            {
                Timeout = BrowserTimeoutMilliseconds
            });

        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { NameString = "Stop", Exact = true })
            .ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });

        await SupportSessionRow(page, "SUP-E2E-001")
            .GetByText("Stopped", new LocatorGetByTextOptions { Exact = true })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });

        var response = await page.Context.APIRequest.GetAsync(TenantUrl(
            seed,
            $"/bff/support-access/tenants/{seed.TenantId:D}/sessions/{activeSessionId:D}/audit-events?limit=100"));
        await Assert.That(response.Status).IsEqualTo((int)HttpStatusCode.OK);
        var content = await response.TextAsync();
        await Assert.That(content).Contains("Stopped");
    }

    private async Task AssertNoCurrentSupportSessionAsync(
        IPage page,
        SupportAccessScenarioSeed.Result seed)
    {
        var response = await page.Context.APIRequest.GetAsync(TenantUrl(seed, "/bff/support-access/current"));
        var content = await response.TextAsync();
        if (response.Status != (int)HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"Expected BFF current support-access endpoint to return OK after stop. Status={response.Status}. Body={content}");
        }

        using var payload = JsonDocument.Parse(content);
        await Assert.That(payload.RootElement.GetProperty("isActive").GetBoolean()).IsFalse();
    }

    private static async Task CaptureResponsiveScreenshotsAsync(IPage page)
    {
        await CaptureViewportAsync(page, "desktop", 1280, 900);
        await CaptureViewportAsync(page, "tablet", 768, 1024);
        await CaptureViewportAsync(page, "mobile", 375, 900);
    }

    private static async Task CaptureTenantEvidenceResponsiveScreenshotsAsync(IPage page)
    {
        await CaptureTenantEvidenceViewportAsync(page, "desktop", 1280, 900);
        await CaptureTenantEvidenceViewportAsync(page, "tablet", 768, 1024);
        await CaptureTenantEvidenceViewportAsync(page, "mobile", 375, 900);
    }

    private static async Task CaptureViewportAsync(IPage page, string name, int width, int height)
    {
        await page.SetViewportSizeAsync(width, height);
        await PrepareForVisualCaptureAsync(page);
        await WaitForSupportAccessConsoleAsync(page);

        var hasHorizontalOverflow = await page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");
        await Assert.That(hasHorizontalOverflow).IsFalse();

        var screenshotDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "TestResults",
            "playwright-artifacts",
            "support-access");
        Directory.CreateDirectory(screenshotDirectory);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(screenshotDirectory, $"support-access-console-{name}-{width}x{height}.png"),
            FullPage = true,
            Animations = ScreenshotAnimations.Disabled
        });
    }

    private static async Task CaptureTenantEvidenceViewportAsync(IPage page, string name, int width, int height)
    {
        await page.SetViewportSizeAsync(width, height);
        await PrepareForVisualCaptureAsync(page);
        await WaitForTenantSupportEvidenceAsync(page);

        var hasHorizontalOverflow = await page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");
        await Assert.That(hasHorizontalOverflow).IsFalse();

        var screenshotDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "TestResults",
            "playwright-artifacts",
            "support-access");
        Directory.CreateDirectory(screenshotDirectory);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(screenshotDirectory, $"support-access-tenant-evidence-{name}-{width}x{height}.png"),
            FullPage = true,
            Animations = ScreenshotAnimations.Disabled
        });
    }

    private static async Task PrepareForVisualCaptureAsync(IPage page)
    {
        await page.WaitForTimeoutAsync(300);
        await CloseShellSidebarIfOpenAsync(page);
        await DismissSnackbarsAsync(page);
        await page.EvaluateAsync("() => window.scrollTo(0, 0)");
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

    private static ILocator SupportSessionRow(IPage page, string ticketReference) =>
        page.Locator("tr")
            .Filter(new LocatorFilterOptions { HasTextString = ticketReference })
            .First;

    private static string[] ResolveJwtProviderSubjects(string token)
    {
        using var document = ReadJwtPayload(token);
        return ProviderSubjectClaimNames
            .Select(claim => document.RootElement.TryGetProperty(claim, out var value) ? value.GetString() : null)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static Guid? ResolveJwtCurrentUserId(string token)
    {
        using var document = ReadJwtPayload(token);
        foreach (var claim in CurrentUserIdClaimNames)
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
