// ABOUTME: OpenAPI document transformer that registers inner DTO schemas from HAL wrapper types.
// Ensures NSwag generates proper DTO classes by adding them as explicit schemas in the OpenAPI document.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Explore.API.OpenApi;

/// <summary>
/// Document transformer that ensures inner DTO types from HAL wrappers are registered as schemas.
/// The HAL wrapper types (HalResource{T}, HalCollectionResource{T}) have custom JSON serialization
/// that flattens properties, but the OpenAPI generator doesn't create schemas for the inner T types.
/// This transformer adds those schemas explicitly.
/// </summary>
public class HalDtoSchemaTransformer : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();

        foreach (var dtoType in HalOpenApiSchemaCatalog.RegisteredDtoTypes)
        {
            var schemaName = dtoType.Name;

            // Skip if schema already exists
            if (document.Components.Schemas.ContainsKey(schemaName))
                continue;

            // Generate schema for this DTO type using the context's method
            // GetOrCreateSchemaAsync returns OpenApiSchema directly
            var schema = await context.GetOrCreateSchemaAsync(dtoType, cancellationToken: cancellationToken);

            // Use AddComponent to properly register the schema in the document
            document.AddComponent(schemaName, schema);
        }

        // Also populate the empty HAL wrapper schemas with flattened DTO properties + HAL links
        await PopulateHalResourceSchemas(document, context, cancellationToken);

        // Type the HAL collection embedded items as HAL resource references instead of object arrays.
        PopulateHalCollectionEmbeddedSchemas(document);

        // Fix inline array item schemas that should be $ref references to component schemas.
        // GetOrCreateSchemaAsync inlines nested DTO types (e.g., EventDto.Tags = List<TagListDto>)
        // which causes NSwag to generate duplicate types with conflicting names.
        ReplaceInlineArrayItemsWithReferences(document);
    }

    private async Task PopulateHalResourceSchemas(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var schemas = document.Components?.Schemas;
        if (schemas == null)
            return;

        foreach (var (halSchemaName, dtoType) in HalOpenApiSchemaCatalog.DetailResourceMappings)
        {
            if (!schemas.TryGetValue(halSchemaName, out var halSchemaInterface))
            {
                halSchemaInterface = new OpenApiSchema();
                schemas[halSchemaName] = halSchemaInterface;
            }

            // Cast to concrete type for mutation (OpenApi 2.x pattern)
            if (halSchemaInterface is not OpenApiSchema halSchema)
                continue;

            // Get or create schema for the inner DTO
            var dtoSchema = await context.GetOrCreateSchemaAsync(dtoType, cancellationToken: cancellationToken);

            HalOpenApiSchemaMutator.FlattenDtoIntoHalResource(halSchema, dtoSchema);
        }
    }

    private static void PopulateHalCollectionEmbeddedSchemas(OpenApiDocument document)
    {
        var schemas = document.Components?.Schemas;
        if (schemas == null)
            return;

        foreach (var (embeddedSchemaName, itemResourceSchemaName) in HalOpenApiSchemaCatalog.CollectionEmbeddedItemResourceMappings)
        {
            if (!schemas.TryGetValue(embeddedSchemaName, out var embeddedSchemaInterface))
                continue;

            if (embeddedSchemaInterface is not OpenApiSchema embeddedSchema)
                continue;

            HalOpenApiSchemaMutator.EnsureEmbeddedItemsArrayType(
                embeddedSchema,
                new OpenApiSchemaReference(itemResourceSchemaName, document));
        }
    }

    /// <summary>
    /// Replaces inline array item schemas with $ref references to registered component schemas.
    /// When GetOrCreateSchemaAsync generates a schema for a DTO with List&lt;T&gt; properties
    /// (e.g., EventDto.Tags = List&lt;TagListDto&gt;), the inner T schema is inlined.
    /// This causes NSwag to generate duplicate types with conflicting names (Tags/tags).
    /// This method uses reflection to find such properties and replaces inline schemas
    /// with proper $ref references to the component schemas registered above.
    /// </summary>
    private void ReplaceInlineArrayItemsWithReferences(OpenApiDocument document)
    {
        var registeredDtoNames = new HashSet<string>(HalOpenApiSchemaCatalog.RegisteredDtoTypes.Select(t => t.Name));

        foreach (var dtoType in HalOpenApiSchemaCatalog.RegisteredDtoTypes)
        {
            foreach (var prop in dtoType.GetProperties())
            {
                if (!TryGetCollectionItemType(prop.PropertyType, out var itemType))
                    continue;
                if (!registeredDtoNames.Contains(itemType.Name))
                    continue;

                // Found a collection of a known DTO — fix only the owning DTO schema and its cataloged HAL wrapper.
                var camelCase = char.ToLowerInvariant(prop.Name[0]) + prop.Name[1..];

                ReplaceInlineArrayItemsWithReference(document, dtoType.Name, camelCase, itemType.Name);

                foreach (var halSchemaName in HalOpenApiSchemaCatalog.DetailResourceMappings
                    .Where(mapping => mapping.Value == dtoType)
                    .Select(mapping => mapping.Key))
                {
                    ReplaceInlineArrayItemsWithReference(document, halSchemaName, camelCase, itemType.Name);
                }
            }
        }
    }

    private static bool TryGetCollectionItemType(Type propertyType, out Type itemType)
    {
        itemType = typeof(object);

        if (!propertyType.IsGenericType)
            return false;

        var genericDefinition = propertyType.GetGenericTypeDefinition();
        if (genericDefinition != typeof(List<>)
            && genericDefinition != typeof(IReadOnlyList<>)
            && genericDefinition != typeof(IList<>))
        {
            return false;
        }

        itemType = propertyType.GetGenericArguments()[0];
        return true;
    }

    private static void ReplaceInlineArrayItemsWithReference(OpenApiDocument document, string schemaName, string propertyName, string itemSchemaName)
    {
        if (document.Components?.Schemas?.TryGetValue(schemaName, out var schemaI) != true)
            return;

        if (schemaI is not OpenApiSchema schema || schema.Properties == null)
            return;

        if (!schema.Properties.TryGetValue(propertyName, out var arraySchemaI))
            return;

        if (arraySchemaI is OpenApiSchema arraySchema
            && arraySchema.Items != null
            && arraySchema.Items is not OpenApiSchemaReference)
        {
            arraySchema.Items = new OpenApiSchemaReference(itemSchemaName, document);
        }
    }

}
