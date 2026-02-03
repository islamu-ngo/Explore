// ABOUTME: Swashbuckle schema filter that properly exposes DTOs from generic HAL wrapper types.
// NOTE: This is for Swashbuckle (AddSwaggerGen). For native ASP.NET Core OpenAPI (AddOpenApi),
// use HalSchemaTransformer instead which implements IOpenApiSchemaTransformer.

using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Explore.API.OpenApi;

/// <summary>
/// Swashbuckle schema filter that properly generates OpenAPI schemas for HAL wrapper types.
/// The generic HalResource{T} flattens T's properties to root level during JSON serialization,
/// but Swagger doesn't understand this. This filter adds T's properties to the schema.
///
/// <para>
/// <b>NOTE:</b> This is the Swashbuckle (ISchemaFilter) version. For ASP.NET Core native OpenAPI,
/// use <see cref="HalSchemaTransformer"/> which implements IOpenApiSchemaTransformer.
/// </para>
/// </summary>
public class HalSchemaFilter : ISchemaFilter
{
    // Swashbuckle 10.x uses IOpenApiSchema interface; cast to OpenApiSchema for mutation
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        // Cast to concrete type for mutation (Swashbuckle 10.x pattern)
        if (schema is not OpenApiSchema openApiSchema)
            return;

        var type = context.Type;

        // Handle HalResource<T> - flatten Data properties to root
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Explore.Application.Hateoas.HalResource<>))
        {
            var innerType = type.GetGenericArguments().FirstOrDefault();
            if (innerType != null)
            {
                // Ensure schema is properly initialized as an object
                // OpenAPI 3.1 uses JsonSchemaType enum instead of strings
                openApiSchema.Type = JsonSchemaType.Object;
                openApiSchema.Properties ??= new Dictionary<string, IOpenApiSchema>();
                openApiSchema.Required ??= new HashSet<string>();

                // First, ensure the inner DTO is registered in the schema repository
                var dtoSchema = context.SchemaGenerator.GenerateSchema(innerType, context.SchemaRepository);

                // Add the DTO properties to the HAL resource schema
                AddDtoPropertiesToSchema(openApiSchema, innerType, dtoSchema, context);
                AddHalLinksProperty(openApiSchema);
                AddHalEmbeddedProperty(openApiSchema);
            }
        }

        // Handle HalCollectionEmbedded<T> - ensure items array has correct type
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Explore.Application.Hateoas.HalCollectionEmbedded<>))
        {
            var innerType = type.GetGenericArguments().FirstOrDefault();
            if (innerType != null)
            {
                openApiSchema.Type = JsonSchemaType.Object;
                openApiSchema.Properties ??= new Dictionary<string, IOpenApiSchema>();
                EnsureEmbeddedItemsArrayType(openApiSchema, innerType, context);
            }
        }
    }

    private void AddDtoPropertiesToSchema(OpenApiSchema schema, Type dtoType, IOpenApiSchema dtoSchema, SchemaFilterContext context)
    {
        // If the DTO schema is a reference, resolve it to get the actual properties
        OpenApiSchema? resolvedSchema = dtoSchema as OpenApiSchema;

        // In OpenApi 2.x, references are handled via OpenApiSchemaReference
        if (dtoSchema is OpenApiSchemaReference schemaRef)
        {
            var refId = schemaRef.Id;
            if (context.SchemaRepository.Schemas.TryGetValue(refId, out var referencedSchema))
            {
                resolvedSchema = referencedSchema as OpenApiSchema;
            }
        }

        if (resolvedSchema == null)
            return;

        // Ensure properties dictionary exists
        schema.Properties ??= new Dictionary<string, IOpenApiSchema>();

        // Copy all properties from the DTO to the HAL resource schema
        if (resolvedSchema.Properties != null)
        {
            foreach (var property in resolvedSchema.Properties)
            {
                if (!schema.Properties.ContainsKey(property.Key))
                {
                    schema.Properties[property.Key] = property.Value;
                }
            }
        }

        // Copy required fields
        schema.Required ??= new HashSet<string>();
        if (resolvedSchema.Required != null)
        {
            foreach (var required in resolvedSchema.Required)
            {
                if (!schema.Required.Contains(required))
                {
                    schema.Required.Add(required);
                }
            }
        }
    }

    private void AddHalLinksProperty(OpenApiSchema schema)
    {
        if (!schema.Properties.ContainsKey("_links"))
        {
            schema.Properties["_links"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                AdditionalPropertiesAllowed = true,
                Description = "HAL hypermedia links",
                AdditionalProperties = new OpenApiSchema
                {
                    Type = JsonSchemaType.Object,
                    Properties = new Dictionary<string, IOpenApiSchema>
                    {
                        ["href"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = "Link URL" },
                        ["method"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = "HTTP method" },
                        ["title"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null, Description = "Link title" }
                    }
                }
            };
        }
    }

    private void AddHalEmbeddedProperty(OpenApiSchema schema)
    {
        if (!schema.Properties.ContainsKey("_embedded"))
        {
            schema.Properties["_embedded"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object | JsonSchemaType.Null,
                Description = "Embedded related resources",
                AdditionalPropertiesAllowed = true
            };
        }
    }

    private void EnsureEmbeddedItemsArrayType(OpenApiSchema schema, Type itemType, SchemaFilterContext context)
    {
        // Generate schema reference for the item type wrapped in HalResource<T>
        var halResourceType = typeof(Explore.Application.Hateoas.HalResource<>).MakeGenericType(itemType);
        var itemSchema = context.SchemaGenerator.GenerateSchema(halResourceType, context.SchemaRepository);

        if (schema.Properties.TryGetValue("items", out var itemsProperty) && itemsProperty is OpenApiSchema itemsProp)
        {
            // Ensure items is typed as an array with proper item reference
            itemsProp.Type = JsonSchemaType.Array;
            itemsProp.Items = itemSchema;
        }
    }
}
