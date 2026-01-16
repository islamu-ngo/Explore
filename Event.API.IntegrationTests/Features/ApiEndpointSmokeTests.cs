using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Event.Api.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class ApiEndpointSmokeTests
{
    private readonly ApiTestFixture _fixture;

    public ApiEndpointSmokeTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task Public_Get_Endpoints_ReturnOk()
    {
        var endpoints = GetApiDescriptions()
            .Where(description => IsHttpMethod(description, HttpMethod.Get))
            .Where(description => !IsProtected(description))
            .Where(description => !IsExternalDependencyEndpoint(description));

        foreach (var description in endpoints)
        {
            var path = BuildPath(description);
            Console.WriteLine($"Testing endpoint: {path}");
            var response = await _fixture.Client.GetAsync(path);

            var isSuccess = response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent;
            await Assert.That(isSuccess).IsTrue();
        }
    }

    [Test]
    public async Task Protected_Endpoints_ReturnUnauthorized_Or_Forbidden()
    {
        var endpoints = GetApiDescriptions()
            .Where(description => IsProtected(description));

        foreach (var description in endpoints)
        {
            var path = BuildPath(description);
            var request = new HttpRequestMessage(new HttpMethod(description.HttpMethod ?? HttpMethod.Get.Method), path)
            {
                Content = BuildBody(description)
            };

            var response = await _fixture.Client.SendAsync(request);
            var isUnauthorized = response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;

            await Assert.That(isUnauthorized)
                .IsTrue();
        }
    }

    [Test]
    public async Task Public_Write_Endpoints_DoNotReturnUnauthorized_Or_ServerError()
    {
        var endpoints = GetApiDescriptions()
            .Where(description => !IsProtected(description))
            .Where(description => !IsHttpMethod(description, HttpMethod.Get))
            .Where(description => !IsExternalDependencyEndpoint(description));

        foreach (var description in endpoints)
        {
            var path = BuildPath(description);
            var request = new HttpRequestMessage(new HttpMethod(description.HttpMethod ?? HttpMethod.Post.Method), path)
            {
                Content = BuildBody(description)
            };

            var response = await _fixture.Client.SendAsync(request);
            var isUnauthorized = response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
            var isServerError = (int)response.StatusCode >= 500;

            await Assert.That(isUnauthorized)
                .IsFalse();
            await Assert.That(isServerError)
                .IsFalse();
        }
    }

    private IReadOnlyList<ApiDescription> GetApiDescriptions()
    {
        var provider = _fixture.Factory.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>();
        return provider.ApiDescriptionGroups.Items.SelectMany(group => group.Items).ToList();
    }

    private static bool IsProtected(ApiDescription description)
    {
        var metadata = description.ActionDescriptor.EndpointMetadata;
        var hasAllowAnonymous = metadata.OfType<IAllowAnonymous>().Any();
        var hasAuthorize = metadata.OfType<IAuthorizeData>().Any();

        return hasAuthorize && !hasAllowAnonymous;
    }

    private static bool IsHttpMethod(ApiDescription description, HttpMethod method)
    {
        return string.Equals(description.HttpMethod, method.Method, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExternalDependencyEndpoint(ApiDescription description)
    {
        var path = description.RelativePath ?? string.Empty;
        return path.Contains("storageobject/file/", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildPath(ApiDescription description)
    {
        var relativePath = description.RelativePath ?? string.Empty;
        var path = "/" + relativePath.TrimStart('/');

        foreach (var parameter in description.ParameterDescriptions.Where(p => p.Source == BindingSource.Path))
        {
            var value = GetSampleValue(parameter, description);
            path = ReplaceRouteParameter(path, parameter.Name, value);
        }

        return path;
    }

    private static string ReplaceRouteParameter(string path, string? name, string value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return path;
        }

        var escapedName = Regex.Escape(name);
        var pattern = $@"\{{\*?{escapedName}(?:\:[^}}]+)?\}}";
        return Regex.Replace(path, pattern, value);
    }

    private static string GetSampleValue(ApiParameterDescription parameter, ApiDescription description)
    {
        var type = parameter.Type ?? typeof(string);

        if (description.RelativePath != null &&
            (description.RelativePath.Contains($"*{{{parameter.Name}}}") || description.RelativePath.Contains($"**{{{parameter.Name}}}")))
        {
            return "test/file.txt";
        }

        if (type == typeof(Guid))
        {
            return Guid.NewGuid().ToString();
        }

        if (type == typeof(int) || type == typeof(long) || type == typeof(short))
        {
            return "1";
        }

        if (type == typeof(bool))
        {
            return "true";
        }

        if (type.IsEnum)
        {
            return "1";
        }

        if (string.Equals(parameter.Name, "did", StringComparison.OrdinalIgnoreCase))
        {
            return "did:plc:test";
        }

        return "test";
    }

    private static HttpContent? BuildBody(ApiDescription description)
    {
        var bodyParameter = description.ParameterDescriptions.FirstOrDefault(p => p.Source == BindingSource.Body);
        if (bodyParameter == null)
        {
            return null;
        }

        var type = bodyParameter.Type ?? typeof(object);
        var payload = GetSamplePayload(type);

        return new StringContent(payload, Encoding.UTF8, "application/json");
    }

    private static string GetSamplePayload(Type type)
    {
        if (type == typeof(string))
        {
            return "\"test\"";
        }

        if (type == typeof(Guid))
        {
            return $"\"{Guid.NewGuid()}\"";
        }

        if (type == typeof(int) || type == typeof(long) || type == typeof(short))
        {
            return "1";
        }

        if (type == typeof(bool))
        {
            return "true";
        }

        return "{}";
    }
}
