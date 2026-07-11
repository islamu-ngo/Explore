// ABOUTME: Unit tests for SecretProviderOptionsValidator.
// Tests validation rules for each provider type configuration.

using Explore.Secrets.Abstractions;
using Explore.Secrets.Configuration;
using Explore.Secrets.Validation;
using FluentAssertions;
using TUnit.Core;

namespace Explore.Secrets.UnitTests.Validation;

public class SecretProviderOptionsValidatorTests
{
    private readonly SecretProviderOptionsValidator _validator;

    public SecretProviderOptionsValidatorTests()
    {
        _validator = new SecretProviderOptionsValidator();
    }

    #region None Provider Tests

    [Test]
    public void Validate_WhenProviderIsNone_ShouldSucceed()
    {
        // Arrange
        var options = new SecretProviderOptions { Provider = SecretProviderType.None };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    #endregion

    #region Infisical Provider Tests

    [Test]
    public void Validate_WhenInfisicalMissingUrl_ShouldFail()
    {
        // Arrange
        var options = new SecretProviderOptions
        {
            Provider = SecretProviderType.Infisical,
            Infisical = new InfisicalOptions
            {
                Url = null,
                ProjectId = "project-id",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                Environment = "dev"
            }
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("URL is required");
    }

    [Test]
    public void Validate_WhenInfisicalMissingProjectId_ShouldFail()
    {
        // Arrange
        var options = new SecretProviderOptions
        {
            Provider = SecretProviderType.Infisical,
            Infisical = new InfisicalOptions
            {
                Url = "https://infisical.example.com",
                ProjectId = null,
                ClientId = "client-id",
                ClientSecret = "client-secret",
                Environment = "dev"
            }
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("Project ID is required");
    }

    [Test]
    public void Validate_WhenInfisicalInvalidUrl_ShouldFail()
    {
        // Arrange
        var options = new SecretProviderOptions
        {
            Provider = SecretProviderType.Infisical,
            Infisical = new InfisicalOptions
            {
                Url = "not-a-valid-url",
                ProjectId = "project-id",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                Environment = "dev"
            }
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("valid HTTP/HTTPS URL");
    }

    [Test]
    public void Validate_WhenInfisicalComplete_ShouldSucceed()
    {
        // Arrange
        var options = new SecretProviderOptions
        {
            Provider = SecretProviderType.Infisical,
            Infisical = new InfisicalOptions
            {
                Url = "https://infisical.example.com",
                ProjectId = "project-id",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                Environment = "dev"
            }
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    #endregion

    #region Vault Provider Tests

    [Test]
    public void Validate_WhenVaultMissingUrl_ShouldFail()
    {
        // Arrange
        var options = new SecretProviderOptions
        {
            Provider = SecretProviderType.Vault,
            Vault = new VaultOptions
            {
                Url = null,
                RoleId = "role-id",
                SecretId = "secret-id",
                Paths = ["secret/data/explore"]
            }
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("URL is required");
    }

    [Test]
    public void Validate_WhenVaultMissingPaths_ShouldFail()
    {
        // Arrange
        var options = new SecretProviderOptions
        {
            Provider = SecretProviderType.Vault,
            Vault = new VaultOptions
            {
                Url = "https://vault.example.com:8200",
                RoleId = "role-id",
                SecretId = "secret-id",
                Paths = [] // Empty paths
            }
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("path is required");
    }

    [Test]
    public void Validate_WhenVaultComplete_ShouldSucceed()
    {
        // Arrange
        var options = new SecretProviderOptions
        {
            Provider = SecretProviderType.Vault,
            Vault = new VaultOptions
            {
                Url = "https://vault.example.com:8200",
                RoleId = "role-id",
                SecretId = "secret-id",
                Paths = ["secret/data/explore"]
            }
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    #endregion

    #region Azure Key Vault Tests

    [Test]
    public void Validate_WhenAzureKvMissingUrl_ShouldFail()
    {
        // Arrange
        var options = new SecretProviderOptions
        {
            Provider = SecretProviderType.AzureKeyVault,
            AzureKeyVault = new AzureKeyVaultOptions
            {
                VaultUrl = null
            }
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("URL is required");
    }

    [Test]
    public void Validate_WhenAzureKvInvalidUrl_ShouldFail()
    {
        // Arrange
        var options = new SecretProviderOptions
        {
            Provider = SecretProviderType.AzureKeyVault,
            AzureKeyVault = new AzureKeyVaultOptions
            {
                VaultUrl = "https://mykeyvault.example.com" // Not .vault.azure.net
            }
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain(".vault.azure.net");
    }

    [Test]
    public void Validate_WhenAzureKvWithServicePrincipalMissingTenant_ShouldFail()
    {
        // Arrange
        var options = new SecretProviderOptions
        {
            Provider = SecretProviderType.AzureKeyVault,
            AzureKeyVault = new AzureKeyVaultOptions
            {
                VaultUrl = "https://mykeyvault.vault.azure.net/",
                ClientId = "client-id",
                // Missing TenantId and ClientSecret
            }
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("Tenant ID is required");
    }

    [Test]
    public void Validate_WhenAzureKvWithManagedIdentity_ShouldSucceed()
    {
        // Arrange
        var options = new SecretProviderOptions
        {
            Provider = SecretProviderType.AzureKeyVault,
            AzureKeyVault = new AzureKeyVaultOptions
            {
                VaultUrl = "https://mykeyvault.vault.azure.net/"
                // No ClientId = Managed Identity
            }
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    #endregion

    #region AWS Secrets Manager Tests

    [Test]
    public void Validate_WhenAwsMissingRegion_ShouldFail()
    {
        // Arrange
        var options = new SecretProviderOptions
        {
            Provider = SecretProviderType.AwsSecretsManager,
            AwsSecretsManager = new AwsSecretsManagerOptions
            {
                Region = null,
                SecretNames = ["my-secret"]
            }
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("Region is required");
    }

    [Test]
    public void Validate_WhenAwsMissingSecretNames_ShouldFail()
    {
        // Arrange
        var options = new SecretProviderOptions
        {
            Provider = SecretProviderType.AwsSecretsManager,
            AwsSecretsManager = new AwsSecretsManagerOptions
            {
                Region = "us-east-1",
                SecretNames = [] // Empty
            }
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("secret name is required");
    }

    [Test]
    public void Validate_WhenAwsWithExplicitCredsMissingSecret_ShouldFail()
    {
        // Arrange
        var options = new SecretProviderOptions
        {
            Provider = SecretProviderType.AwsSecretsManager,
            AwsSecretsManager = new AwsSecretsManagerOptions
            {
                Region = "us-east-1",
                SecretNames = ["my-secret"],
                AccessKeyId = "AKIAIOSFODNN7EXAMPLE",
                SecretAccessKey = null // Missing
            }
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("Secret Access Key is required");
    }

    [Test]
    public void Validate_WhenAwsWithIrsa_ShouldSucceed()
    {
        // Arrange
        var options = new SecretProviderOptions
        {
            Provider = SecretProviderType.AwsSecretsManager,
            AwsSecretsManager = new AwsSecretsManagerOptions
            {
                Region = "us-east-1",
                SecretNames = ["my-secret"]
                // No AccessKeyId = IRSA
            }
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    #endregion
}
