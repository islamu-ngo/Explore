// ABOUTME: Shared OpenAPI enum schema normalization for native OpenAPI and Swashbuckle.
// ABOUTME: Keeps public enum schemas aligned with the API's JsonStringEnumConverter contract.

using System.Text.Json.Nodes;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.LocationPrivacy;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.CustomPropertyProjection;
using Explore.Application.DTOs.EventOrganizerClaim;
using Explore.Application.DTOs.Location;
using Explore.Application.DTOs.ManagedProviderProvisioning;
using Explore.Application.DTOs.OrganizationTenantEvidence;
using Explore.Application.DTOs.Settings;
using Explore.Application.Features.EventReporting.Models;
using Explore.Application.Models;
using Explore.Application.Models.PublicExperience;
using Explore.Application.Onboarding;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Enums.Analytics;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Explore.API.OpenApi;

/// <summary>
/// Explicit allowlist of public enum schemas that must be represented as strings in OpenAPI.
/// </summary>
internal static class OpenApiStringEnumSchemaCatalog
{
    public static IReadOnlyCollection<Type> EnumTypes { get; } =
    [
        typeof(CustomPropertyProjectionScopeType),
        typeof(CustomPropertyProjectionState),
        typeof(CustomPropertyFilterOperator),
        typeof(DeclineBehavior),
        typeof(EmailDispatchUnknownReconciliationOutcome),
        typeof(EntityTypeName),
        typeof(EventReportDecisionKind),
        typeof(EventReportProviderEvidenceMode),
        typeof(EventReportPriority),
        typeof(EventReportSeverityHint),
        typeof(EventOrganizerClaimReviewDecision),
        typeof(EventLocationDisclosureField),
        typeof(EventLocationDisclosureFieldClass),
        typeof(EventLocationDisclosureFields),
        typeof(EventLocationDisclosurePolicyGate),
        typeof(EventLocationDisclosurePurpose),
        typeof(EventLocationDisclosureState),
        typeof(EventRoleAssignmentStatus),
        typeof(ExposureLevel),
        typeof(GuestRecoveryPolicyEnum),
        typeof(GenderSegregationMode),
        typeof(HomeDiscoveryMode),
        typeof(HomeDiscoverySectionStatus),
        typeof(KeycloakBootstrapMode),
        typeof(ManagedProviderOrganizerKindDto),
        typeof(OrganizationTenantEvidenceReviewDecisionDto),
        typeof(PosthogCookielessMode),
        typeof(PosthogPersonProfiles),
        typeof(PrayerTime),
        typeof(PromotionRecommendation),
        typeof(PropertyType),
        typeof(PublicExperienceCtaPlacement),
        typeof(PublicExperienceCtaStyle),
        typeof(PublicExperienceHomeBlockKind),
        typeof(PublicExperienceMode),
        typeof(PublicExperiencePrimaryOrganizationState),
        typeof(RoleEnum),
        typeof(SessionStartTimeType),
        typeof(SessionEndTimeType),
        typeof(SettingSource),
        typeof(SkillLevel),
        typeof(SupportAccessModeEnum),
        typeof(BatchUpdateMode),
        typeof(DeploymentMode)
    ];

    public static bool IsStringEnum(Type type)
        => type.IsEnum && EnumTypes.Contains(type);

    public static bool TryGetEnumType(string schemaName, out Type enumType)
    {
        enumType = EnumTypes.FirstOrDefault(type => string.Equals(type.Name, schemaName, StringComparison.Ordinal))
            ?? typeof(void);

        return enumType != typeof(void);
    }
}

internal static class OpenApiStringEnumSchemaMutator
{
    public static void Apply(OpenApiSchema schema, Type enumType)
    {
        schema.Type = JsonSchemaType.String;
        schema.Format = null;
        schema.Pattern = null;
        schema.Enum = Enum.GetNames(enumType)
            .Select(name => JsonValue.Create(name)!)
            .ToList<JsonNode>();
    }
}

/// <summary>
/// Native OpenAPI document transformer that normalizes known public enum component schemas.
/// </summary>
internal sealed class OpenApiStringEnumDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        if (document.Components?.Schemas is null)
        {
            return Task.CompletedTask;
        }

        foreach (var (schemaName, schema) in document.Components.Schemas)
        {
            var openApiSchema = schema as OpenApiSchema;

            if (openApiSchema is not null
                && OpenApiStringEnumSchemaCatalog.TryGetEnumType(schemaName, out var enumType))
            {
                OpenApiStringEnumSchemaMutator.Apply(openApiSchema, enumType);
            }

            if (schema.Properties is not null
                && schema.Properties.ContainsKey("endTimeType"))
            {
                schema.Properties["endTimeType"] =
                    new OpenApiSchemaReference(nameof(SessionEndTimeType), document);
            }
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Swashbuckle schema filter that mirrors native string-enum schema normalization.
/// </summary>
internal sealed class OpenApiStringEnumSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is OpenApiSchema openApiSchema
            && OpenApiStringEnumSchemaCatalog.IsStringEnum(context.Type))
        {
            OpenApiStringEnumSchemaMutator.Apply(openApiSchema, context.Type);
        }
    }
}
