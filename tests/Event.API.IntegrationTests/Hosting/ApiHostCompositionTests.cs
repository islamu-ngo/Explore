// ABOUTME: Characterizes the reusable API host boundary and its factory-hosted liveness surface.
// ABOUTME: Protects public composition modules while preserving Program-based integration hosting.

using System.Net;
using System.Reflection;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Hosting;

public sealed class ApiHostCompositionTests
{
    [Test]
    public async Task ApiHostModules_ExposeReusablePublicCompositionSeams()
    {
        var apiAssembly = typeof(Program).Assembly;
        var serviceModule = apiAssembly.GetType(
            "Explore.API.Hosting.ApiHostServiceCollectionExtensions",
            throwOnError: false);
        var startupModule = apiAssembly.GetType(
            "Explore.API.Hosting.ApiHostStartupExtensions",
            throwOnError: false);
        var applicationModule = apiAssembly.GetType(
            "Explore.API.Hosting.ApiHostApplicationExtensions",
            throwOnError: false);

        await Assert.That(serviceModule).IsNotNull();
        await Assert.That(startupModule).IsNotNull();
        await Assert.That(applicationModule).IsNotNull();
        await Assert.That(serviceModule!.IsPublic).IsTrue();
        await Assert.That(startupModule!.IsPublic).IsTrue();
        await Assert.That(applicationModule!.IsPublic).IsTrue();
        await Assert.That(HasPublicStaticMethod(serviceModule, "AddApiHostServices")).IsTrue();
        await Assert.That(HasPublicStaticMethod(startupModule, "RunApiHostStartupAsync")).IsTrue();
        await Assert.That(HasPublicStaticMethod(applicationModule, "UseApiHostMiddleware")).IsTrue();
        await Assert.That(HasPublicStaticMethod(applicationModule, "MapApiHostEndpoints")).IsTrue();
        await Assert.That(
            startupModule.GetField("GracefulShutdownSeconds", BindingFlags.Public | BindingFlags.Static)?
                .GetRawConstantValue()).IsEqualTo(25);
    }

    [Test]
    public async Task ProgramFactory_MapsBoundedAliveHealthResponse()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/alive");
        var content = await response.Content.ReadAsByteArrayAsync();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(content.Length).IsGreaterThan(0);
        await Assert.That(content.Length).IsLessThanOrEqualTo(4096);
        using var _ = JsonDocument.Parse(content);
    }

    private static bool HasPublicStaticMethod(Type type, string methodName) =>
        type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static) is not null;
}
