// ABOUTME: Unit tests for AuditingSecretProviderDecorator.
// Tests audit logging, key redaction, and decorator passthrough behavior.

using Explore.Secrets.Abstractions;
using Explore.Secrets.Providers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TUnit.Core;

namespace Explore.Secrets.UnitTests.Providers;

public class AuditingSecretProviderDecoratorTests
{
    private readonly ISecretProvider _innerProvider;
    private readonly ISecretAuditLogger _auditLogger;
    private readonly ILogger<AuditingSecretProviderDecorator> _logger;
    private readonly AuditingSecretProviderDecorator _decorator;
    private readonly List<SecretAuditEntry> _capturedAuditEntries;

    public AuditingSecretProviderDecoratorTests()
    {
        _innerProvider = Substitute.For<ISecretProvider>();
        _auditLogger = Substitute.For<ISecretAuditLogger>();
        _logger = Substitute.For<ILogger<AuditingSecretProviderDecorator>>();

        _capturedAuditEntries = new List<SecretAuditEntry>();
        _auditLogger.LogAsync(Arg.Any<SecretAuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci => _capturedAuditEntries.Add(ci.Arg<SecretAuditEntry>()));

        _decorator = new AuditingSecretProviderDecorator(
            _innerProvider,
            _auditLogger,
            _logger,
            httpContextAccessor: null);
    }

    [Test]
    public void ProviderType_ShouldDelegateToInner()
    {
        // Arrange
        _innerProvider.ProviderType.Returns(SecretProviderType.Vault);

        // Act & Assert
        _decorator.ProviderType.Should().Be(SecretProviderType.Vault);
    }

    [Test]
    public void SupportsRefresh_ShouldDelegateToInner()
    {
        // Arrange
        _innerProvider.SupportsRefresh.Returns(true);

        // Act & Assert
        _decorator.SupportsRefresh.Should().BeTrue();
    }

    [Test]
    public async Task InitializeAsync_ShouldDelegateAndLogSuccess()
    {
        // Arrange
        _innerProvider.ProviderType.Returns(SecretProviderType.Infisical);
        _innerProvider.InitializeAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _decorator.InitializeAsync();

        // Assert
        await _innerProvider.Received(1).InitializeAsync(Arg.Any<CancellationToken>());
        _capturedAuditEntries.Should().ContainSingle(e =>
            e.Operation == SecretOperation.Initialize && e.Success);
    }

    [Test]
    public async Task InitializeAsync_WhenFails_ShouldLogFailureAndRethrow()
    {
        // Arrange
        _innerProvider.ProviderType.Returns(SecretProviderType.Vault);
        _innerProvider.InitializeAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(SecretProviderException.Permanent("Auth failed", SecretProviderType.Vault, "Initialize")));

        // Act
        var act = async () => await _decorator.InitializeAsync();

