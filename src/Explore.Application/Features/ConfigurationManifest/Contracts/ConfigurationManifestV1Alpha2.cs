// ABOUTME: Declares strict v1alpha2 instance and tenant configuration portability artifacts.
// ABOUTME: Keeps source provenance separate from trusted target authority.

namespace Explore.Application.Features.ConfigurationManifest.Contracts;

using System.Text.Json;
using System.Text.Json.Serialization;
using Explore.Domain;

public static class ConfigurationManifestContractMetadata
{
    public const string SchemaId =
        "https://schemas.islamu.org/event/configuration-manifest/v1alpha2/schema.json";
    public const string ApiVersion = "configuration.islamu.org/v1alpha2";
    public const string Kind = "ConfigurationManifest";
    public const string MediaType =
        "application/vnd.islamu.configuration-manifest.v1alpha2+json";
}

public static class TenantConfigurationPackageContractMetadata
{
    public const string SchemaId =
        "https://schemas.islamu.org/event/tenant-configuration-package/v1alpha2/schema.json";
    public const string ApiVersion = "configuration.islamu.org/v1alpha2";
    public const string Kind = "TenantConfigurationPackage";
    public const string MediaType =
        "application/vnd.islamu.tenant-configuration-package.v1alpha2+json";
}

public static class ConfigurationManifestContentLimits
{
    public const int MaximumArtifactUtf8Bytes = 4 * 1024 * 1024;
    public const int MaximumLegalDocumentsPerScope =
        LegalDocumentContentLimits.MaximumDocumentsPerScope;
    public const int MaximumLegalLocalesPerDocument =
        LegalDocumentContentLimits.MaximumLocalesPerDocument;
    public const int MaximumLegalMarkdownUtf8BytesPerLocale =
        LegalDocumentContentLimits.MaximumMarkdownUtf8BytesPerLocale;
    public const int MaximumLegalLinksPerLocale =
        LegalDocumentContentLimits.MaximumLinksPerLocale;
    public const int MaximumLegalPlaceholdersPerLocale =
        LegalDocumentContentLimits.MaximumPlaceholdersPerLocale;
}

public static class ConfigurationManifestExportMetadataValues
{
    public const string OverridesView = "Overrides";
    public const string PortableView = "Portable";
    public const string InstanceAndTenantsAuthorityScope = "InstanceAndTenants";
    public const string TenantAuthorityScope = "Tenant";
}

