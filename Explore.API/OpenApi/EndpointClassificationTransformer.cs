// ABOUTME: OpenAPI operation transformer that emits x-endpoint-class for every classified action.
// ABOUTME: Reads [EndpointClassification] from endpoint metadata and injects a vendor extension.

using System.Threading;
using System.Threading.Tasks;
using Explore.API.Attributes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Explore.API.OpenApi;

/// <summary>
/// Operation transformer that projects each action's <see cref="EndpointClassificationAttribute"/>
/// into the <c>x-endpoint-class</c> OpenAPI vendor extension. This makes the classification
/// visible to downstream consumers of <c>/openapi/event-api.json</c> (the generated
/// <c>IEventApiClient</c>, the action-inventory markdown, and contract diff tooling).
/// Attribute resolution: <c>ActionDescriptor.EndpointMetadata</c> is populated with both
/// controller-level and action-level attributes, so a single lookup via <c>GetMetadata</c>
/// returns the effective value with action-level winning over controller-level.
/// </summary>
public sealed class EndpointClassificationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var classification = context.Description.ActionDescriptor
            .EndpointMetadata
            .OfType<EndpointClassificationAttribute>()
            .LastOrDefault();

        if (classification is null)
        {
            return Task.CompletedTask;
        }

        operation.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        operation.Extensions["x-endpoint-class"] =
            new JsonNodeExtension(classification.Class.ToString());

        return Task.CompletedTask;
    }
}
