using System.Net;
using System.Text.Json;
using TUnit.Assertions;

namespace Event.Api.IntegrationTests.Helpers;

internal static class ProblemDetailsAssertions
{
    public static async Task<JsonDocument> ReadAsJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content);
    }

    public static async Task AssertProblemDetailsAsync(HttpResponseMessage response, HttpStatusCode expectedStatusCode, string expectedTitle)
    {
        await Assert.That(response.StatusCode).IsEqualTo(expectedStatusCode);

        var contentType = response.Content.Headers.ContentType?.MediaType;
        var hasValidContentType = contentType is "application/problem+json" or "application/json";
        await Assert.That(hasValidContentType).IsTrue();

        using var document = await ReadAsJsonAsync(response);
        var root = document.RootElement;

        await Assert.That(root.TryGetProperty("status", out var status)).IsTrue();
        await Assert.That(status.GetInt32()).IsEqualTo((int)expectedStatusCode);

        await Assert.That(root.TryGetProperty("title", out var title)).IsTrue();
        await Assert.That(title.GetString()).IsEqualTo(expectedTitle);

        await Assert.That(root.TryGetProperty("traceId", out _)).IsTrue();
        await Assert.That(root.TryGetProperty("timestamp", out _)).IsTrue();
    }
}