public static class ConfigurationManifestDocumentKeys
{
    public const string InstancePaidEventPolicy = "instance.paid_event_policy";
    public const string TenantPaidEventPolicy = "tenant.paid_event_policy";
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfigurationManifestV1Alpha2
{
    [JsonPropertyName("$schema")]
    public required string Schema { get; init; }

    public required string ApiVersion { get; init; }

    public required string Kind { get; init; }

    public required ConfigurationManifestMetadataV1Alpha2 Metadata { get; init; }

    public required ConfigurationManifestSpecV1Alpha2 Spec { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfigurationManifestMetadataV1Alpha2
{
    public required string Name { get; init; }

    public ConfigurationManifestExportMetadataV1Alpha2? Export { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfigurationManifestExportMetadataV1Alpha2
{
    public required string View { get; init; }

    public required bool EffectiveValuesFlattened { get; init; }

    public required bool SensitiveValuesOmitted { get; init; }

    public required string AuthorityScope { get; init; }

    public required bool SovereignValuesOmitted { get; init; }

    public required IReadOnlyList<string> SovereignLockedFields { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfigurationManifestSpecV1Alpha2
{
    public required ConfigurationManifestInstanceV1Alpha2 Instance { get; init; }

    public required IReadOnlyList<ConfigurationManifestTenantV1Alpha2> Tenants { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfigurationManifestInstanceV1Alpha2
{
    public required IReadOnlyDictionary<string, JsonElement> Settings { get; init; }

    public required IReadOnlyDictionary<string, ConfigurationManifestDocumentV1Alpha2>
        Documents { get; init; }

    [JsonRequired]
    public IReadOnlyDictionary<string, ConfigurationManifestLegalDocumentV1Alpha2>
        LegalDocuments { get; init; } =
        new Dictionary<string, ConfigurationManifestLegalDocumentV1Alpha2>(
            StringComparer.Ordinal);
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfigurationManifestTenantV1Alpha2
{
    public required ConfigurationManifestTenantMetadataV1Alpha2 Metadata { get; init; }

    public required ConfigurationManifestTenantSpecV1Alpha2 Spec { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfigurationManifestTenantMetadataV1Alpha2
{
    public required string Name { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfigurationManifestTenantSpecV1Alpha2
{
    public required string DisplayName { get; init; }

    public required IReadOnlyDictionary<string, JsonElement> Settings { get; init; }

    public required IReadOnlyDictionary<string, ConfigurationManifestDocumentV1Alpha2>
        Documents { get; init; }

    [JsonRequired]
    public IReadOnlyDictionary<string, ConfigurationManifestLegalDocumentV1Alpha2>
        LegalDocuments { get; init; } =
        new Dictionary<string, ConfigurationManifestLegalDocumentV1Alpha2>(
            StringComparer.Ordinal);
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfigurationManifestDocumentV1Alpha2
{
    public required int SchemaVersion { get; init; }

    public required JsonElement Payload { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfigurationManifestBrandingPayloadV1Alpha2
{
    public string? DisplayName { get; init; }

    public string? LogoUrl { get; init; }

    public string? FaviconUrl { get; init; }

    public string? CustomCssUrl { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfigurationManifestPaidEventPolicyPayloadV1Alpha2
{
    public required bool IsPaymentsEnabled { get; init; }

    public required IReadOnlyList<int> AllowedOrganizerKindIds { get; init; }

    public required bool RequiresLocalVerification { get; init; }

    public required IReadOnlyList<string> AllowedCurrencyCodes { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required string? DefaultCurrencyCode { get; init; }

    public required IReadOnlyList<int> RefundProtectionIds { get; init; }

    public required IReadOnlyList<ConfigurationManifestPaidEventPolicyCurrencyRiskLimitV1Alpha2>
        CurrencyRiskLimits { get; init; }

    public required bool RequiresFirstPaidEventReview { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required int? FarFutureReviewThresholdDays { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfigurationManifestPaidEventPolicyCurrencyRiskLimitV1Alpha2
{
    public required string CurrencyCode { get; init; }

    public long? PerEventSalesCeilingMinor { get; init; }

    public int? PerEventSalesCountCeiling { get; init; }

    public long? RollingOrganizerSalesCeilingMinor { get; init; }

    public int? RollingOrganizerSalesCountCeiling { get; init; }

    public int? RollingOrganizerWindowDays { get; init; }

    public long? HighValueReviewThresholdMinor { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record TenantConfigurationPackageV1Alpha2
{
    [JsonPropertyName("$schema")]
    public required string Schema { get; init; }

    public required string ApiVersion { get; init; }

    public required string Kind { get; init; }

    public required TenantConfigurationPackageMetadataV1Alpha2 Metadata { get; init; }

    public required TenantConfigurationPackageSpecV1Alpha2 Spec { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record TenantConfigurationPackageMetadataV1Alpha2
{
    public required string Name { get; init; }

    public required TenantConfigurationPackageSourceV1Alpha2 Source { get; init; }

    public ConfigurationManifestExportMetadataV1Alpha2? Export { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record TenantConfigurationPackageSourceV1Alpha2
{
    public required string TenantName { get; init; }

    public string? InstanceName { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record TenantConfigurationPackageSpecV1Alpha2
{
    public required string DisplayName { get; init; }

    public required IReadOnlyDictionary<string, JsonElement> Settings { get; init; }

    public required IReadOnlyDictionary<string, ConfigurationManifestDocumentV1Alpha2>
        Documents { get; init; }

    [JsonRequired]
    public IReadOnlyDictionary<string, ConfigurationManifestLegalDocumentV1Alpha2>
        LegalDocuments { get; init; } =
        new Dictionary<string, ConfigurationManifestLegalDocumentV1Alpha2>(
            StringComparer.Ordinal);
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfigurationManifestLegalDocumentV1Alpha2
{
    public required string Kind { get; init; }

    public required string Audience { get; init; }

    public required string LifecycleIntent { get; init; }

    public DateTime? ProposedEffectiveAt { get; init; }

    public bool RequiresFreshAcceptance { get; init; }

    public string? AccountableIdentityReference { get; init; }

    public string? ChangeSummary { get; init; }

    public ConfigurationManifestLegalTemplateProvenanceV1Alpha2?
        TemplateProvenance { get; init; }

    [JsonRequired]
    public IReadOnlyList<string> JurisdictionAssumptions { get; init; } =
        Array.Empty<string>();

    public required IReadOnlyList<
        ConfigurationManifestLegalDocumentLocalizedSourceV1Alpha2> Localizations
    {
        get;
        init;
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfigurationManifestLegalDocumentLocalizedSourceV1Alpha2
{
    public required string LanguageTag { get; init; }

    public required string Title { get; init; }

    public required string Summary { get; init; }

    public required string Markdown { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfigurationManifestLegalTemplateProvenanceV1Alpha2
{
    public required string TemplateId { get; init; }

    public required string TemplateVersion { get; init; }

    public required string SourceKind { get; init; }

    public required string LicenseExpression { get; init; }

    public required string ReviewReference { get; init; }
}

public enum ConfigurationImportApplyMode
{
    PreviewOnly,
    CreateNew,
    MergeMissing,
    ApplySelected,
    ReplacePortableConfiguration,
    ReconcileManaged
}
