// ABOUTME: Architecture tests for authentication and federation governance key definitions.
// ABOUTME: Guards key names and alias mappings to prevent configuration regressions.

namespace Event.Architecture.Tests;

using System.Reflection;
using Explore.Domain.Constants;

public class GovernanceSettingKeysTests
{
    [Test]
    public async Task AuthenticationGovernanceKeys_ShouldUseAuthPrefix()
    {
        var keyFields = typeof(GovernanceSettingKeys.Authentication)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string));

        foreach (var field in keyFields)
        {
            var value = field.GetRawConstantValue() as string;
            await Assert.That(value).IsNotNull();
            await Assert.That(value!).StartsWith("auth.");
        }
    }

    [Test]
    public async Task FederationGovernanceKeys_ShouldUseFederationPrefix()
    {
        var keyFields = typeof(GovernanceSettingKeys.Federation)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string));

        foreach (var field in keyFields)
        {
            var value = field.GetRawConstantValue() as string;
            await Assert.That(value).IsNotNull();
            await Assert.That(value!).StartsWith("federation.");
        }
    }

    [Test]
    public async Task FlatAliases_ShouldMatchGroupedAuthenticationAndFederationKeys()
    {
        await Assert.That(GovernanceSettingKeys.AuthKeycloakEnabled).IsEqualTo(GovernanceSettingKeys.Authentication.KeycloakEnabled);
        await Assert.That(GovernanceSettingKeys.AuthKeycloakAuthority).IsEqualTo(GovernanceSettingKeys.Authentication.KeycloakAuthority);
        await Assert.That(GovernanceSettingKeys.AuthKeycloakClientId).IsEqualTo(GovernanceSettingKeys.Authentication.KeycloakClientId);
        await Assert.That(GovernanceSettingKeys.AuthAtprotoLoginEnabled).IsEqualTo(GovernanceSettingKeys.Authentication.AtprotoLoginEnabled);
        await Assert.That(GovernanceSettingKeys.AuthAtprotoPublicUrl).IsEqualTo(GovernanceSettingKeys.Authentication.AtprotoPublicUrl);
        await Assert.That(GovernanceSettingKeys.AuthGoogleSsoEnabled).IsEqualTo(GovernanceSettingKeys.Authentication.GoogleSsoEnabled);
        await Assert.That(GovernanceSettingKeys.AuthGoogleClientId).IsEqualTo(GovernanceSettingKeys.Authentication.GoogleClientId);
        await Assert.That(GovernanceSettingKeys.FederationDecentralizationEnabled).IsEqualTo(GovernanceSettingKeys.Federation.DecentralizationEnabled);
    }

    [Test]
    public async Task AuthenticationSecretKeys_ShouldRemainInInfrastructureSecretSettingKeys()
    {
        await Assert.That(GovernanceSettingKeys.AuthKeycloakClientSecret).IsEqualTo(InfrastructureSecretSettingKeys.Authentication.KeycloakClientSecret);
        await Assert.That(GovernanceSettingKeys.AuthGoogleClientSecret).IsEqualTo(InfrastructureSecretSettingKeys.Authentication.GoogleClientSecret);
        await Assert.That(InfrastructureSecretSettingKeys.Authentication.KeycloakClientSecret).IsEqualTo("auth.keycloak_client_secret");
        await Assert.That(InfrastructureSecretSettingKeys.Authentication.GoogleClientSecret).IsEqualTo("auth.google_client_secret");
    }
}
