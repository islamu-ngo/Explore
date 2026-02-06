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
[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
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
        var response = await _fixture.Client.GetAsync("/api/v1/tenant");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Tenant_GetById_WithRandomId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync($"/api/v1/tenant/{Guid.NewGuid()}");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task Tenant_Create_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/v1/tenant", new
        {
            FullName = "Test Tenant",
            Slug = "test-tenant",
            IsActive = true
        });
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region TenantUser Controller

    [Test]
    public async Task TenantUser_GetAll_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/tenantuser");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task TenantUser_GetById_WithRandomId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync($"/api/v1/tenantuser/{1}");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task TenantUser_Create_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/v1/tenantuser", new
        {
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            UserRoleId = 1
        });
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region TenantSettings Controller

    [Test]
    public async Task TenantSettings_GetAll_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/tenantsettings");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task TenantSettings_GetById_WithRandomId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync($"/api/v1/tenantsettings/{1}");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion
}
