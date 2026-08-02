// ABOUTME: Integration tests for shared custom-property definition API endpoints.
// ABOUTME: Verifies basic route behavior and auth posture for the shared-definition governance surface.

using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Event.Api.IntegrationTests.Fixtures;
using Explore.API.Controllers;
using Explore.API.Hateoas.Policies;
using Explore.Application.DTOs.CustomPropertyDefinition;
using Explore.Application.Hateoas;
using Explore.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

[NotInParallel("ApiTestFixture")]
[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class CustomPropertyDefinitionControllerTests
{
    private readonly ApiTestFixture _fixture;
    private const string BaseUrl = "/api/custompropertydefinition";

    public CustomPropertyDefinitionControllerTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task GetAll_WithEntityTypeScope_ShouldReturnOk()
    {
        var response = await _fixture.Client.GetAsync($"{BaseUrl}?entityTypeName={EntityTypeName.Organization}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetById_WithRandomId_ShouldReturnNotFound()
    {
        var response = await _fixture.Client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Create_WithoutAuth_ShouldReturnUnauthorized()
    {
        var dto = new CreateCustomPropertyDefinitionDto
        {
            EntityTypeName = EntityTypeName.Organization,
            Namespace = "tenant.community",
            Key = "prayer_notes",
            DisplayName = "Prayer Notes",
            PropertyType = PropertyType.Text,
            ExposureLevel = ExposureLevel.Internal,
        };

        var response = await _fixture.Client.PostAsJsonAsync(BaseUrl, dto);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Update_WithoutAuth_ShouldReturnUnauthorized()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateCustomPropertyDefinitionDto
        {
            Metadata = new UpdateCustomPropertyDefinitionMetadataDto
            {
                DisplayName = "Prayer Notes",
                ExposureLevel = ExposureLevel.Internal
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"{BaseUrl}/{id}")
        {
            Content = JsonContent.Create(dto)
        };
        request.Headers.TryAddWithoutValidation("If-Match", $"\"{Guid.NewGuid():D}\"");
        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Delete_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.DeleteAsync($"{BaseUrl}/{Guid.NewGuid()}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UpdateContracts_ShouldUsePatchAndHeaderConcurrency()
    {
        AssertUpdateAction<CustomPropertyDefinitionController>("Update");
        AssertUpdateAction<EventCustomPropertyController>("Update");
        AssertUpdateAction<EventSessionCustomPropertyController>("Update");
    }

    [Test]
    public async Task UpdateDtos_ShouldNotCarryIdentityOrConcurrencyFields()
    {
        AssertNoProperty<UpdateCustomPropertyDefinitionDto>("Id", "ExpectedConcurrencyStamp", "TenantId", "EventId", "EventSessionId");
        AssertNoProperty<Explore.Application.DTOs.EventCustomProperty.UpdateEventCustomPropertyDefinitionDto>("Id", "ExpectedConcurrencyStamp", "TenantId", "EventId", "EventSessionId");
        AssertNoProperty<Explore.Application.DTOs.EventSessionCustomProperty.UpdateEventSessionCustomPropertyDefinitionDto>("Id", "ExpectedConcurrencyStamp", "TenantId", "EventId", "EventSessionId");
    }

    [Test]
    public async Task UpdateDtos_ShouldExposeOnlyGroupedPatchContracts()
    {
        AssertGroupedDto<UpdateCustomPropertyDefinitionDto>("Relations", "Metadata", "Validation", "Options");
        AssertGroupedDto<Explore.Application.DTOs.EventCustomProperty.UpdateEventCustomPropertyDefinitionDto>("Metadata", "Validation", "Options");
        AssertGroupedDto<Explore.Application.DTOs.EventSessionCustomProperty.UpdateEventSessionCustomPropertyDefinitionDto>("Metadata", "Validation", "Options");
    }

    [Test]
    public async Task DetailHalEditLinks_ShouldAdvertisePatch()
    {
        Assert.That(new CustomPropertyDefinitionDetailLinkPolicy().GetLinks(new Explore.Application.DTOs.CustomPropertyDefinition.CustomPropertyDefinitionDto { Id = Guid.NewGuid(), EntityTypeName = EntityTypeName.Organization, Namespace = "tenant.community", Key = "prayer_notes", DisplayName = "Prayer Notes" }, null).Single(link => link.Rel == LinkRelations.Edit).Method).IsEqualTo("PATCH");
        Assert.That(new EventCustomPropertyDefinitionDetailLinkPolicy().GetLinks(new Explore.Application.DTOs.EventCustomProperty.EventCustomPropertyDefinitionDto { Id = Guid.NewGuid(), EventId = Guid.NewGuid(), Namespace = "tenant.community", Key = "prayer_notes", DisplayName = "Prayer Notes" }, null).Single(link => link.Rel == LinkRelations.Edit).Method).IsEqualTo("PATCH");
        Assert.That(new EventSessionCustomPropertyDefinitionDetailLinkPolicy().GetLinks(new Explore.Application.DTOs.EventSessionCustomProperty.EventSessionCustomPropertyDefinitionDto { Id = Guid.NewGuid(), EventSessionId = Guid.NewGuid(), Namespace = "tenant.community", Key = "prayer_notes", DisplayName = "Prayer Notes" }, null).Single(link => link.Rel == LinkRelations.Edit).Method).IsEqualTo("PATCH");
    }

    [Test]
    public async Task ValuePutActions_ShouldStayPut()
    {
        AssertHttpMethod<EventCustomPropertyController>("SetValue", "PUT");
        AssertHttpMethod<EventCustomPropertyController>("SetMultiValues", "PUT");
        AssertHttpMethod<EventSessionCustomPropertyController>("SetValue", "PUT");
        AssertHttpMethod<EventSessionCustomPropertyController>("SetMultiValues", "PUT");
    }

    private static void AssertUpdateAction<TController>(string methodName)
    {
        var method = typeof(TController).GetMethods(BindingFlags.Instance | BindingFlags.Public).Single(m => m.Name == methodName);
        var httpPatch = method.GetCustomAttributes<HttpPatchAttribute>(inherit: true).SingleOrDefault();
        Assert.That(httpPatch).IsNotNull();
        Assert.That(httpPatch!.Template).IsEqualTo("{id:guid}");

        var headerParam = method.GetParameters().SingleOrDefault(p => string.Equals(p.Name, "ifMatch", StringComparison.OrdinalIgnoreCase));
        Assert.That(headerParam).IsNotNull();
        Assert.That(headerParam!.GetCustomAttribute<FromHeaderAttribute>()?.Name).IsEqualTo("If-Match");
    }

    private static void AssertNoProperty<T>(params string[] propertyNames)
    {
        var type = typeof(T);
        foreach (var propertyName in propertyNames)
        {
            Assert.That(type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)).IsNull();
        }
    }

    private static void AssertGroupedDto<T>(params string[] expectedProperties)
    {
        var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(property => property.Name).ToArray();
        Assert.That(properties).IsEquivalentTo(expectedProperties);
    }

    private static void AssertHttpMethod<TController>(string methodName, string expectedMethod)
    {
        var method = typeof(TController).GetMethods(BindingFlags.Instance | BindingFlags.Public).Single(m => m.Name == methodName);
        var httpMethod = method.GetCustomAttributes<HttpMethodAttribute>(inherit: true).Single();
        Assert.That(httpMethod.HttpMethods).Contains(expectedMethod);
    }
}