        // Assert
        await act.Should().ThrowAsync<SecretProviderException>();
        _capturedAuditEntries.Should().ContainSingle(e =>
            e.Operation == SecretOperation.InitializeFailed &&
            !e.Success &&
            e.ErrorMessage == "Auth failed");
    }

    [Test]
    public async Task GetSecretAsync_ShouldDelegateAndLogAccess()
    {
        // Arrange
        _innerProvider.ProviderType.Returns(SecretProviderType.Infisical);
        _innerProvider.GetSecretAsync("Database:Host", Arg.Any<CancellationToken>())
            .Returns("localhost");

        // Act
        var result = await _decorator.GetSecretAsync("Database:Host");

        // Assert
        result.Should().Be("localhost");
        await _innerProvider.Received(1).GetSecretAsync("Database:Host", Arg.Any<CancellationToken>());
        _capturedAuditEntries.Should().ContainSingle(e =>
            e.Operation == SecretOperation.Access && e.Success);
    }

    [Test]
    public async Task GetSecretAsync_WhenNotFound_ShouldLogAsNotSuccess()
    {
        // Arrange
        _innerProvider.ProviderType.Returns(SecretProviderType.None);
        _innerProvider.GetSecretAsync("Missing:Key", Arg.Any<CancellationToken>())
            .Returns((string?)null);

        // Act
        var result = await _decorator.GetSecretAsync("Missing:Key");

        // Assert
        result.Should().BeNull();
        _capturedAuditEntries.Should().ContainSingle(e =>
            e.Operation == SecretOperation.Access && !e.Success);
    }

    [Test]
    public async Task GetSecretAsync_ShouldRedactSensitiveKeys()
    {
        // Arrange
        _innerProvider.ProviderType.Returns(SecretProviderType.Infisical);
        _innerProvider.GetSecretAsync("Database:ConnectionString", Arg.Any<CancellationToken>())
            .Returns("Server=localhost;Password=secret");

        // Act
        await _decorator.GetSecretAsync("Database:ConnectionString");

        // Assert - Key should be redacted in audit log
        _capturedAuditEntries.Should().ContainSingle();
        _capturedAuditEntries[0].KeyPattern.Should().Be("Database:***");
    }

    [Test]
    public async Task GetSecretAsync_ShouldRedactPasswordKeys()
    {
        // Arrange
        _innerProvider.ProviderType.Returns(SecretProviderType.Vault);
        _innerProvider.GetSecretAsync("Smtp:Password", Arg.Any<CancellationToken>())
            .Returns("secret123");

        // Act
        await _decorator.GetSecretAsync("Smtp:Password");

        // Assert
        _capturedAuditEntries[0].KeyPattern.Should().Be("Smtp:***");
    }

    [Test]
    public async Task GetSecretAsync_ShouldRedactSecretKeys()
    {
        // Arrange
        _innerProvider.ProviderType.Returns(SecretProviderType.AzureKeyVault);
        _innerProvider.GetSecretAsync("Keycloak:ClientSecret", Arg.Any<CancellationToken>())
            .Returns("client-secret-value");

        // Act
        await _decorator.GetSecretAsync("Keycloak:ClientSecret");

        // Assert
        _capturedAuditEntries[0].KeyPattern.Should().Be("Keycloak:***");
    }

    [Test]
    public async Task GetSecretAsync_ShouldRedactApiKeyKeys()
    {
        // Arrange
        _innerProvider.ProviderType.Returns(SecretProviderType.AwsSecretsManager);
        _innerProvider.GetSecretAsync("External:ApiKey", Arg.Any<CancellationToken>())
            .Returns("api-key-123");

        // Act
        await _decorator.GetSecretAsync("External:ApiKey");

        // Assert
        _capturedAuditEntries[0].KeyPattern.Should().Be("External:***");
    }

    [Test]
    public async Task GetSecretAsync_ShouldNotRedactNonSensitiveKeys()
    {
        // Arrange
        _innerProvider.ProviderType.Returns(SecretProviderType.Infisical);
        _innerProvider.GetSecretAsync("Database:Host", Arg.Any<CancellationToken>())
            .Returns("localhost");

        // Act
        await _decorator.GetSecretAsync("Database:Host");

        // Assert - Non-sensitive key should not be redacted
        _capturedAuditEntries[0].KeyPattern.Should().Be("Database:Host");
    }

    [Test]
    public async Task GetSecretsByPathAsync_ShouldDelegateAndLog()
    {
        // Arrange
        _innerProvider.ProviderType.Returns(SecretProviderType.Infisical);
        var secrets = new Dictionary<string, string>
        {
            ["Database:Host"] = "localhost",
            ["Database:Port"] = "5432"
        };
        _innerProvider.GetSecretsByPathAsync("Database", Arg.Any<CancellationToken>())
            .Returns(secrets);

        // Act
        var result = await _decorator.GetSecretsByPathAsync("Database");

        // Assert
        result.Should().BeEquivalentTo(secrets);
        _capturedAuditEntries.Should().ContainSingle(e =>
            e.Operation == SecretOperation.Access &&
            e.KeyPattern!.Contains("Database") &&
            e.KeyPattern.Contains("2 secrets"));
    }

    [Test]
    public async Task RefreshAsync_ShouldDelegateAndLogSuccess()
    {
        // Arrange
        _innerProvider.ProviderType.Returns(SecretProviderType.Vault);
        _innerProvider.RefreshAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _decorator.RefreshAsync();

        // Assert
        await _innerProvider.Received(1).RefreshAsync(Arg.Any<CancellationToken>());
        _capturedAuditEntries.Should().ContainSingle(e =>
            e.Operation == SecretOperation.Refresh && e.Success);
    }

    [Test]
    public async Task RefreshAsync_WhenFails_ShouldLogFailureAndRethrow()
    {
        // Arrange
        _innerProvider.ProviderType.Returns(SecretProviderType.Infisical);
        _innerProvider.RefreshAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(SecretProviderException.Transient("Network timeout", SecretProviderType.Infisical, "Refresh")));

        // Act
        var act = async () => await _decorator.RefreshAsync();

        // Assert
        await act.Should().ThrowAsync<SecretProviderException>();
        _capturedAuditEntries.Should().ContainSingle(e =>
            e.Operation == SecretOperation.RefreshFailed &&
            !e.Success &&
            e.ErrorMessage == "Network timeout");
    }

    [Test]
    public async Task GetHealthAsync_ShouldDelegateWithoutLogging()
    {
        // Arrange
        var healthInfo = new ProviderHealthInfo(
            ProviderType: SecretProviderType.Infisical,
            IsHealthy: true,
            ConsecutiveFailures: 0,
            LastSuccessfulRefresh: DateTimeOffset.UtcNow,
            ErrorMessage: null);

        _innerProvider.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns(healthInfo);

        // Act
        var result = await _decorator.GetHealthAsync();

        // Assert
        result.Should().BeEquivalentTo(healthInfo);
        await _innerProvider.Received(1).GetHealthAsync(Arg.Any<CancellationToken>());
        // Health checks should not generate audit entries
        _capturedAuditEntries.Should().BeEmpty();
    }

    [Test]
    public async Task GetSecretWithMetadataAsync_ShouldDelegateAndLog()
    {
        // Arrange
        var secretValue = new SecretValue("secret-data", "v1", DateTimeOffset.UtcNow);
        _innerProvider.ProviderType.Returns(SecretProviderType.AzureKeyVault);
        _innerProvider.GetSecretWithMetadataAsync("App:Setting", Arg.Any<CancellationToken>())
            .Returns(secretValue);

        // Act
        var result = await _decorator.GetSecretWithMetadataAsync("App:Setting");

        // Assert
        result.Should().BeEquivalentTo(secretValue);
        _capturedAuditEntries.Should().ContainSingle(e => e.Operation == SecretOperation.Access);
    }

    [Test]
    public async Task AuditEntries_ShouldIncludeTimestamp()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;
        _innerProvider.ProviderType.Returns(SecretProviderType.None);
        _innerProvider.GetSecretAsync("Test:Key", Arg.Any<CancellationToken>())
            .Returns("value");

        // Act
        await _decorator.GetSecretAsync("Test:Key");

        // Assert
        _capturedAuditEntries[0].Timestamp.Should().BeOnOrAfter(before);
        _capturedAuditEntries[0].Timestamp.Should().BeOnOrBefore(DateTimeOffset.UtcNow);
    }

    [Test]
    public async Task AuditEntries_ShouldIncludeProviderType()
    {
        // Arrange
        _innerProvider.ProviderType.Returns(SecretProviderType.AwsSecretsManager);
        _innerProvider.GetSecretAsync("Test:Key", Arg.Any<CancellationToken>())
            .Returns("value");

        // Act
        await _decorator.GetSecretAsync("Test:Key");

        // Assert
        _capturedAuditEntries[0].ProviderType.Should().Be(SecretProviderType.AwsSecretsManager);
    }
}
