// ABOUTME: Contract invariant tests over the exported OpenAPI document (/openapi/event-api.json).
// ABOUTME: RED tests documenting 464-duplicate-operation defect - enforces invariants expected post-stabilization.

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;

namespace Event.Api.IntegrationTests.Features;

/// <summary>
/// Invariants every canonical OpenAPI document MUST satisfy. These are enforced at the
/// contract boundary so drift is caught at CI time, not at client-generation time.
///
/// Strategy:
/// 1. Fetch the runtime OpenAPI document at <c>/openapi/event-api.json</c>.
/// 2. Walk <c>paths.*.operations.*</c> and assert invariants.
///
/// Phase: P0 Guardrails (api-contract-stabilization plan).
/// Current state: these tests are EXPECTED TO FAIL until Phase 2 (delete URL-segment alias)
/// and Phase 3 (stable operationId) land. They prove the defect exists and prevent regression
/// once fixed.
/// </summary>
[ClassDataSource<ContractApiFixture>(Shared = SharedType.PerAssembly)]
public class ContractInvariantsTests
{
    private const string OpenApiEndpoint = "/openapi/event-api.json";

    /// <summary>HTTP verbs that carry an operation in OpenAPI 3.0.</summary>
    private static readonly string[] HttpVerbs =
    [
        "get", "post", "put", "delete", "patch", "head", "options", "trace"
    ];

    /// <summary>
    /// Placeholder/fallback names that indicate a missing or broken <c>operationId</c>.
    /// These are what NSwag synthesizes when the OpenAPI doc lacks a stable id.
    /// </summary>
    private static readonly string[] BannedOperationIdPatterns =
    [
        "GET", "GET2", "GET3", "POST", "POST2", "POST3",
        "PUT", "PUT2", "PUT3", "DELETE", "DELETE2", "DELETE3",
        "PATCH", "PATCH2", "PATCH3"
    ];

    private readonly ContractApiFixture _fixture;

    public ContractInvariantsTests(ContractApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task OpenApiDocument_IsReachable_AndReturnsJson()
    {
        var response = await _fixture.Client.GetAsync(OpenApiEndpoint);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("application/json");
    }

    [Test]
    public async Task OpenApiDocument_ContainsNoUrlSegmentVersionedPaths()
    {
        using var document = await GetOpenApiDocumentAsync();

        var versionedPaths = EnumeratePaths(document)
            .Where(path => path.StartsWith("/api/v", System.StringComparison.OrdinalIgnoreCase)
                && path.Length > 6
                && char.IsDigit(path[6]))
            .ToList();

        await Assert.That(versionedPaths)
            .IsEmpty()
            .Because($"URL-segment versioning is retired. Found {versionedPaths.Count} versioned path(s): {string.Join(", ", versionedPaths.Take(5))}");
    }

    [Test]
    public async Task OpenApiDocument_ContainsNoDuplicatePathMethodPairs()
    {
        using var document = await GetOpenApiDocumentAsync();

        var pairs = EnumerateOperations(document)
            .Select(op => $"{op.Method.ToUpperInvariant()} {op.Path}")
            .ToList();

        var duplicates = pairs
            .GroupBy(p => p)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key} (x{g.Count()})")
            .ToList();

        await Assert.That(duplicates)
            .IsEmpty()
            .Because($"Every (method, path) pair must appear exactly once. Duplicates: {string.Join("; ", duplicates.Take(5))}");
    }

    [Test]
    public async Task OpenApiDocument_EveryOperationHasOperationId()
    {
        using var document = await GetOpenApiDocumentAsync();

        var missing = EnumerateOperations(document)
            .Where(op => string.IsNullOrWhiteSpace(op.OperationId))
            .Select(op => $"{op.Method.ToUpperInvariant()} {op.Path}")
            .ToList();

        await Assert.That(missing)
            .IsEmpty()
            .Because($"Every operation MUST declare a stable operationId. {missing.Count} missing: {string.Join("; ", missing.Take(5))}");
    }

    [Test]
    public async Task OpenApiDocument_OperationIdsAreUnique()
    {
        using var document = await GetOpenApiDocumentAsync();

        var duplicates = EnumerateOperations(document)
            .Where(op => !string.IsNullOrWhiteSpace(op.OperationId))
            .GroupBy(op => op.OperationId!)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key} (x{g.Count()})")
            .ToList();

