// ABOUTME: Provides strict source-generated JSON metadata for v1alpha2 portability artifacts.
// ABOUTME: Uses canonical camel-case names and rejects unmapped members through contract metadata.

namespace Explore.Application.Features.ConfigurationManifest.Serialization;

using System.Text.Json.Serialization;
using Explore.Application.Features.ConfigurationManifest.Contracts;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    GenerationMode = JsonSourceGenerationMode.Metadata,
    WriteIndented = false)]
[JsonSerializable(typeof(ConfigurationManifestV1Alpha2))]
[JsonSerializable(typeof(TenantConfigurationPackageV1Alpha2))]
[JsonSerializable(typeof(ConfigurationManifestLegalDocumentV1Alpha2))]
[JsonSerializable(typeof(ConfigurationManifestPaidEventPolicyPayloadV1Alpha2))]
public sealed partial class ConfigurationManifestJsonContext : JsonSerializerContext;
