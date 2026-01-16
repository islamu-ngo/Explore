using System.Net;
using System.Net.Http.Json;
using Event.Api.IntegrationTests.Fixtures;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

/// <summary>
/// Integration tests for link table controllers.
/// These are many-to-many relationship controllers that connect entities.
/// </summary>
[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class LinkTableControllerTests
{
    private readonly ApiTestFixture _fixture;

    public LinkTableControllerTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    #region EventCategories Controller

    [Test]
    public async Task EventCategories_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/eventcategories");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task EventCategories_GetById_WithRandomId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync($"/api/v1/eventcategories/{1}");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task EventCategories_Create_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/v1/eventcategories", new { EventId = Guid.NewGuid(), CategoryId = Guid.NewGuid() });
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task EventCategories_Delete_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.DeleteAsync($"/api/v1/eventcategories/{1}");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region EventTags Controller

    [Test]
    public async Task EventTags_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/eventtags");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task EventTags_GetById_WithRandomId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync($"/api/v1/eventtags/{1}");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task EventTags_Create_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/v1/eventtags", new { EventId = Guid.NewGuid(), TagId = Guid.NewGuid() });
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task EventTags_Delete_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.DeleteAsync($"/api/v1/eventtags/{1}");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region EventSessionLanguage Controller

    [Test]
    public async Task EventSessionLanguage_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/eventsessionlanguage");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task EventSessionLanguage_GetById_WithRandomId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync($"/api/v1/eventsessionlanguage/{1}");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task EventSessionLanguage_Create_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/v1/eventsessionlanguage", new { EventSessionId = Guid.NewGuid(), LanguageId = 1 });
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task EventSessionLanguage_Delete_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.DeleteAsync($"/api/v1/eventsessionlanguage/{1}");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region EventSessionSpeaker Controller

    [Test]
    public async Task EventSessionSpeaker_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/eventsessionspeaker");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task EventSessionSpeaker_GetById_WithRandomId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync($"/api/v1/eventsessionspeaker/{1}");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task EventSessionSpeaker_GetBySession_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync($"/api/v1/eventsessionspeaker/by-session/{Guid.NewGuid()}");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task EventSessionSpeaker_Create_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/v1/eventsessionspeaker", new { ActorId = Guid.NewGuid(), EventSessionId = Guid.NewGuid() });
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task EventSessionSpeaker_Delete_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.DeleteAsync($"/api/v1/eventsessionspeaker/{1}");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region EventSessionAgendaItem Controller

    [Test]
    public async Task EventSessionAgendaItem_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/eventsessionagendaitem");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task EventSessionAgendaItem_GetById_WithRandomId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync($"/api/v1/eventsessionagendaitem/{Guid.NewGuid()}");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task EventSessionAgendaItem_GetBySession_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync($"/api/v1/eventsessionagendaitem/by-session/{Guid.NewGuid()}");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task EventSessionAgendaItem_Create_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/v1/eventsessionagendaitem", new
        {
            EventSessionId = Guid.NewGuid(),
            Title = "Test Agenda Item",
            StartTime = DateTimeOffset.UtcNow.AddDays(1),
            EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(1)
        });
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task EventSessionAgendaItem_Delete_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.DeleteAsync($"/api/v1/eventsessionagendaitem/{Guid.NewGuid()}");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region TagTypeTags Controller

    [Test]
    public async Task TagTypeTags_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/tagtypetags");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task TagTypeTags_GetById_WithRandomId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync($"/api/v1/tagtypetags/{1}");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task TagTypeTags_Create_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/v1/tagtypetags", new { TagId = Guid.NewGuid(), TagTypeId = 1 });
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task TagTypeTags_Delete_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.DeleteAsync($"/api/v1/tagtypetags/{1}");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region OrganizationMember Controller

    [Test]
    public async Task OrganizationMember_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/organizationmember");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task OrganizationMember_GetById_WithRandomId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync($"/api/v1/organizationmember/{1}");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task OrganizationMember_GetByOrganization_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync($"/api/v1/organizationmember/organization/{Guid.NewGuid()}");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task OrganizationMember_Create_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/v1/organizationmember", new
        {
            OrganizationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            OrganizationRoleId = 1
        });
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task OrganizationMember_Delete_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.DeleteAsync($"/api/v1/organizationmember/{1}");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region OrganizationReview Controller

    [Test]
    public async Task OrganizationReview_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/organizationreview");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task OrganizationReview_GetById_WithRandomId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync($"/api/v1/organizationreview/{Guid.NewGuid()}");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task OrganizationReview_Create_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/v1/organizationreview", new
        {
            OrganizationId = Guid.NewGuid(),
            Rating = 5,
            Comment = "Great organization!"
        });
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task OrganizationReview_Delete_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.DeleteAsync($"/api/v1/organizationreview/{Guid.NewGuid()}");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region EventRegistration Controller

    [Test]
    public async Task EventRegistration_GetAll_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/eventregistration");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task EventRegistration_GetById_WithRandomId_ShouldNotReturnServerError()
    {
        var response = await _fixture.Client.GetAsync($"/api/v1/eventregistration/{Guid.NewGuid()}");
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task EventRegistration_Create_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/v1/eventregistration", new
        {
            EventSessionId = Guid.NewGuid(),
            UserId = Guid.NewGuid()
        });
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task EventRegistration_Delete_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.DeleteAsync($"/api/v1/eventregistration/{Guid.NewGuid()}");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    #endregion
}
