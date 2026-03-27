// ABOUTME: Configures API versioning using both media type and URL segment strategies.
// ABOUTME: Clients can use Accept header (application/json;v=0.1) or URL path (/api/v0.1/actor).

using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Explore.API.Extensions;

/// <summary>
/// Configures API versioning using combined media type and URL segment readers.
/// Clients specify the desired version either:
///   - Accept header parameter: Accept: application/json;v=0.1
///   - URL path segment: /api/v0.1/actor
///
/// When no version is specified, defaults to the latest (current) version.
/// URL versioning is applied automatically to all controllers via <see cref="VersionedRouteConvention"/>
/// — no modifications to individual controller files are required.
/// </summary>
public static class ApiVersioningExtensions
{
    public static IServiceCollection AddApiMediaTypeVersioning(this IServiceCollection services)
    {
        var mediaTypeReader = new MediaTypeApiVersionReaderBuilder()
            .Parameter("v")
            .Include("application/json")
            .Include("application/hal+json")
            .Build();

        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(0, 1);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                mediaTypeReader,
                new UrlSegmentApiVersionReader());
        })
        .AddMvc()
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // Apply versioned route convention to all controllers automatically
        services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
        {
            options.Conventions.Add(new VersionedRouteConvention());
        });

        return services;
    }
}

/// <summary>
/// Application model convention that adds versioned URL routes to all API controller actions.
/// For each action endpoint under an <c>api/</c> controller route, creates an additional
/// absolute route with the <c>/api/v{version:apiVersion}/</c> prefix.
/// The versioned routes use <c>Name = null</c> to avoid collisions with the original named
/// routes used for HATEOAS link generation.
/// </summary>
internal sealed class VersionedRouteConvention : IApplicationModelConvention
{
    private const string VersionedRoutePrefix = "api/v{version:apiVersion}";

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            var controllerRoute = controller.Selectors
                .FirstOrDefault(s =>
                    s.AttributeRouteModel?.Template != null
                    && s.AttributeRouteModel.Template.StartsWith("api/", StringComparison.OrdinalIgnoreCase)
                    && !s.AttributeRouteModel.Template.Contains("{version:apiVersion}", StringComparison.OrdinalIgnoreCase))
                ?.AttributeRouteModel?.Template;

            if (controllerRoute is null)
                continue;

            // "api/actor" → "actor"
            var controllerSuffix = controllerRoute["api/".Length..];

            foreach (var action in controller.Actions)
            {
                foreach (var selector in action.Selectors.ToList())
                {
                    var actionTemplate = selector.AttributeRouteModel?.Template;

                    // Build full versioned path: api/v{version}/actor or api/v{version}/actor/{id}
                    var versionedPath = string.IsNullOrEmpty(actionTemplate)
                        ? $"{VersionedRoutePrefix}/{controllerSuffix}"
                        : $"{VersionedRoutePrefix}/{controllerSuffix}/{actionTemplate}";

                    // Absolute route (leading /) prevents MVC from combining with
                    // the controller-level [Route("api/...")] template
                    action.Selectors.Add(new SelectorModel(selector)
                    {
                        AttributeRouteModel = new AttributeRouteModel
                        {
                            Template = $"/{versionedPath}",
                            Name = null,
                            Order = (selector.AttributeRouteModel?.Order ?? 0) + 1
                        }
                    });
                }
            }
        }
    }
}
