// ABOUTME: Browser E2E coverage for the single-tenant webhook management surface.
// ABOUTME: Exercises LocalProvider UI actions, HAL affordances, and responsive screenshot evidence.

using System.Text.Json;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.E2ETests.Fixtures;
using Explore.Blazor.Client.E2ETests.Seeds;

namespace Explore.Blazor.Client.E2ETests.Flows.Webhooks;

[Category(E2ETestCategories.E2E)]
[ClassDataSource<AppHostFixture, PlaywrightFixture>(Shared = [SharedType.PerClass, SharedType.PerTestSession])]
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
        var adminTokens = await appHost.GetTestAdminTokensAsync();
        var userAccessToken = await appHost.GetTestUserAccessTokenAsync();
        var adminProviderSubjects = ResolveJwtProviderSubjects(adminTokens.IdToken)
            .Concat(ResolveJwtProviderSubjects(adminTokens.AccessToken))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var adminApi = appHost.CreateApiClient(adminTokens.AccessToken);
        var seed = await WebhookManagementScenarioSeed.SeedAsync(adminApi);

        await AssertDirectAdminAuthorityAsync(adminApi, adminTokens.AccessToken, seed, adminProviderSubjects);

        var page = await playwright.CreatePageAsync(nameof(InstanceAdminWebhooks_ManagesLocalProviderAndCapturesResponsiveScreenshots));
        try
        {
            await BffCookieAuthHelper.LoginAsTestAdminAsync(page, appHost, "/auth/status");
            var syncedBrowserUserId = await SyncBrowserUserAsync(page);
            var currentUserSnapshot = await SnapshotApiCurrentUserAsync(page);
            var apiResolvedUserId = currentUserSnapshot.ResolvedUserId ??
                throw new InvalidOperationException(
                    $"API current-user snapshot did not resolve a user id. Snapshot={currentUserSnapshot}");
            await InvalidateApiAdminCacheAsync(page, syncedBrowserUserId);
            if (apiResolvedUserId != syncedBrowserUserId)
            {
                await InvalidateApiAdminCacheAsync(page, apiResolvedUserId);
            }

            await AssertAdminAuthorityAsync(page, seed, adminProviderSubjects, syncedBrowserUserId, currentUserSnapshot);
            await OpenWebhookSettingsAsync(page);

            await AssertOwnerPanelAsync(page, "Instance", "Operations local bridge", expectSensitiveTabs: false);
            await AssertInitialWebhookSurfaceAsync(page, seed.IdleEndpointUrl);
            await CreateLocalEndpointAsync(page);
            await RotateExistingEndpointSecretAsync(page, seed.ExistingEndpointUrl);
            await ResumeExistingEndpointAsync(page, seed.ExistingEndpointUrl);
            await RetrySeededFailedAttemptAsync(page, adminApi, seed.FailedAttemptId);
            await TestExistingEndpointAsync(page, "https://created-hooks.example.test/islamu-created");
            await AssertIdleEndpointHasNoDeliveryAttemptsAsync(adminApi, seed.IdleEndpointId);
            await CaptureResponsiveScreenshotsAsync(page, "instance-allowed", "Instance");

            await WebhookManagementScenarioSeed.EnableMultiTenantRoutingAsync(adminApi);
            var typedOwnership = await WebhookManagementScenarioSeed.SeedTypedOwnershipAsync(
                adminApi,
                tenantSlug => appHost.CreateApiClient(adminTokens.AccessToken, tenantSlug),
                tenantSlug => appHost.CreateApiClient(userAccessToken, tenantSlug),
                ResolveJwtProviderSubject(userAccessToken));
            await RunTypedOwnershipBrowserEvidenceAsync(
                seed,
                typedOwnership);
        }
        finally
        {
            await playwright.ClosePageAsync(
                page,
                nameof(InstanceAdminWebhooks_ManagesLocalProviderAndCapturesResponsiveScreenshots));
        }
    }

    private async Task<Guid> SyncBrowserUserAsync(IPage page, string? bffBaseUrl = null)
    {
        bffBaseUrl ??= appHost.BlazorBaseUrl;
        var response = await BffCookieAuthHelper.PostWithAntiforgeryAsync(
            page,
            $"{bffBaseUrl}/api/user/sync");
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
        var response = await BffCookieAuthHelper.PostWithAntiforgeryAsync(
            page,
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
        var response = await BffCookieAuthHelper.PostWithAntiforgeryAsync(
            page,
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

    private async Task RunTypedOwnershipBrowserEvidenceAsync(
        WebhookManagementScenarioSeed.Result instanceSeed,
        WebhookManagementScenarioSeed.TypedOwnershipResult seed)
    {
        var page = await playwright.CreatePageAsync(nameof(RunTypedOwnershipBrowserEvidenceAsync));
        try
        {
            var tenantBaseUrl = $"{appHost.BlazorBaseUrl}/t/{seed.TenantSlug}";
            await BffCookieAuthHelper.LoginAsTestUserAsync(
                page,
                appHost,
                $"/t/{seed.TenantSlug}/auth/status");
            var browserUserId = await SyncBrowserUserAsync(page, tenantBaseUrl);
            await Assert.That(browserUserId).IsEqualTo(seed.UserId);

            await AssertTypedOwnerAuthorityAsync(page, seed, tenantBaseUrl);
            await AssertTypedOwnerApiBoundariesAsync(page, instanceSeed, seed, tenantBaseUrl);

            await OpenTenantWebhookSettingsAsync(page, tenantBaseUrl);
            await AssertOwnerPanelAsync(page, "Tenant", seed.TenantConsumerName, expectSensitiveTabs: true);
            await CaptureResponsiveScreenshotsAsync(page, "tenant-allowed", "Tenant");

            await OpenOrganizationWebhookSettingsAsync(page, tenantBaseUrl, seed.OrganizationId);
            await AssertOwnerPanelAsync(
                page,
                "Organization",
                seed.OrganizationConsumerName,
                expectSensitiveTabs: false);
            await CaptureResponsiveScreenshotsAsync(page, "organization-allowed", "Organization");

            await OpenGroupWebhookSettingsAsync(page, tenantBaseUrl, seed.GroupId);
            await AssertOwnerPanelAsync(page, "Group", seed.GroupConsumerName, expectSensitiveTabs: false);
            await CaptureResponsiveScreenshotsAsync(page, "group-allowed", "Group");

            await OpenUserWebhookSettingsAsync(page, tenantBaseUrl);
            await AssertOwnerPanelAsync(page, "User", seed.UserConsumerName, expectSensitiveTabs: false);
            await CaptureResponsiveScreenshotsAsync(page, "user-allowed", "User");

            await BffCookieAuthHelper.AssertBrowserStorageDoesNotContainTokensAsync(page, appHost.BlazorBaseUrl);
        }
        finally
        {
            await playwright.ClosePageAsync(page, nameof(RunTypedOwnershipBrowserEvidenceAsync));
        }
    }

    private async Task AssertTypedOwnerAuthorityAsync(
        IPage page,
        WebhookManagementScenarioSeed.TypedOwnershipResult seed,
        string tenantBaseUrl)
    {
        var response = await page.Context.APIRequest.GetAsync($"{tenantBaseUrl}/api/user/admin-authority");
        var content = await response.TextAsync();
        if (response.Status != (int)HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"Expected scoped admin-authority to return OK. Status={response.Status}. Body={content}");
        }

        using var payload = JsonDocument.Parse(content);
        var root = payload.RootElement;
        var isTenantAdmin = ContainsGuid(root, "adminTenantIds", seed.TenantId);
        var isOrganizationAdmin = ContainsGuid(root, "adminOrganizationIds", seed.OrganizationId);
        var isInstanceAdmin = root.TryGetProperty("isInstanceAdmin", out var instanceAdmin)
            && instanceAdmin.ValueKind == JsonValueKind.True;

        await Assert.That(isTenantAdmin).IsTrue();
        await Assert.That(isOrganizationAdmin).IsTrue();
        await Assert.That(isInstanceAdmin).IsFalse();
    }

    private async Task AssertTypedOwnerApiBoundariesAsync(
        IPage page,
        WebhookManagementScenarioSeed.Result instanceSeed,
        WebhookManagementScenarioSeed.TypedOwnershipResult seed,
        string tenantBaseUrl)
    {
        await AssertOwnerRequestStatusAsync(
            page,
            tenantBaseUrl,
            WebhookManagementScenarioSeed.TenantOwnerKindId,
            ownerId: null,
            HttpStatusCode.OK);
        await AssertOwnerRequestStatusAsync(
            page,
            tenantBaseUrl,
            WebhookManagementScenarioSeed.OrganizationOwnerKindId,
            seed.OrganizationId,
            HttpStatusCode.OK);
        await AssertOwnerRequestStatusAsync(
            page,
            tenantBaseUrl,
            WebhookManagementScenarioSeed.GroupOwnerKindId,
            seed.GroupId,
            HttpStatusCode.OK);
        await AssertOwnerRequestStatusAsync(
            page,
            tenantBaseUrl,
            WebhookManagementScenarioSeed.UserOwnerKindId,
            ownerId: null,
            HttpStatusCode.OK);

        await AssertOwnerRequestStatusAsync(
            page,
            tenantBaseUrl,
            WebhookManagementScenarioSeed.InstanceOwnerKindId,
            ownerId: null,
            HttpStatusCode.Forbidden);
        await AssertOwnerRequestStatusAsync(
            page,
            tenantBaseUrl,
            WebhookManagementScenarioSeed.OrganizationOwnerKindId,
            seed.UnrelatedOrganizationId,
            HttpStatusCode.Forbidden);
        await AssertOwnerRequestStatusAsync(
            page,
            tenantBaseUrl,
            WebhookManagementScenarioSeed.GroupOwnerKindId,
            seed.UnrelatedGroupId,
            HttpStatusCode.Forbidden);
        await AssertOwnerRequestStatusAsync(
            page,
            tenantBaseUrl,
            WebhookManagementScenarioSeed.UserOwnerKindId,
            instanceSeed.AdminUserId,
            HttpStatusCode.Forbidden);
    }

    private async Task AssertOwnerRequestStatusAsync(
        IPage page,
        string tenantBaseUrl,
        int ownerKindId,
        Guid? ownerId,
        HttpStatusCode expectedStatus)
    {
        var ownerIdQuery = ownerId.HasValue ? $"&ownerId={ownerId.Value:D}" : string.Empty;
        var response = await page.Context.APIRequest.GetAsync(
            $"{tenantBaseUrl}/api/webhooks/consumers?ownerKindId={ownerKindId}{ownerIdQuery}&limit=20");
        var content = await response.TextAsync();
        if (response.Status != (int)expectedStatus)
        {
            throw new InvalidOperationException(
                "Unexpected typed webhook owner response. " +
                $"OwnerKindId={ownerKindId}. OwnerId={ownerId}. " +
                $"Expected={(int)expectedStatus}. Actual={response.Status}. Body={content}");
        }
    }

    private async Task OpenTenantWebhookSettingsAsync(IPage page, string tenantBaseUrl)
    {
        await OpenSettingsPageAsync(page, tenantBaseUrl, "/admin/tenant/settings", "Tenant Administration");
        await SelectWebhookSettingsSectionAsync(page);
        await WaitForWebhookPanelAsync(page, "Tenant");
        await Assertions.Expect(page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { NameString = "Save Tenant Settings" }))
            .ToHaveCountAsync(0);
    }

    private async Task OpenOrganizationWebhookSettingsAsync(
        IPage page,
        string tenantBaseUrl,
        Guid organizationId)
    {
        await OpenSettingsPageAsync(page, tenantBaseUrl, $"/admin/organization/{organizationId:D}/settings");
        await SelectWebhookSettingsSectionAsync(page);
        await WaitForWebhookPanelAsync(page, "Organization");
    }

    private async Task OpenGroupWebhookSettingsAsync(IPage page, string tenantBaseUrl, Guid groupId)
    {
        await OpenSettingsPageAsync(page, tenantBaseUrl, $"/admin/group/{groupId:D}/settings");
        await SelectWebhookSettingsSectionAsync(page);
        await WaitForWebhookPanelAsync(page, "Group");
    }

    private async Task OpenUserWebhookSettingsAsync(IPage page, string tenantBaseUrl)
    {
        await OpenSettingsPageAsync(page, tenantBaseUrl, "/settings?section=webhooks", "Account Settings");
        await WaitForWebhookPanelAsync(page, "User");
    }

    private static async Task OpenSettingsPageAsync(
        IPage page,
        string tenantBaseUrl,
        string relativePath,
        string? heading = null)
    {
        await page.SetViewportSizeAsync(1280, 900);
        await page.GotoAsync($"{tenantBaseUrl}{relativePath}", new PageGotoOptions
        {
            Timeout = BrowserTimeoutMilliseconds,
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await page.Locator("[data-blazor-interactive='true']")
            .WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Attached,
                Timeout = BrowserTimeoutMilliseconds
            });

        if (!string.IsNullOrWhiteSpace(heading))
        {
            await page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { NameString = heading })
                .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
        }
    }

    private static async Task SelectWebhookSettingsSectionAsync(IPage page)
    {
        await page.GetByText("Webhooks", new PageGetByTextOptions { Exact = true })
            .First
            .ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });
    }

    private static async Task AssertOwnerPanelAsync(
        IPage page,
        string ownerDisplayName,
        string consumerName,
        bool expectSensitiveTabs)
    {
        await WaitForWebhookPanelAsync(page, ownerDisplayName);
        await page.GetByText(consumerName, new PageGetByTextOptions { Exact = true })
            .First
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });

        var providerTabCount = await page.GetByRole(
            AriaRole.Tab,
            new PageGetByRoleOptions { NameString = "Provider", Exact = true }).CountAsync();
        var replayTabCount = await page.GetByRole(
            AriaRole.Tab,
            new PageGetByRoleOptions { NameString = "Replay", Exact = true }).CountAsync();
        var expectedCount = expectSensitiveTabs ? 1 : 0;

        await Assert.That(providerTabCount).IsEqualTo(expectedCount);
        await Assert.That(replayTabCount).IsEqualTo(expectedCount);
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

    private static async Task AssertDirectAdminAuthorityAsync(
        IEventApiClient api,
        string accessToken,
        WebhookManagementScenarioSeed.Result seed,
        IReadOnlyCollection<string> adminProviderSubjects)
    {
        var authority = await api.GetCurrentUserAdminAuthorityAsync();
        var tokenClaims = ResolveJwtClaimValues(
            accessToken,
            ["sub", "sid", "internal_user_id", "preferred_username", "email"]);
        if (authority.IsInstanceAdmin != true)
        {
            throw new InvalidOperationException(
                "Expected direct API admin-authority to report instance admin. " +
                $"SeedAdminUserId={seed.AdminUserId}. " +
                $"SeedProviderSubjects=[{string.Join(", ", adminProviderSubjects)}]. " +
                $"AccessTokenClaims={tokenClaims}.");
        }
    }

    private static async Task AssertInitialWebhookSurfaceAsync(IPage page, string idleEndpointUrl)
    {
        await page.GetByRole(AriaRole.Cell, new PageGetByRoleOptions { NameString = "Operations local bridge" })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByRole(AriaRole.Cell, new PageGetByRoleOptions { NameString = "DryRun verification bridge" })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });

        var dryRunConsumerRow = page.GetByRole(AriaRole.Row)
            .Filter(new LocatorFilterOptions { HasTextString = "DryRun verification bridge" });
        await Assert.That(await dryRunConsumerRow.InnerTextAsync()).Contains("DryRun");

        var pageText = await page.Locator("body").InnerTextAsync();
        await Assert.That(pageText).DoesNotContain("secrets/webhooks/e2e-local-v1");

        await page.GetByRole(AriaRole.Tab, new PageGetByRoleOptions { NameString = "Endpoints" })
            .ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });
        var idleEndpointRow = EndpointRow(page, idleEndpointUrl);
        await idleEndpointRow.WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
        await Assert.That(await idleEndpointRow.InnerTextAsync()).Contains("Local");
        await Assert.That(await idleEndpointRow.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { NameString = "Send test webhook" }).CountAsync()).IsEqualTo(1);
    }

    private static async Task CreateLocalEndpointAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { NameString = "Create Endpoint" })
            .ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });

        await page.GetByLabel("Endpoint URL")
            .FillAsync("https://created-hooks.example.test/islamu-created", new LocatorFillOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByLabel("Signing secret reference")
            .FillAsync("secrets/webhooks/e2e-created-v1", new LocatorFillOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { NameString = "Save" })
            .ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });

        await page.GetByText("Webhook endpoint created.", new PageGetByTextOptions { Exact = true })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByText("created-hooks.example.test", new PageGetByTextOptions { Exact = true })
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
        await page.GetByTestId("webhook-rotate-preserve-pending")
            .ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByTestId("webhook-rotate-pending-reason")
            .FillAsync(
                "Preserve frozen delivery credentials during the E2E rotation.",
                new LocatorFillOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { NameString = "Rotate", Exact = true })
            .ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });

        await page.GetByText("Webhook endpoint signing credential rotated;", new PageGetByTextOptions { Exact = false })
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

    private static async Task ResumeExistingEndpointAsync(IPage page, string endpointUrl)
    {
        await page.GetByRole(AriaRole.Tab, new PageGetByRoleOptions { NameString = "Endpoints" })
            .ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });

        await EndpointRow(page, endpointUrl)
            .GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { NameString = "Resume webhook endpoint" })
            .ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByTestId("webhook-endpoint-control-reason")
            .FillAsync("e2e_operator_recovery", new LocatorFillOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { NameString = "Resume", Exact = true })
            .ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });

        await page.GetByText("Webhook endpoint resumed.", new PageGetByTextOptions { Exact = true })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
    }

    private static async Task RetrySeededFailedAttemptAsync(
        IPage page,
        IEventApiClient api,
        Guid failedAttemptId)
    {
        await page.GetByRole(AriaRole.Tab, new PageGetByRoleOptions { NameString = "Deliveries" })
            .ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });

        var failedAttempt = await api.GetWebhookDeliveryAttemptByIdAsync(failedAttemptId);
        var failedMessageId = failedAttempt.MessageId
            ?? throw new InvalidOperationException("Failed webhook attempt did not expose its message id.");
        var failedEndpointId = failedAttempt.EndpointId
            ?? throw new InvalidOperationException("Failed webhook attempt did not expose its endpoint id.");

        var ownerAttempts = await api.GetWebhookDeliveryAttemptsAsync(
            ownerKindId: WebhookManagementScenarioSeed.InstanceOwnerKindId,
            limit: 100);
        var ownerAttempt = ownerAttempts._embedded?.Items?
            .SingleOrDefault(candidate => candidate.Id == failedAttemptId)
            ?? throw new InvalidOperationException(
                $"Instance delivery collection omitted seeded attempt {failedAttemptId:D}. " +
                $"Detail outcome={failedAttempt.OutcomeCode}; endpointStatus={failedAttempt.EndpointStatusCode}.");
        if (ownerAttempt._links?.ContainsKey("retry") != true)
        {
            var linkRelations = ownerAttempt._links is { Count: > 0 }
                ? string.Join(",", ownerAttempt._links.Keys.Order(StringComparer.Ordinal))
                : "none";
            throw new InvalidOperationException(
                $"Seeded attempt {failedAttemptId:D} has no HAL retry relation. " +
                $"Outcome={ownerAttempt.OutcomeCode}; endpointStatus={ownerAttempt.EndpointStatusCode}; " +
                $"failureCategory={ownerAttempt.FailureCategory}; links={linkRelations}.");
        }

        var retryButton = page.GetByTestId($"webhook-retry-attempt-{failedAttemptId:D}").First;
        await retryButton.ScrollIntoViewIfNeededAsync(new LocatorScrollIntoViewIfNeededOptions
        {
            Timeout = BrowserTimeoutMilliseconds
        });
        await retryButton.WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });

        await retryButton.ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByText("Webhook delivery retry scheduled.", new PageGetByTextOptions { Exact = true })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
        await WaitForManualRetryAttemptAsync(api, failedAttemptId, failedMessageId, failedEndpointId);
    }

    private static async Task WaitForManualRetryAttemptAsync(
        IEventApiClient api,
        Guid failedAttemptId,
        Guid failedMessageId,
        Guid failedEndpointId)
    {
        var deadline = DateTimeOffset.UtcNow.AddMilliseconds(BrowserTimeoutMilliseconds);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var attempts = await api.GetWebhookDeliveryAttemptsAsync(
                ownerKindId: WebhookManagementScenarioSeed.InstanceOwnerKindId,
                messageId: failedMessageId,
                endpointId: failedEndpointId,
                limit: 100);
            if (attempts._embedded?.Items?.Any(candidate => candidate.Id != failedAttemptId) == true)
            {
                return;
            }

            await Task.Delay(250);
        }

        Assert.Fail("Expected manual retry to create a persisted webhook delivery attempt.");
    }

    private static async Task AssertIdleEndpointHasNoDeliveryAttemptsAsync(
        IEventApiClient api,
        Guid idleEndpointId)
    {
        var attempts = await api.GetWebhookDeliveryAttemptsAsync(
            ownerKindId: WebhookManagementScenarioSeed.InstanceOwnerKindId,
            endpointId: idleEndpointId,
            limit: 100);
        var attemptCount = attempts._embedded?.Items?.Count ?? 0;
        await Assert.That(attemptCount).IsEqualTo(0);
    }

    private static async Task CaptureResponsiveScreenshotsAsync(
        IPage page,
        string artifactPrefix,
        string ownerDisplayName)
    {
        await CaptureViewportAsync(page, artifactPrefix, ownerDisplayName, "desktop", 1280, 900);
        await CaptureViewportAsync(page, artifactPrefix, ownerDisplayName, "tablet", 768, 1024);
        await CaptureViewportAsync(page, artifactPrefix, ownerDisplayName, "mobile", 375, 900);
    }

    private static async Task CaptureViewportAsync(
        IPage page,
        string artifactPrefix,
        string ownerDisplayName,
        string viewportName,
        int width,
        int height)
    {
        await page.SetViewportSizeAsync(width, height);
        await PrepareForVisualCaptureAsync(page);
        await WaitForWebhookPanelAsync(page, ownerDisplayName);

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
            Path = Path.Combine(
                screenshotDirectory,
                $"webhook-management-{artifactPrefix}-{viewportName}-{width}x{height}.png"),
            FullPage = true,
            Animations = ScreenshotAnimations.Disabled
        });
    }

    private static async Task PrepareForVisualCaptureAsync(IPage page)
    {
        await page.WaitForTimeoutAsync(300);
        await CloseShellSidebarIfOpenAsync(page);
        await HideSnackbarsAsync(page);
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

    private static async Task HideSnackbarsAsync(IPage page)
    {
        await page.AddStyleTagAsync(new PageAddStyleTagOptions
        {
            Content = ".mud-snackbar-provider, .mud-snackbar { display: none !important; }"
        });
    }

    private static async Task WaitForWebhookPanelAsync(IPage page, string ownerDisplayName = "Instance")
    {
        await page.GetByRole(
                AriaRole.Region,
                new PageGetByRoleOptions { NameString = $"{ownerDisplayName} webhook management" })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { NameString = "Create Endpoint" })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
    }

    private static ILocator EndpointRow(IPage page, string endpointUrl) =>
        page.GetByRole(AriaRole.Row)
            .Filter(new LocatorFilterOptions { HasTextString = new Uri(endpointUrl, UriKind.Absolute).Host })
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

    private static string ResolveJwtProviderSubject(string token) =>
        ResolveJwtProviderSubjects(token).FirstOrDefault()
        ?? throw new InvalidOperationException("JWT did not contain a supported provider-subject claim.");

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

    private static bool ContainsGuid(JsonElement root, string propertyName, Guid expected)
    {
        return root.TryGetProperty(propertyName, out var values)
            && values.ValueKind == JsonValueKind.Array
            && values.EnumerateArray().Any(value =>
                value.ValueKind == JsonValueKind.String
                && Guid.TryParse(value.GetString(), out var parsed)
                && parsed == expected);
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
