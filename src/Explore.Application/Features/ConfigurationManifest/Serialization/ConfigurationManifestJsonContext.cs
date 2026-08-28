// ABOUTME: Provides strict source-generated JSON metadata for ConfigurationManifest v1alpha1.
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
[JsonSerializable(typeof(ConfigurationManifestV1Alpha1))]
[JsonSerializable(typeof(ConfigurationManifestPaidEventPolicyPayloadV1Alpha1))]
public sealed partial class ConfigurationManifestJsonContext : JsonSerializerContext;
