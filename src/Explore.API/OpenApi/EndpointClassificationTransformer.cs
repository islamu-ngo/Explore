// ABOUTME: OpenAPI operation transformer that emits endpoint posture vendor extensions.
// ABOUTME: Projects classification, rate-limit, cache, and tenant-mode metadata from endpoint attributes.

using System.Threading;
using System.Threading.Tasks;
using Explore.API.Attributes;
using Explore.API.Filters;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;

namespace Explore.API.OpenApi;

public sealed class EndpointClassificationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var endpointMetadata = context.Description.ActionDescriptor.EndpointMetadata;
        var classification = endpointMetadata.OfType<EndpointClassificationAttribute>().LastOrDefault();
        var rateLimit = endpointMetadata.OfType<EnableRateLimitingAttribute>().LastOrDefault();
        var outputCache = endpointMetadata.OfType<OutputCacheAttribute>().LastOrDefault();
        var requiresMultiTenant = endpointMetadata.OfType<RequireMultiTenantAttribute>().Any();

        if (classification is null
            && rateLimit is null
            && outputCache is null
            && !requiresMultiTenant)
        {
            return Task.CompletedTask;
        }

        operation.Extensions ??= new Dictionary<string, IOpenApiExtension>();

        if (classification is not null)
        {
            operation.Extensions["x-endpoint-class"] =
                new JsonNodeExtension(classification.Class.ToString());
        }

        if (!string.IsNullOrWhiteSpace(rateLimit?.PolicyName))
        {
            operation.Extensions["x-rate-limit-policy"] =
                new JsonNodeExtension(rateLimit.PolicyName);
        }

        if (!string.IsNullOrWhiteSpace(outputCache?.PolicyName))
        {
            operation.Extensions["x-output-cache-policy"] =
                new JsonNodeExtension(outputCache.PolicyName);
        }

        if (requiresMultiTenant)
        {
            operation.Extensions["x-tenant-mode"] =
                new JsonNodeExtension("multi-tenant-required");
        }

        return Task.CompletedTask;
    }
}
