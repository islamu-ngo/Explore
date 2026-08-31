// ABOUTME: Provides source-generated JSON metadata for package-free v1alpha2 portability artifacts.
// ABOUTME: Preserves strict camel-case, case-sensitive, compact, null-omitting wire behavior.

namespace ISLAMU.Wire.Contracts.ConfigurationPortability;

using System.Text.Json;
using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = false,
    AllowTrailingCommas = false,
    ReadCommentHandling = JsonCommentHandling.Disallow,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    GenerationMode = JsonSourceGenerationMode.Metadata,
    WriteIndented = false)]
[JsonSerializable(typeof(ConfigurationManifestV1Alpha2))]
[JsonSerializable(typeof(TenantConfigurationPackageV1Alpha2))]
[JsonSerializable(typeof(ConfigurationManifestLegalDocumentV1Alpha2))]
[JsonSerializable(typeof(ConfigurationManifestPaidEventPolicyPayloadV1Alpha2))]
public sealed partial class ConfigurationPortabilityJsonContext : JsonSerializerContext;
