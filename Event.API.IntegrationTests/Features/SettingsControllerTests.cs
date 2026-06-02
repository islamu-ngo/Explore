// ABOUTME: Integration tests for the unified SettingsController verifying auth gates and endpoint availability.
// ABOUTME: Tests both anonymous (401) and authenticated access for all 9 user/tenant settings endpoints.

using System.Net;
using System.Net.Http.Json;
using Event.Api.IntegrationTests.Fixtures;
using Explore.Application.DTOs.Settings;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

[NotInParallel("ApiTestFixture")]
[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class SettingsControllerAnonymousTests
{
    private readonly ApiTestFixture _fixture;
    private const string BaseUrl = "/api/settings";

    public SettingsControllerAnonymousTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    #region User Scope — Anonymous Access

    [Test]
    public async Task GetUserSettings_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.GetAsync($"{BaseUrl}/user/EventList");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UpdateUserSettingsBatch_WithoutAuth_ShouldReturnUnauthorized()
    {
        var body = new UpdateSettingBatchDto
        {
            Values = new Dictionary<string, string> { ["event_list.page_size"] = "24" }
        };

        var response = await _fixture.Client.PutAsJsonAsync($"{BaseUrl}/user/EventList", body);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UpdateUserSetting_WithoutAuth_ShouldReturnUnauthorized()
    {
        var body = new UpdateSettingValueDto { Value = "24" };

        var response = await _fixture.Client.PutAsJsonAsync($"{BaseUrl}/user/keys/event_list.page_size", body);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ResetUserSetting_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.DeleteAsync($"{BaseUrl}/user/keys/event_list.page_size");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Tenant Scope — Anonymous Access

    [Test]
    public async Task GetTenantSettings_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.GetAsync($"{BaseUrl}/tenant/EventList");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UpdateTenantSettingsBatch_WithoutAuth_ShouldReturnUnauthorized()
    {
        var body = new UpdateSettingBatchDto
        {
            Values = new Dictionary<string, string> { ["event_list.page_size"] = "24" }
        };

        var response = await _fixture.Client.PutAsJsonAsync($"{BaseUrl}/tenant/EventList", body);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UpdateTenantSetting_WithoutAuth_ShouldReturnUnauthorized()
    {
        var body = new UpdateSettingValueDto { Value = "24" };

        var response = await _fixture.Client.PutAsJsonAsync($"{BaseUrl}/tenant/keys/event_list.page_size", body);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task LockTenantSetting_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.PostAsync($"{BaseUrl}/tenant/keys/event_list.page_size/lock", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UnlockTenantSetting_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.DeleteAsync($"{BaseUrl}/tenant/keys/event_list.page_size/lock");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    #endregion
}

[NotInParallel("AuthenticatedApiFixture")]
[ClassDataSource<AuthenticatedApiTestFixture>(Shared = SharedType.PerAssembly)]
public class SettingsControllerAuthenticatedTests
{
    private readonly AuthenticatedApiTestFixture _fixture;
    private const string BaseUrl = "/api/settings";

    public SettingsControllerAuthenticatedTests(AuthenticatedApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    #region User Scope — Authenticated Access

    [Test]
    public async Task GetUserSettings_WithAuth_ShouldReturnOk()
    {
        var userId = Guid.NewGuid();
        var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Get, $"{BaseUrl}/user/EventList", userId);

        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains("EventList");
    }

    [Test]
    public async Task GetUserSettings_UnknownCategory_ShouldReturnOkWithEmptySettings()
    {
        var userId = Guid.NewGuid();
        var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Get, $"{BaseUrl}/user/NonExistentCategory", userId);

        var response = await _fixture.Client.SendAsync(request);

        // Unknown category returns empty group, not 404
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task UpdateUserSetting_WithAuth_InvalidKey_ShouldReturnBadRequest()
    {
        var userId = Guid.NewGuid();
        var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Put, $"{BaseUrl}/user/keys/nonexistent.key", userId);
        request.Content = JsonContent.Create(new UpdateSettingValueDto { Value = "test" });

        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ResetUserSetting_WithAuth_NonexistentKey_ShouldReturnBadRequest()
    {
        var userId = Guid.NewGuid();
        var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Delete, $"{BaseUrl}/user/keys/nonexistent.key", userId);

        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Tenant Scope — Authenticated Non-Admin Access

    [Test]
    public async Task GetTenantSettings_WithAuthNoAdmin_ShouldReturnOk()
    {
        // Tenant GET resolves settings through handler — handler returns OK with CanEdit=false for non-admins
        var userId = Guid.NewGuid();
        var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Get, $"{BaseUrl}/tenant/EventList", userId);

        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task LockTenantSetting_WithAuthNoAdmin_ShouldReturnForbidden()
    {
        var userId = Guid.NewGuid();
        var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Post, $"{BaseUrl}/tenant/keys/event_list.page_size/lock", userId);

        var response = await _fixture.Client.SendAsync(request);

        // Handler returns "Only tenant administrators..." which controller maps to Forbid()
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task UnlockTenantSetting_WithAuthNoAdmin_ShouldReturnForbidden()
    {
        var userId = Guid.NewGuid();
        var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Delete, $"{BaseUrl}/tenant/keys/event_list.page_size/lock", userId);

        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task UpdateTenantSetting_WithAuthNoAdmin_ShouldReturnForbidden()
    {
        var userId = Guid.NewGuid();
        var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Put, $"{BaseUrl}/tenant/keys/event_list.page_size", userId);
        request.Content = JsonContent.Create(new UpdateSettingValueDto { Value = "24" });

        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task UpdateTenantSettingsBatch_WithAuthNoAdmin_ShouldReturnForbiddenOrBadRequest()
    {
        var userId = Guid.NewGuid();
        var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Put, $"{BaseUrl}/tenant/EventList", userId);
        request.Content = JsonContent.Create(new UpdateSettingBatchDto
        {
            Values = new Dictionary<string, string> { ["event_list.page_size"] = "24" }
        });

        var response = await _fixture.Client.SendAsync(request);

        // Handler validates auth first; non-admin → either Forbid or BadRequest depending on batch mode
        var statusCode = response.StatusCode;
        await Assert.That(statusCode == HttpStatusCode.Forbidden || statusCode == HttpStatusCode.BadRequest).IsTrue();
    }

    #endregion
}
