// ABOUTME: Resolves the API base address for browser-BFF hosts from config and Aspire service discovery.
// ABOUTME: Keeps API proxy resolution shared without depending on a specific host project.

using Microsoft.Extensions.Configuration;

namespace Event.Web.BffHosting.Proxy;

public static class EventApiBaseAddressResolver
{
    public static string Resolve(IConfiguration configuration)
    {
        var explicitUrl = configuration["ExploreApi:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(explicitUrl))
        {
            return NormalizeBaseUrl(explicitUrl);
        }

        var aspireHttps = GetAspireApiReference(configuration, "https");
        if (!string.IsNullOrWhiteSpace(aspireHttps))
        {
            return NormalizeBaseUrl(aspireHttps);
        }

        var aspireHttp = GetAspireApiReference(configuration, "http");
        if (!string.IsNullOrWhiteSpace(aspireHttp))
        {
            return NormalizeBaseUrl(aspireHttp);
        }

        return "https://localhost:7039/";
    }

    private static string NormalizeBaseUrl(string url) => url.EndsWith('/') ? url : url + "/";

    private static string? GetAspireApiReference(IConfiguration configuration, string scheme) =>
        configuration[$"services:explore-api:{scheme}:0"]
        ?? configuration[$"services__explore-api__{scheme}__0"];
}
