using System.Net;
using Event.Api.IntegrationTests.Fixtures;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

/// <summary>
/// Integration tests for all lookup table controllers.
/// Lookup tables are read-only reference data with no tenant filtering.
/// All GET endpoints should be publicly accessible (AllowAnonymous).
/// </summary>
[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class LookupTableControllerTests
{
    private readonly ApiTestFixture _fixture;

    public LookupTableControllerTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    #region EventType Controller

    [Test]
    public async Task EventType_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/eventtype");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task EventType_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/eventtype/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region EventStatus Controller

    [Test]
    public async Task EventStatus_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/eventstatus");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task EventStatus_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/eventstatus/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region EventFormat Controller

    [Test]
    public async Task EventFormat_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/eventformat");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task EventFormat_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/eventformat/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region VisibilityType Controller

    [Test]
    public async Task VisibilityType_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/visibilitytype");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task VisibilityType_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/visibilitytype/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region RegistrationMode Controller

    [Test]
    public async Task RegistrationMode_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/registrationmode");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task RegistrationMode_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/registrationmode/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region ApprovalStatus Controller

    [Test]
    public async Task ApprovalStatus_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/approvalstatus");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task ApprovalStatus_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/approvalstatus/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region AudienceAge Controller

    [Test]
    public async Task AudienceAge_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/audienceage");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task AudienceAge_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/audienceage/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region AudienceGender Controller

    [Test]
    public async Task AudienceGender_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/audiencegender");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task AudienceGender_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/audiencegender/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region Madhab Controller

    [Test]
    public async Task Madhab_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/madhab");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Madhab_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/madhab/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region Language Controller

    [Test]
    public async Task Language_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/language");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Language_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/language/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region OrganizationRole Controller

    [Test]
    public async Task OrganizationRole_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/organizationrole");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task OrganizationRole_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/organizationrole/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region OrganizationPosition Controller

    [Test]
    public async Task OrganizationPosition_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/organizationposition");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task OrganizationPosition_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/organizationposition/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region ActorType Controller

    [Test]
    public async Task ActorType_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/actortype");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task ActorType_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/actortype/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region DidCustodyType Controller

    [Test]
    public async Task DidCustodyType_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/didcustodytype");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task DidCustodyType_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/didcustodytype/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region FileType Controller

    [Test]
    public async Task FileType_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/filetype");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task FileType_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/filetype/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region TagType Controller

    [Test]
    public async Task TagType_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/tagtype");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task TagType_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/tagtype/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region UserRole Controller

    [Test]
    public async Task UserRole_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/userrole");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task UserRole_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/userrole/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion
}
