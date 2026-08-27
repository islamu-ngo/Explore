// ABOUTME: Shapes the canonical configuration manifest response as one binary OpenAPI download.
// ABOUTME: Removes versioned JSON byte-array variants so NSwag generates a typed file response.

namespace Explore.API.OpenApi;

using Explore.API.Controllers;
using Explore.API.Hateoas;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

public sealed class ConfigurationManifestExportResponseTransformer
    : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(
                operation.OperationId,
                RouteNames.ExportConfigurationManifest,
                StringComparison.Ordinal)
            || operation.Responses is null
            || !operation.Responses.TryGetValue("200", out IOpenApiResponse? response)
            || response.Content is null)
        {
            return Task.CompletedTask;
        }

        response.Content.Clear();
        response.Content[ConfigurationManifestExportApiContract.MediaType] =
            new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Format = "binary"
                }
            };

        return Task.CompletedTask;
    }
}
