// ABOUTME: Unit-style tests for safe BFF forwarding response translation helpers.
// ABOUTME: Protects generic ProblemDetails and JSON/content passthrough behavior used by preference endpoints.

using System.Net;
using System.Text;
using Explore.Blazor.Services.Preferences;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Explore.Blazor.IntegrationTests.Services;

public sealed class BffForwardingResultsTests
{
    [Test]
    public async Task JsonStreamOrProblemAsync_WithSuccess_StreamsJsonPayload()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"ok\":true}", Encoding.UTF8, "application/json")
        };

        var result = await BffForwardingResults.JsonStreamOrProblemAsync(response, "hidden detail", "Hidden title", CancellationToken.None);
        var context = CreateContext();

        await result.ExecuteAsync(context);

        context.Response.ContentType.Should().Be("application/json");
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();
        body.Should().Be("{\"ok\":true}");
    }

    [Test]
    public async Task JsonStreamOrProblemAsync_WithDisposedResponse_StillWritesJsonPayload()
    {
        IResult result;
        using (var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"ok\":true}", Encoding.UTF8, "application/json")
        })
        {
            result = await BffForwardingResults.JsonStreamOrProblemAsync(
                response,
                "hidden detail",
                "Hidden title",
                CancellationToken.None);
        }

        var context = CreateContext();

        await result.ExecuteAsync(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();
        body.Should().Be("{\"ok\":true}");
    }

    [Test]
    public async Task JsonStreamOrProblemAsync_WithFailure_ReturnsSafeProblemStatus()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadGateway);

        var result = await BffForwardingResults.JsonStreamOrProblemAsync(response, "Safe failure detail.", "Safe failure title", CancellationToken.None);
        var context = CreateContext();

        await result.ExecuteAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
    }

    [Test]
    public async Task ContentOrProblemAsync_WithSuccess_PreservesMediaTypeAndPayload()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/hal+json")
        };

        var result = await BffForwardingResults.ContentOrProblemAsync(response, "Safe failure detail.", "Safe failure title", CancellationToken.None);
        var context = CreateContext();

        await result.ExecuteAsync(context);

        context.Response.ContentType.Should().StartWith("application/hal+json");
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();
        body.Should().Be("[]");
    }

    [Test]
    public async Task OkOrProblem_WithFailure_ReturnsSafeProblemStatus()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.Conflict);

        var result = BffForwardingResults.OkOrProblem(response, "Safe conflict detail.", "Safe conflict title");
        var context = CreateContext();

        await result.ExecuteAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    private static DefaultHttpContext CreateContext()
    {
        return new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider(),
            Response =
            {
                Body = new MemoryStream()
            }
        };
    }
}
