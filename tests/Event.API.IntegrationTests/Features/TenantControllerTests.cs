using System.Net;
using System.Net.Http.Json;
using Event.Api.IntegrationTests.Fixtures;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

/// <summary>
/// Integration tests for multi-tenancy controllers.
/// These manage tenant configuration and user-tenant relationships.
/// </summary>
[NotInParallel("ApiTestFixture")]
[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerClass)]
public class TenantControllerTests
{
    private readonly ApiTestFixture _fixture;

    public TenantControllerTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    #region Tenant Controller

    [Test]
    public async Task Tenant_GetAll_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.GetAsync("/api/tenant");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Tenant_GetById_WithRandomId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync($"/api/tenant/{Guid.NewGuid()}");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task Tenant_Create_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/tenant", new
        {
            FullName = "Test Tenant",
            Slug = "test-tenant",
            IsActive = true
        });
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region TenantUserRoleGrant Controller

    [Test]
    public async Task TenantUserRoleGrant_GetAll_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.GetAsync("/api/tenant-user-role-grants");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task TenantUserRoleGrant_GetById_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.GetAsync($"/api/tenant-user-role-grants/{Guid.NewGuid()}");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task TenantUserRoleGrant_Create_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/tenant-user-role-grants", new
        {
            TenantUserId = Guid.NewGuid(),
            RoleId = 1
        });
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    #endregion

    // TenantSettings region removed — TenantSettingsController was deleted in the settings refactor.
    // Settings are now managed via InstanceSettingsController at api/instance/settings.
}
