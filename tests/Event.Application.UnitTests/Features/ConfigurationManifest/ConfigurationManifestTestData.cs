// ABOUTME: Builds canonical tenant-configuration manifest objects for focused Application tests.
// ABOUTME: Keeps valid envelope and tenant defaults centralized so each test changes one concern.

namespace Event.Application.UnitTests.Features.ConfigurationManifest;

using System.Text.Json;
using Explore.Application.Features.ConfigurationManifest.Contracts;

internal static class ConfigurationManifestTestData
{
    public static ConfigurationManifestV1Alpha1 Valid(
        string tenantSlug = "default",
        IReadOnlyDictionary<string, JsonElement>? settings = null,
        IReadOnlyDictionary<string, ConfigurationManifestDocumentV1Alpha1>? documents = null,
        IReadOnlyDictionary<string, JsonElement>? instanceSettings = null,
        IReadOnlyDictionary<string, ConfigurationManifestDocumentV1Alpha1>? instanceDocuments = null) =>
        new()
        {
            Schema = ConfigurationManifestContractMetadata.SchemaId,
            ApiVersion = ConfigurationManifestContractMetadata.ApiVersion,
            Kind = ConfigurationManifestContractMetadata.Kind,
            Metadata = new ConfigurationManifestMetadataV1Alpha1
            {
                Name = "primary-deployment"
            },
            Spec = new ConfigurationManifestSpecV1Alpha1
            {
                Instance = new ConfigurationManifestInstanceV1Alpha1
                {
                    Settings = instanceSettings
                        ?? new Dictionary<string, JsonElement>(StringComparer.Ordinal),
                    Documents = instanceDocuments
                        ?? new Dictionary<string, ConfigurationManifestDocumentV1Alpha1>(
                            StringComparer.Ordinal)
                },
                Tenants =
                [
                    new ConfigurationManifestTenantV1Alpha1
                    {
                        Metadata = new ConfigurationManifestTenantMetadataV1Alpha1
                        {
                            Name = tenantSlug
                        },
                        Spec = new ConfigurationManifestTenantSpecV1Alpha1
                        {
                            DisplayName = "Primary Community",
                            Settings = settings ?? new Dictionary<string, JsonElement>(StringComparer.Ordinal),
                            Documents = documents
                                ?? new Dictionary<string, ConfigurationManifestDocumentV1Alpha1>(
                                    StringComparer.Ordinal)
                        }
                    }
                ]
            }
        };

    public static JsonElement Json(string value) =>
        JsonDocument.Parse(value).RootElement.Clone();
}
