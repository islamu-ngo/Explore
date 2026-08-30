// ABOUTME: Restores nullable quota metadata semantics for immutable command-response OpenAPI schemas.
// ABOUTME: Keeps the shared quota component browser-safe while preserving a canonical component reference.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Explore.API.OpenApi;

internal sealed class QuotaExceededDetailsOpenApiTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (document.Components?.Schemas is null)
        {
            return Task.CompletedTask;
        }

        foreach (IOpenApiSchema schema in document.Components.Schemas.Values)
        {
            if (schema.Properties?.ContainsKey("quotaExceeded") != true)
            {
                continue;
            }

            schema.Properties["quotaExceeded"] = new OpenApiSchema
            {
                OneOf =
                [
                    new OpenApiSchema { Type = JsonSchemaType.Null },
                    new OpenApiSchemaReference(nameof(Explore.Application.Responses.QuotaExceededDetails), document)
                ]
            };
        }

        return Task.CompletedTask;
    }
}
