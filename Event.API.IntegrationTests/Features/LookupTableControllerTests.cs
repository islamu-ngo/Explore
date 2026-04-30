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
        var response = await _fixture.Client.GetAsync("/api/eventtype");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task EventType_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/eventtype/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region EventStatus Controller

    [Test]
    public async Task EventStatus_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/eventstatus");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task EventStatus_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/eventstatus/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region EventFormat Controller

    [Test]
    public async Task EventFormat_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/eventformat");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task EventFormat_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/eventformat/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region VisibilityType Controller

    [Test]
    public async Task VisibilityType_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/visibilitytype");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task VisibilityType_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/visibilitytype/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region RegistrationMode Controller

    [Test]
    public async Task RegistrationMode_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/registrationmode");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task RegistrationMode_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/registrationmode/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region ApprovalStatus Controller

    [Test]
    public async Task ApprovalStatus_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/approvalstatus");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task ApprovalStatus_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/approvalstatus/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region AudienceAge Controller

    [Test]
    public async Task AudienceAge_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/audienceage");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task AudienceAge_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/audienceage/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region AudienceGender Controller

    [Test]
    public async Task AudienceGender_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/audiencegender");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task AudienceGender_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/audiencegender/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region Madhab Controller

    [Test]
    public async Task Madhab_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/madhab");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Madhab_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/madhab/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region Language Controller

    [Test]
    public async Task Language_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/language");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Language_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/language/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region Role Controller (Unified)

    [Test]
    public async Task Role_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/role");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Role_GetByOrganizationScope_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/role?scope=Organization");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Role_GetByEventScope_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/role?scope=Event");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Role_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/role/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region OrganizationPosition Controller

    [Test]
    public async Task OrganizationPosition_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/organizationposition");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task OrganizationPosition_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/organizationposition/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region ActorType Controller

    [Test]
    public async Task ActorType_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/actortype");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task ActorType_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/actortype/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region DidCustodyType Controller

    [Test]
    public async Task DidCustodyType_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/didcustodytype");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task DidCustodyType_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/didcustodytype/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region FileType Controller

    [Test]
    public async Task FileType_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/filetype");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task FileType_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/filetype/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region TagType Controller

    [Test]
    public async Task TagType_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/tagtype");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task TagType_GetById_WithValidId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/tagtype/1");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion
}
