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
    // DTOs that need to be explicitly registered because they're only used inside HAL wrappers
    private static readonly Type[] DtoTypes = new[]
    {
        // Event DTOs
        typeof(Explore.Application.DTOs.Event.EventDto),
        typeof(Explore.Application.DTOs.Event.EventListDto),

        // EventSession DTOs
        typeof(Explore.Application.DTOs.EventSession.EventSessionDto),
        typeof(Explore.Application.DTOs.EventSession.EventSessionListDto),

        // Category DTOs
        typeof(Explore.Application.DTOs.Category.CategoryDto),
        typeof(Explore.Application.DTOs.Category.CategoryListDto),

        // Tag DTOs
        typeof(Explore.Application.DTOs.Tag.TagDto),
        typeof(Explore.Application.DTOs.Tag.TagListDto),

        // Location DTOs
        typeof(Explore.Application.DTOs.Location.LocationDto),
        typeof(Explore.Application.DTOs.Location.LocationListDto),

        // Organization DTOs
        typeof(Explore.Application.DTOs.Organization.OrganizationDto),
        typeof(Explore.Application.DTOs.Organization.OrganizationListDto),

        // Actor DTOs
        typeof(Explore.Application.DTOs.Actor.ActorDto),
        typeof(Explore.Application.DTOs.Actor.ActorListDto),

        // EventSessionSpeaker DTOs
        typeof(Explore.Application.DTOs.EventSessionSpeaker.EventSessionSpeakerDto),
        typeof(Explore.Application.DTOs.EventSessionSpeaker.EventSessionSpeakerListDto),

        // EventSessionLanguage DTOs
        typeof(Explore.Application.DTOs.EventSessionLanguage.EventSessionLanguageDto),
        typeof(Explore.Application.DTOs.EventSessionLanguage.EventSessionLanguageListDto),

        // EventAspects DTOs
        typeof(Explore.Application.DTOs.EventAspects.EventIslamicAspectDto),
        typeof(Explore.Application.DTOs.EventAspects.EventTechAspectDto),
    };

    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();

        foreach (var dtoType in DtoTypes)
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
    }

    private async Task PopulateHalResourceSchemas(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var halResourceMappings = new Dictionary<string, Type>
        {
            ["HalResourceOfEventDto"] = typeof(Explore.Application.DTOs.Event.EventDto),
            ["HalResourceOfEventSessionDto"] = typeof(Explore.Application.DTOs.EventSession.EventSessionDto),
            ["HalResourceOfCategoryDto"] = typeof(Explore.Application.DTOs.Category.CategoryDto),
            ["HalResourceOfTagDto"] = typeof(Explore.Application.DTOs.Tag.TagDto),
            ["HalResourceOfLocationDto"] = typeof(Explore.Application.DTOs.Location.LocationDto),
            ["HalResourceOfOrganizationDto"] = typeof(Explore.Application.DTOs.Organization.OrganizationDto),
            ["HalResourceOfActorDto"] = typeof(Explore.Application.DTOs.Actor.ActorDto),
        };

        foreach (var (halSchemaName, dtoType) in halResourceMappings)
        {
            if (!document.Components.Schemas.TryGetValue(halSchemaName, out var halSchemaInterface))
                continue;

            // Cast to concrete type for mutation (OpenApi 2.x pattern)
            if (halSchemaInterface is not OpenApiSchema halSchema)
                continue;

            // Only process if the schema is empty (no properties defined)
            if (halSchema.Properties != null && halSchema.Properties.Count > 0)
                continue;

            // Get or create schema for the inner DTO
            var dtoSchema = await context.GetOrCreateSchemaAsync(dtoType, cancellationToken: cancellationToken);

            // Initialize the HAL schema as an object
            halSchema.Type = JsonSchemaType.Object;
            halSchema.Properties ??= new Dictionary<string, IOpenApiSchema>();

            // Copy properties from DTO to HAL resource (flattening behavior)
            // dtoSchema is OpenApiSchema, so we can access Properties directly
            if (dtoSchema.Properties != null)
            {
                foreach (var prop in dtoSchema.Properties)
                {
                    if (!halSchema.Properties.ContainsKey(prop.Key))
                    {
                        halSchema.Properties[prop.Key] = prop.Value;
                    }
                }
            }

            // Add HAL _links property
            if (!halSchema.Properties.ContainsKey("_links"))
            {
                halSchema.Properties["_links"] = CreateHalLinksSchema();
            }

            // Add HAL _embedded property
            if (!halSchema.Properties.ContainsKey("_embedded"))
            {
                halSchema.Properties["_embedded"] = new OpenApiSchema
                {
                    // OpenApi 2.x: Use Type flags for nullable
                    Type = JsonSchemaType.Object | JsonSchemaType.Null,
                    Description = "Embedded related resources",
                    AdditionalPropertiesAllowed = true
                };
            }
        }
    }

    private OpenApiSchema CreateHalLinksSchema()
    {
        return new OpenApiSchema
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
                    // OpenApi 2.x: Use Type flags for nullable instead of Nullable property
                    ["title"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null, Description = "Link title" }
                }
            }
        };
    }
}
