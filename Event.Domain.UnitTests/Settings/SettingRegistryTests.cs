// ABOUTME: Tests for the SettingRegistry ensuring all definitions are valid, unique, and properly categorized.
// ABOUTME: Validates that every GovernanceSettingKey has a corresponding registry definition.

namespace Event.Domain.UnitTests.Settings;

using Explore.Domain.Constants;
using Explore.Domain.Settings;
using Explore.Domain.Settings.Definitions;

public class SettingRegistryTests
{
    [Test]
    public async Task All_ReturnsNonEmptyCollection()
    {
        await Assert.That(SettingRegistry.All.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task All_HasNoDuplicateKeys()
    {
        var keys = SettingRegistry.All.Select(d => d.Key).ToList();
        var distinctKeys = keys.Distinct().ToList();

        await Assert.That(keys.Count).IsEqualTo(distinctKeys.Count);
    }

    [Test]
    public async Task All_EveryDefinitionHasNonEmptyKey()
    {
        foreach (var definition in SettingRegistry.All)
        {
            await Assert.That(string.IsNullOrWhiteSpace(definition.Key)).IsFalse();
        }
    }

    [Test]
    public async Task All_EveryDefinitionHasCategory()
    {
        foreach (var definition in SettingRegistry.All)
        {
            await Assert.That(string.IsNullOrWhiteSpace(definition.Category)).IsFalse();
        }
    }

    [Test]
    public async Task All_EveryDefinitionHasDescription()
    {
        foreach (var definition in SettingRegistry.All)
        {
            await Assert.That(string.IsNullOrWhiteSpace(definition.Description)).IsFalse();
        }
    }

    [Test]
    public async Task All_MinScopeIsLessThanOrEqualToMaxScope()
    {
        foreach (var definition in SettingRegistry.All)
        {
            await Assert.That((int)definition.MinScope)
                .IsLessThanOrEqualTo((int)definition.MaxScope);
        }
    }

    [Test]
    public async Task Get_ReturnsDefinitionForKnownKey()
    {
        var definition = SettingRegistry.Get("email.smtp_host");

        await Assert.That(definition).IsNotNull();
        await Assert.That(definition!.Category).IsEqualTo("Email");
    }

    [Test]
    public async Task Get_ReturnsNullForUnknownKey()
    {
        var definition = SettingRegistry.Get("nonexistent.key");

        await Assert.That(definition).IsNull();
    }

    [Test]
    public async Task Contains_ReturnsTrueForRegisteredKey()
    {
        await Assert.That(SettingRegistry.Contains("deployment.mode")).IsTrue();
    }

    [Test]
    public async Task Contains_ReturnsFalseForUnregisteredKey()
    {
        await Assert.That(SettingRegistry.Contains("does.not.exist")).IsFalse();
    }

    [Test]
    public async Task GetByCategory_ReturnsDefinitionsForKnownCategory()
    {
        var emailSettings = SettingRegistry.GetByCategory("Email");

        await Assert.That(emailSettings.Count).IsGreaterThan(0);
        foreach (var d in emailSettings)
        {
            await Assert.That(d.Category).IsEqualTo("Email");
        }
    }

    [Test]
    public async Task GetByCategory_ReturnsEmptyForUnknownCategory()
    {
        var result = SettingRegistry.GetByCategory("NonExistentCategory");

        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AllCategories_ContainsExpectedCategories()
    {
        var categories = SettingRegistry.AllCategories;

        await Assert.That(categories).Contains("Email");
        await Assert.That(categories).Contains("Branding");
        await Assert.That(categories).Contains("Analytics");
        await Assert.That(categories).Contains("Routing");
    }

    [Test]
    public async Task Registry_CoversAllGovernanceSettingKeys()
    {
        // All keys from GovernanceSettingKeys nested classes should exist in the registry
        var governanceKeys = new[]
        {
            GovernanceSettingKeys.Deployment.Mode,
            GovernanceSettingKeys.Tenants.SelfServiceRegistration,
            GovernanceSettingKeys.Tenants.WhiteLabelingEnabled,
            GovernanceSettingKeys.Routing.DefaultPublicHomePage,
            GovernanceSettingKeys.Events.UserSubmissionEnabled,
            GovernanceSettingKeys.Events.RequireApproval,
            GovernanceSettingKeys.Events.CardClickOpensDetailPage,
            GovernanceSettingKeys.Organizations.VerificationRequired,
            GovernanceSettingKeys.Organizations.TenantCanOmitVerification,
            GovernanceSettingKeys.Modules.IslamicEnabled,
            GovernanceSettingKeys.Modules.TechEnabled,
            GovernanceSettingKeys.Branding.DisplayName,
            GovernanceSettingKeys.Branding.LogoUrl,
            GovernanceSettingKeys.Branding.FaviconUrl,
            GovernanceSettingKeys.Branding.CustomCssUrl,
            GovernanceSettingKeys.Domains.InstanceBaseDomain,
            GovernanceSettingKeys.Domains.AllowTenantCustomDomain,
            GovernanceSettingKeys.Domains.TenantSubdomain,
            GovernanceSettingKeys.Domains.TenantCustomDomain,
            GovernanceSettingKeys.Email.SmtpHost,
            GovernanceSettingKeys.Email.SmtpPort,
            GovernanceSettingKeys.Email.SmtpSecurity,
            GovernanceSettingKeys.Email.FromAddress,
            GovernanceSettingKeys.Email.FromName,
            GovernanceSettingKeys.Email.SmtpTimeoutSeconds,
            GovernanceSettingKeys.Email.SmtpSkipCertValidation,
            GovernanceSettingKeys.Storage.Endpoint,
            GovernanceSettingKeys.Storage.PublicEndpoint,
            GovernanceSettingKeys.Storage.BucketName,
            GovernanceSettingKeys.Storage.Region,
            GovernanceSettingKeys.Storage.ForcePathStyle,
            GovernanceSettingKeys.Storage.UploadUrlExpirationMinutes,
            GovernanceSettingKeys.Security.AuthorizationProvider,
            GovernanceSettingKeys.Cerbos.TenantCustomizationEnabled,
            GovernanceSettingKeys.Cerbos.Mode,
            GovernanceSettingKeys.Cerbos.CustomEndpoint,
            GovernanceSettingKeys.Cerbos.FailureMode,
            GovernanceSettingKeys.Cerbos.CustomAdminEndpoint,
            GovernanceSettingKeys.Analytics.Provider,
            GovernanceSettingKeys.Analytics.Enabled,
            GovernanceSettingKeys.Analytics.ApiKey,
            GovernanceSettingKeys.Analytics.EndpointUrl,
            GovernanceSettingKeys.Analytics.PersonalApiKey,
        };

        var missingKeys = governanceKeys
            .Where(key => !SettingRegistry.Contains(key))
            .ToList();

        await Assert.That(missingKeys.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Registry_CoversAllInfrastructureSecretKeys()
    {
        var secretKeys = new[]
        {
            InfrastructureSecretSettingKeys.Email.SmtpUsername,
            InfrastructureSecretSettingKeys.Email.SmtpPassword,
            InfrastructureSecretSettingKeys.Storage.AccessKeyId,
            InfrastructureSecretSettingKeys.Storage.SecretAccessKey,
            InfrastructureSecretSettingKeys.Cerbos.CustomAdminUsername,
            InfrastructureSecretSettingKeys.Cerbos.CustomAdminPassword,
        };

        var missingKeys = secretKeys
            .Where(key => !SettingRegistry.Contains(key))
            .ToList();

        await Assert.That(missingKeys.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Registry_SensitiveKeysAreFlagged()
    {
        var sensitiveKeys = new[]
        {
            InfrastructureSecretSettingKeys.Email.SmtpUsername,
            InfrastructureSecretSettingKeys.Email.SmtpPassword,
            InfrastructureSecretSettingKeys.Storage.AccessKeyId,
            InfrastructureSecretSettingKeys.Storage.SecretAccessKey,
            InfrastructureSecretSettingKeys.Cerbos.CustomAdminUsername,
            InfrastructureSecretSettingKeys.Cerbos.CustomAdminPassword,
        };

        foreach (var key in sensitiveKeys)
        {
            var definition = SettingRegistry.Get(key);
            await Assert.That(definition).IsNotNull();
            await Assert.That(definition!.IsSensitive).IsTrue();
        }
    }

    [Test]
    public async Task Registry_DeploymentModeIsInstanceOnly()
    {
        var definition = SettingRegistry.Get("deployment.mode");

        await Assert.That(definition).IsNotNull();
        await Assert.That(definition!.MinScope).IsEqualTo(SettingScope.Instance);
        await Assert.That(definition.MaxScope).IsEqualTo(SettingScope.Instance);
    }

    [Test]
    public async Task Count_MatchesAllCount()
    {
        await Assert.That(SettingRegistry.Count).IsEqualTo(SettingRegistry.All.Count);
    }
}
