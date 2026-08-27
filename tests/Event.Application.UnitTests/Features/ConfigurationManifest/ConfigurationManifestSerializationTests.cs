// ABOUTME: Pins strict source-generated JSON behavior for the configuration manifest.
// ABOUTME: Verifies canonical envelope names, flat keys, and unknown-member rejection.

namespace Event.Application.UnitTests.Features.ConfigurationManifest;

using System.Text.Json;
using Explore.Application.Features.ConfigurationManifest.Contracts;
using Explore.Application.Features.ConfigurationManifest.Serialization;

public sealed class ConfigurationManifestSerializationTests
{
    [Test]
    public async Task Serialize_UsesCanonicalEnvelopeAndFlatSettingKeys()
    {
        var settings = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            ["events.require_approval"] = ConfigurationManifestTestData.Json("true")
        };
        ConfigurationManifestV1Alpha1 manifest = ConfigurationManifestTestData.Valid(settings: settings);

        string json = JsonSerializer.Serialize(
            manifest,
            ConfigurationManifestJsonContext.Default.ConfigurationManifestV1Alpha1);

        await Assert.That(json.Contains("\"$schema\"", StringComparison.Ordinal)).IsTrue();
        await Assert.That(json.Contains("\"apiVersion\"", StringComparison.Ordinal)).IsTrue();
        await Assert.That(json.Contains("\"events.require_approval\"", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Deserialize_UnknownRootMember_ThrowsJsonException()
    {
        const string json =
            """
            {
              "$schema": "https://schemas.islamu.org/event/configuration-manifest/v1alpha1/schema.json",
              "apiVersion": "configuration.islamu.org/v1alpha1",
              "kind": "ConfigurationManifest",
              "metadata": { "name": "primary-deployment" },
              "spec": {
                "instance": {
                  "settings": {},
                  "documents": {}
                },
                "tenants": []
              },
              "unexpected": true
            }
            """;
        JsonException? exception = null;

        try
        {
            _ = JsonSerializer.Deserialize(
                json,
                ConfigurationManifestJsonContext.Default.ConfigurationManifestV1Alpha1);
        }
        catch (JsonException caught)
        {
            exception = caught;
        }

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task RoundTrip_PreservesCanonicalManifestIdentity()
    {
        ConfigurationManifestV1Alpha1 expected = ConfigurationManifestTestData.Valid();
        string json = JsonSerializer.Serialize(
            expected,
            ConfigurationManifestJsonContext.Default.ConfigurationManifestV1Alpha1);

        ConfigurationManifestV1Alpha1? actual = JsonSerializer.Deserialize(
            json,
            ConfigurationManifestJsonContext.Default.ConfigurationManifestV1Alpha1);

        await Assert.That(actual).IsNotNull();
        await Assert.That(actual!.Schema).IsEqualTo(expected.Schema);
        await Assert.That(actual.ApiVersion).IsEqualTo(expected.ApiVersion);
        await Assert.That(actual.Kind).IsEqualTo(expected.Kind);
        await Assert.That(actual.Spec.Tenants[0].Metadata.Name).IsEqualTo("default");
    }
}
