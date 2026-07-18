// ABOUTME: Unit tests for the three purpose-separated AT Protocol secret definitions.
// ABOUTME: Proves the canonical registry uses instance-only non-bootstrap bindings under the ATProto path.

using Explore.Domain.Enums;
using Explore.Domain.Secrets;
using FluentAssertions;

namespace Explore.Secrets.UnitTests.Configuration;

public sealed class AtprotoSecretDefinitionTests
{
    [Test]
    public void RegistryDefinesSeparateInstanceScopedAtprotoKeyPurposes()
    {
        var expectations = new Dictionary<string, string>
        {
            [SecretDefinitionRegistry.Keys.Atproto.OAuthClientPrivateJwks] = "ATPROTO_OAUTH_CLIENT_PRIVATE_JWKS",
            [SecretDefinitionRegistry.Keys.Atproto.SessionEncryptionKeyRing] = "ATPROTO_SESSION_ENCRYPTION_KEYRING",
            [SecretDefinitionRegistry.Keys.Atproto.SessionJwtPrivateJwks] = "ATPROTO_SESSION_JWT_PRIVATE_JWKS"
        };

        foreach (var expectation in expectations)
        {
            var definition = SecretDefinitionRegistry.GetRequired(expectation.Key);
            definition.AllowedScopes.Should().Equal(SecretScope.Instance);
            definition.DefaultInfisicalPath.Should().Be("/atproto");
            definition.DefaultInfisicalKey.Should().Be(expectation.Value);
            definition.DefaultEnvironmentVariableName.Should().Be(expectation.Value);
            definition.IsBootstrapSecret.Should().BeFalse();
        }
    }
}
