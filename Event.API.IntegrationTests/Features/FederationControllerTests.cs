using System.Net;
using System.Net.Http.Json;
using Event.Api.IntegrationTests.Fixtures;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

/// <summary>
/// Integration tests for federation-related controllers.
/// These manage ATProto/ActivityPub entities (Actor keys, indexed DIDs, sync state, records).
/// </summary>
[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class FederationControllerTests
{
    private readonly ApiTestFixture _fixture;

    public FederationControllerTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    #region ActorKeyStore Controller

    [Test]
    public async Task ActorKeyStore_GetAll_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/actorkeystore");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ActorKeyStore_GetById_WithRandomId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync($"/api/v1/actorkeystore/{Guid.NewGuid()}");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task ActorKeyStore_Create_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/v1/actorkeystore", new
        {
            ActorId = Guid.NewGuid(),
            KeyPurpose = "signing",
            PrivateKeyEncrypted = "encrypted-key",
            PublicKey = "public-key",
            IsActive = true
        });
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ActorKeyStore_Delete_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.DeleteAsync($"/api/v1/actorkeystore/{Guid.NewGuid()}");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region IndexedDid Controller

    [Test]
    public async Task IndexedDid_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/indexeddid");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task IndexedDid_GetById_WithValidDid_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/indexeddid/did:plc:test");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region SyncState Controller

    [Test]
    public async Task SyncState_GetAll_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/syncstate");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task SyncState_GetById_WithRandomId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync($"/api/v1/syncstate/{1}");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region AtprotoRecord Controller

    [Test]
    public async Task AtprotoRecord_GetAll_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/atprotorecord");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task AtprotoRecord_GetById_WithRandomId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync($"/api/v1/atprotorecord/{Guid.NewGuid()}");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task AtprotoRecord_Create_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/v1/atprotorecord", new
        {
            Did = "did:plc:test",
            Collection = "app.bsky.feed.post",
            RecordKey = "test-key"
        });
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task AtprotoRecord_Delete_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.DeleteAsync($"/api/v1/atprotorecord/{Guid.NewGuid()}");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region UserAuthenticationToken Controller

    [Test]
    public async Task UserAuthenticationToken_GetAll_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/userauthenticationtoken");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UserAuthenticationToken_GetById_WithRandomId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync($"/api/v1/userauthenticationtoken/{Guid.NewGuid()}");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region UserExternalLogin Controller

    [Test]
    public async Task UserExternalLogin_GetAll_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/userexternallogin");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UserExternalLogin_GetById_WithRandomId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync($"/api/v1/userexternallogin/{Guid.NewGuid()}");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion
}