        await Assert.That(duplicates)
            .IsEmpty()
            .Because($"operationId values must be globally unique. Duplicates: {string.Join("; ", duplicates.Take(5))}");
    }

    [Test]
    public async Task OpenApiDocument_NoOperationIdsUsePlaceholderFallbackNames()
    {
        using var document = await GetOpenApiDocumentAsync();

        var banned = EnumerateOperations(document)
            .Where(op => !string.IsNullOrWhiteSpace(op.OperationId))
            .Where(op => BannedOperationIdPatterns.Contains(op.OperationId, System.StringComparer.Ordinal)
                || EndsWithDigitBeforeSuffix(op.OperationId!, "Async")
                || EndsWithDigit(op.OperationId!))
            .Select(op => $"{op.OperationId} @ {op.Method.ToUpperInvariant()} {op.Path}")
            .ToList();

        await Assert.That(banned)
            .IsEmpty()
            .Because($"operationIds must be human-readable, not NSwag collision fallbacks (GET, POST2, TenantGET2, FooAsync2, ...). Found: {string.Join("; ", banned.Take(5))}");
    }

    [Test]
    public async Task OpenApiDocument_EventListResponseReferencesHalCollectionSchema()
    {
        using var document = await GetOpenApiDocumentAsync();

        var content = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/event")
            .GetProperty("get")
            .GetProperty("responses")
            .GetProperty("200")
            .GetProperty("content");

        await Assert.That(GetSchemaReference(content.GetProperty("application/hal+json; v=0.1")))
            .IsEqualTo("#/components/schemas/HalCollectionResourceOfEventListDto")
            .Because("The canonical HAL event list response must reference the HAL collection wrapper schema.");
        await Assert.That(GetSchemaReference(content.GetProperty("application/json; v=0.1")))
            .IsEqualTo("#/components/schemas/HalCollectionResourceOfEventListDto")
            .Because("The versioned JSON event list response must stay aligned with the HAL collection wrapper schema.");
    }

    [Test]
    public async Task OpenApiDocument_HalCollectionResourceSchemaHasLinksEmbeddedAndPagination()
    {
        using var document = await GetOpenApiDocumentAsync();

        var properties = GetSchemaProperties(document, "HalCollectionResourceOfEventListDto");
        var expectedProperties = new[] { "_links", "_embedded", "pageNumber", "pageSize", "totalCount", "totalPages" };

        var missingProperties = expectedProperties
            .Where(propertyName => !properties.TryGetProperty(propertyName, out _))
            .ToList();

        await Assert.That(missingProperties)
            .IsEmpty()
            .Because($"The event list HAL collection wrapper must expose pagination plus HAL affordances. Missing: {string.Join(", ", missingProperties)}");
        await Assert.That(GetReference(properties.GetProperty("_embedded")))
            .IsEqualTo("#/components/schemas/HalCollectionEmbeddedOfEventListDto")
            .Because("The HAL collection wrapper must reference the typed embedded collection schema.");
        await Assert.That(GetReference(properties.GetProperty("_links").GetProperty("additionalProperties")))
            .IsEqualTo("#/components/schemas/HalLink")
            .Because("HAL _links must be documented as a relation-name map of HalLink objects.");
    }

    [Test]
    public async Task OpenApiDocument_HalCollectionEmbeddedSchemaHasItemsArray()
    {
        using var document = await GetOpenApiDocumentAsync();

        var embeddedProperties = GetSchemaProperties(document, "HalCollectionEmbeddedOfEventListDto");
        var items = embeddedProperties.GetProperty("items");

        await Assert.That(GetStringProperty(items, "type"))
            .IsEqualTo("array")
            .Because("The event list embedded HAL collection must expose an items array for embedded resources.");
        await Assert.That(GetReference(items.GetProperty("items")))
            .IsEqualTo("#/components/schemas/HalResourceOfEventListDto")
            .Because("Embedded event list items must be typed as HAL resources, not as object arrays.");
    }

    [Test]
    public async Task OpenApiDocument_HalCollectionEmbeddedItemsReferenceTypedHalResources()
    {
        using var document = await GetOpenApiDocumentAsync();

        var schemas = document.RootElement
            .GetProperty("components")
            .GetProperty("schemas");

        var untypedEmbeddedCollections = schemas
            .EnumerateObject()
            .Where(schema => schema.Name.StartsWith("HalCollectionEmbeddedOf", System.StringComparison.Ordinal))
            .Select(schema =>
            {
                var itemReference = schema.Value.TryGetProperty("properties", out var properties)
                    && properties.TryGetProperty("items", out var items)
                    && items.TryGetProperty("items", out var itemSchema)
                    ? GetReference(itemSchema)
                    : null;

                return new { SchemaName = schema.Name, ItemReference = itemReference };
            })
            .Where(schema => string.IsNullOrWhiteSpace(schema.ItemReference)
                || !schema.ItemReference.StartsWith("#/components/schemas/HalResourceOf", System.StringComparison.Ordinal))
            .Select(schema => $"{schema.SchemaName} -> {schema.ItemReference ?? "<missing>"}")
            .ToList();

        await Assert.That(untypedEmbeddedCollections)
            .IsEmpty()
            .Because("Every HAL embedded collection must type its items as HAL resources so generated clients do not fall back to ICollection<object>. "
                + $"Untyped: {string.Join(", ", untypedEmbeddedCollections)}");
    }

    [Test]
    public async Task OpenApiDocument_HalResourceSchemaIsFlattenedAndHasHalProperties()
    {
        using var document = await GetOpenApiDocumentAsync();

        var properties = GetSchemaProperties(document, "HalResourceOfEventDto");
        var expectedProperties = new[] { "id", "title", "_links", "_embedded" };
        var missingProperties = expectedProperties
            .Where(propertyName => !properties.TryGetProperty(propertyName, out _))
            .ToList();

        await Assert.That(missingProperties)
            .IsEmpty()
            .Because($"The event HAL resource schema must stay flattened and expose HAL affordances. Missing: {string.Join(", ", missingProperties)}");
        await Assert.That(properties.TryGetProperty("data", out _))
            .IsFalse()
            .Because("HAL resource schemas must be flattened; a nested data property would break the client contract.");
    }

    [Test]
    public async Task OpenApiDocument_CatalogedHalResourceSchemasAreFlattened()
    {
        using var document = await GetOpenApiDocumentAsync();
        var catalogedSchemaNames = new[]
        {
            "HalResourceOfEventDto",
            "HalResourceOfEventSessionDto",
            "HalResourceOfEventSessionGroupDto",
            "HalResourceOfCategoryDto",
            "HalResourceOfTagDto",
            "HalResourceOfLocationDto",
            "HalResourceOfOrganizationDto",
            "HalResourceOfActorDto",
            "HalResourceOfCustomPropertyDefinitionDto",
            "HalResourceOfEventCustomPropertyDefinitionDto",
            "HalResourceOfEventSessionCustomPropertyDefinitionDto",
            "HalResourceOfEventTemplateDto",
            "HalResourceOfEventSessionTemplateDto",
            "HalResourceOfGroupDto",
            "HalResourceOfIndexedDidDto",
            "HalResourceOfLocationRoomDto",
            "HalResourceOfEventDayDto",
            "HalResourceOfEventAgendaItemDto",
            "HalResourceOfTemplateDiffDto"
        };

        var unflattened = catalogedSchemaNames
            .Where(schemaName =>
            {
                var properties = GetSchemaProperties(document, schemaName);
                return properties.TryGetProperty("data", out _)
                    || !properties.TryGetProperty("_links", out _)
                    || !properties.TryGetProperty("_embedded", out _);
            })
            .ToList();

        await Assert.That(unflattened)
            .IsEmpty()
            .Because($"Every explicitly cataloged HAL detail schema must be flattened and expose HAL affordances. Offenders: {string.Join(", ", unflattened)}");
    }

    [Test]
    public async Task OpenApiDocument_NonHalDtoSchemasAreNotMutatedAsHalResources()
    {
        using var document = await GetOpenApiDocumentAsync();

        var eventDtoProperties = GetSchemaProperties(document, "EventDto");

        await Assert.That(eventDtoProperties.TryGetProperty("_links", out _))
            .IsFalse()
            .Because("Plain DTO component schemas must not receive HAL affordance properties; only explicit HAL wrapper schemas are mutated.");
        await Assert.That(eventDtoProperties.TryGetProperty("_embedded", out _))
            .IsFalse()
            .Because("Plain DTO component schemas must stay reusable outside HAL wrappers.");
    }

    [Test]
    public async Task OpenApiDocument_PublicHalDetailResourceSchemasAreNotEmpty()
    {
        using var document = await GetOpenApiDocumentAsync();

        var schemas = document.RootElement
            .GetProperty("components")
            .GetProperty("schemas");

        var emptyHalDetailSchemas = schemas
            .EnumerateObject()
            .Where(schema => schema.Name.StartsWith("HalResourceOf", System.StringComparison.Ordinal))
            .Where(schema => !schema.Value.TryGetProperty("properties", out var properties)
                || properties.ValueKind != JsonValueKind.Object
                || !properties.EnumerateObject().Any())
            .Select(schema => schema.Name)
            .ToList();

        await Assert.That(emptyHalDetailSchemas)
            .IsEmpty()
            .Because($"Every public HAL detail wrapper must be explicitly evaluated and documented instead of remaining an empty schema. Empty: {string.Join(", ", emptyHalDetailSchemas)}");
    }

    [Test]
    public async Task OpenApiDocument_HalResourceArrayItemsRemainComponentReferences()
    {
        using var document = await GetOpenApiDocumentAsync();

        var properties = GetSchemaProperties(document, "HalResourceOfEventDto");

        await Assert.That(GetReference(properties.GetProperty("tags").GetProperty("items")))
            .IsEqualTo("#/components/schemas/TagListDto")
            .Because("HAL event tag arrays must reference the registered DTO component to avoid duplicate NSwag DTO artifacts.");
        await Assert.That(GetReference(properties.GetProperty("categories").GetProperty("items")))
            .IsEqualTo("#/components/schemas/CategoryListDto")
            .Because("HAL event category arrays must reference the registered DTO component to avoid duplicate NSwag DTO artifacts.");
    }

    [Test]
    public async Task OpenApiDocument_PublicEnumSchemasUseStringValues()
    {
        using var document = await GetOpenApiDocumentAsync();

        var roleEnum = GetSchema(document, "RoleEnum");
        var deploymentMode = GetSchema(document, "DeploymentMode");

        await Assert.That(GetStringProperty(roleEnum, "type"))
            .IsEqualTo("string")
            .Because("The API serializes enums with JsonStringEnumConverter, so RoleEnum must not be documented as an integer.");
        await Assert.That(GetEnumValues(roleEnum))
            .IsEquivalentTo(["Admin", "Moderator", "Member", "TenantAdmin", "TenantModerator", "TenantMember", "OrgAdmin", "OrgModerator", "OrgMember", "GroupAdmin", "GroupModerator", "GroupMember", "EventOwner", "EventManager", "RegistrationManager", "CheckInStaff"])
            .Because("RoleEnum must expose the public string literals clients receive over JSON.");

        await Assert.That(GetStringProperty(deploymentMode, "type"))
            .IsEqualTo("string")
            .Because("DeploymentMode must remain documented as the same string enum shape generated for the client.");
        await Assert.That(GetEnumValues(deploymentMode))
            .IsEquivalentTo(["SingleTenant", "MultiTenant"])
            .Because("DeploymentMode must expose public string literals, not numeric enum values.");
    }

    [Test]
    public async Task OpenApiDocument_RepresentativeNullablePropertiesIncludeNull()
    {
        using var document = await GetOpenApiDocumentAsync();

        var eventProperties = GetSchemaProperties(document, "EventDto");
        var organizationProperties = GetSchemaProperties(document, "OrganizationListDto");

        await Assert.That(SchemaAllowsNull(eventProperties.GetProperty("subtitle")))
            .IsTrue()
            .Because("Nullable scalar properties must include null in the schema so generated clients preserve optionality.");
        await Assert.That(SchemaAllowsNull(eventProperties.GetProperty("description")))
            .IsTrue()
            .Because("Additional nullable scalar properties must include null in the schema so generated clients preserve optionality.");
        await Assert.That(SchemaAllowsNull(organizationProperties.GetProperty("currentUserRole")))
            .IsTrue()
            .Because("Nullable enum references must include null in the schema while still referencing the enum component.");
        await Assert.That(GetOneOfReference(organizationProperties.GetProperty("currentUserRole")))
            .IsEqualTo("#/components/schemas/RoleEnum")
            .Because("Nullable enum references must retain the RoleEnum component reference instead of inlining or losing the enum schema.");
    }

    private async Task<JsonDocument> GetOpenApiDocumentAsync()
    {
        var response = await _fixture.Client.GetAsync(OpenApiEndpoint);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json);
    }

    private static IEnumerable<string> EnumeratePaths(JsonDocument document)
    {
        if (!document.RootElement.TryGetProperty("paths", out var paths)
            || paths.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        foreach (var pathEntry in paths.EnumerateObject())
        {
            yield return pathEntry.Name;
        }
    }

    private static IEnumerable<OperationRef> EnumerateOperations(JsonDocument document)
    {
        if (!document.RootElement.TryGetProperty("paths", out var paths)
            || paths.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        foreach (var pathEntry in paths.EnumerateObject())
        {
            if (pathEntry.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var operationEntry in pathEntry.Value.EnumerateObject())
            {
                if (!HttpVerbs.Contains(operationEntry.Name, System.StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                string? operationId = null;
                if (operationEntry.Value.TryGetProperty("operationId", out var opIdElement)
                    && opIdElement.ValueKind == JsonValueKind.String)
                {
                    operationId = opIdElement.GetString();
                }

                yield return new OperationRef(pathEntry.Name, operationEntry.Name, operationId);
            }
        }
    }

    private static bool EndsWithDigit(string value)
        => value.Length > 0 && char.IsDigit(value[^1]);

    private static bool EndsWithDigitBeforeSuffix(string value, string suffix)
    {
        if (!value.EndsWith(suffix, System.StringComparison.Ordinal)) return false;
        var stem = value[..^suffix.Length];
        return stem.Length > 0 && char.IsDigit(stem[^1]);
    }

    private static JsonElement GetSchemaProperties(JsonDocument document, string schemaName) => document.RootElement
        .GetProperty("components")
        .GetProperty("schemas")
        .GetProperty(schemaName)
        .GetProperty("properties");

    private static JsonElement GetSchema(JsonDocument document, string schemaName) => document.RootElement
        .GetProperty("components")
        .GetProperty("schemas")
        .GetProperty(schemaName);

    private static string? GetSchemaReference(JsonElement contentEntry) => GetReference(contentEntry.GetProperty("schema"));

    private static string? GetReference(JsonElement element) => GetStringProperty(element, "$ref");

    private static string? GetStringProperty(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static IReadOnlyList<string> GetEnumValues(JsonElement schema)
        => schema.TryGetProperty("enum", out var enumValues) && enumValues.ValueKind == JsonValueKind.Array
            ? enumValues.EnumerateArray()
                .Where(value => value.ValueKind == JsonValueKind.String)
                .Select(value => value.GetString()!)
                .ToArray()
            : [];

    private static bool SchemaAllowsNull(JsonElement schema)
    {
        if (schema.TryGetProperty("type", out var type))
        {
            if (type.ValueKind == JsonValueKind.String)
            {
                return string.Equals(type.GetString(), "null", System.StringComparison.Ordinal);
            }

            if (type.ValueKind == JsonValueKind.Array
                && type.EnumerateArray().Any(value => value.ValueKind == JsonValueKind.String
                    && string.Equals(value.GetString(), "null", System.StringComparison.Ordinal)))
            {
                return true;
            }
        }

        return schema.TryGetProperty("oneOf", out var oneOf)
            && oneOf.ValueKind == JsonValueKind.Array
            && oneOf.EnumerateArray().Any(SchemaAllowsNull);
    }

    private static string? GetOneOfReference(JsonElement schema)
    {
        if (!schema.TryGetProperty("oneOf", out var oneOf) || oneOf.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        return oneOf.EnumerateArray()
            .Select(GetReference)
            .FirstOrDefault(reference => !string.IsNullOrWhiteSpace(reference));
    }

    private readonly record struct OperationRef(string Path, string Method, string? OperationId);
}
