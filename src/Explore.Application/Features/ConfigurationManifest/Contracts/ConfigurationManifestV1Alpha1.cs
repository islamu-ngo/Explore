// ABOUTME: Declares the strict v1alpha1 contract for one instance-wide configuration manifest.
// ABOUTME: Keeps portable non-secret transport shape in Application and rejects unmapped members.

namespace Explore.Application.Features.ConfigurationManifest.Contracts;

using System.Text.Json;
using System.Text.Json.Serialization;

public static class ConfigurationManifestContractMetadata
{
    public const string SchemaId =
        "https://schemas.islamu.org/event/configuration-manifest/v1alpha1/schema.json";
    public const string ApiVersion = "configuration.islamu.org/v1alpha1";
    public const string Kind = "ConfigurationManifest";
    public const string MediaType =
        "application/vnd.islamu.configuration-manifest.v1alpha1+json";
}

public static class ConfigurationManifestExportMetadataValues
{
    public const string OverridesView = "Overrides";
    public const string PortableView = "Portable";
    public const string InstanceAndTenantsAuthorityScope = "InstanceAndTenants";
}

public static class ConfigurationManifestDocumentKeys
{
    public const string InstancePaidEventPolicy = "instance.paid_event_policy";
    public const string TenantPaidEventPolicy = "tenant.paid_event_policy";
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfigurationManifestV1Alpha1
{
    [JsonPropertyName("$schema")]
    public required string Schema { get; init; }

    public required string ApiVersion { get; init; }

    public required string Kind { get; init; }

    public required ConfigurationManifestMetadataV1Alpha1 Metadata { get; init; }

    public required ConfigurationManifestSpecV1Alpha1 Spec { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfigurationManifestMetadataV1Alpha1
{
    public required string Name { get; init; }

    public ConfigurationManifestExportMetadataV1Alpha1? Export { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfigurationManifestExportMetadataV1Alpha1
{
    public required string View { get; init; }

    public required bool EffectiveValuesFlattened { get; init; }

    public required bool SensitiveValuesOmitted { get; init; }

    public required string AuthorityScope { get; init; }

    public required bool SovereignValuesOmitted { get; init; }

    public required IReadOnlyList<string> SovereignLockedFields { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfigurationManifestSpecV1Alpha1
{
    public required ConfigurationManifestInstanceV1Alpha1 Instance { get; init; }

    public required IReadOnlyList<ConfigurationManifestTenantV1Alpha1> Tenants { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfigurationManifestInstanceV1Alpha1
{
    public required IReadOnlyDictionary<string, JsonElement> Settings { get; init; }

    public required IReadOnlyDictionary<string, ConfigurationManifestDocumentV1Alpha1>
        Documents { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfigurationManifestTenantV1Alpha1
{
    public required ConfigurationManifestTenantMetadataV1Alpha1 Metadata { get; init; }

    public required ConfigurationManifestTenantSpecV1Alpha1 Spec { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfigurationManifestTenantMetadataV1Alpha1
{
    public required string Name { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfigurationManifestTenantSpecV1Alpha1
{
    public required string DisplayName { get; init; }

    public required IReadOnlyDictionary<string, JsonElement> Settings { get; init; }

    public required IReadOnlyDictionary<string, ConfigurationManifestDocumentV1Alpha1>
        Documents { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfigurationManifestDocumentV1Alpha1
{
    public required int SchemaVersion { get; init; }

    public required JsonElement Payload { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfigurationManifestBrandingPayloadV1Alpha1
{
    public string? DisplayName { get; init; }

    public string? LogoUrl { get; init; }

    public string? FaviconUrl { get; init; }

    public string? CustomCssUrl { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfigurationManifestPaidEventPolicyPayloadV1Alpha1
{
    public required bool IsPaymentsEnabled { get; init; }

    public required IReadOnlyList<int> AllowedOrganizerKindIds { get; init; }

    public required bool RequiresLocalVerification { get; init; }

    public required IReadOnlyList<string> AllowedCurrencyCodes { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required string? DefaultCurrencyCode { get; init; }

    public required IReadOnlyList<int> RefundProtectionIds { get; init; }

    public required IReadOnlyList<ConfigurationManifestPaidEventPolicyCurrencyRiskLimitV1Alpha1>
        CurrencyRiskLimits { get; init; }

    public required bool RequiresFirstPaidEventReview { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required int? FarFutureReviewThresholdDays { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfigurationManifestPaidEventPolicyCurrencyRiskLimitV1Alpha1
{
    public required string CurrencyCode { get; init; }

    public long? PerEventSalesCeilingMinor { get; init; }

    public int? PerEventSalesCountCeiling { get; init; }

    public long? RollingOrganizerSalesCeilingMinor { get; init; }

    public int? RollingOrganizerSalesCountCeiling { get; init; }

    public int? RollingOrganizerWindowDays { get; init; }

    public long? HighValueReviewThresholdMinor { get; init; }
}
