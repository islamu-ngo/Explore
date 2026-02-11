using System.Net;
using Event.Api.IntegrationTests.Fixtures;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class EventsControllerTests
{
    private readonly ApiTestFixture _fixture;

    public EventsControllerTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task GetAll_ShouldReturnOk()
    {
        // Act
        // Route is "api/v1/[controller]" -> "api/v1/Event" (singular based on class name EventController)
        var response = await _fixture.Client.GetAsync("/api/v1/event");

        // Debug: Print content if error
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[TEST DEBUG] Status: {response.StatusCode}, Content: {content}");
        }

        // Assert
        // Note: It might return 401 Unauthorized if AllowAnonymous is not set on the endpoint.
        // Assuming public read access based on conventions "GET = AllowAnonymous".
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetById_WithInvalidId_ShouldReturnNotFound()
    {
        // Act
        var response = await _fixture.Client.GetAsync($"/api/v1/event/{Guid.NewGuid()}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }
}
