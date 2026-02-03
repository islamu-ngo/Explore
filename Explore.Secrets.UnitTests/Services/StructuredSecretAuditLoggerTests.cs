// ABOUTME: Unit tests for StructuredSecretAuditLogger.
// Tests structured logging output for audit entries.

using Explore.Secrets.Abstractions;
using Explore.Secrets.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TUnit.Core;

namespace Explore.Secrets.UnitTests.Services;

public class StructuredSecretAuditLoggerTests
{
    private readonly ILogger<StructuredSecretAuditLogger> _logger;
    private readonly StructuredSecretAuditLogger _auditLogger;

    public StructuredSecretAuditLoggerTests()
    {
        _logger = Substitute.For<ILogger<StructuredSecretAuditLogger>>();
        _auditLogger = new StructuredSecretAuditLogger(_logger);
    }

    [Test]
    public void Log_WithSuccessfulEntry_ShouldLogAtInformationLevel()
    {
        // Arrange
        var entry = new SecretAuditEntry(
            Operation: SecretOperation.Access,
            ProviderType: SecretProviderType.Infisical,
            KeyPattern: "Database:Host",
            Timestamp: DateTimeOffset.UtcNow,
            Success: true);

        // Act
        _auditLogger.Log(entry);

        // Assert - Should log at Information level for success
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public void Log_WithFailedEntry_ShouldLogAtWarningLevel()
    {
        // Arrange
        var entry = new SecretAuditEntry(
            Operation: SecretOperation.RefreshFailed,
            ProviderType: SecretProviderType.Vault,
            KeyPattern: null,
            Timestamp: DateTimeOffset.UtcNow,
            Success: false,
            ErrorMessage: "Connection timeout");

        // Act
        _auditLogger.Log(entry);

        // Assert - Should log at Warning level for failure
        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public async Task LogAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var entry = new SecretAuditEntry(
            Operation: SecretOperation.Initialize,
            ProviderType: SecretProviderType.AzureKeyVault,
            KeyPattern: null,
            Timestamp: DateTimeOffset.UtcNow,
            Success: true);

        // Act & Assert - Should complete without exception
        await _auditLogger.LogAsync(entry);
    }

    [Test]
    public void Log_ShouldIncludeEventIdFromOperation()
    {
        // Arrange
        var entry = new SecretAuditEntry(
            Operation: SecretOperation.Refresh,
            ProviderType: SecretProviderType.Infisical,
            KeyPattern: null,
            Timestamp: DateTimeOffset.UtcNow,
            Success: true);

        // Act
        _auditLogger.Log(entry);

        // Assert - EventId should match operation
        _logger.Received().Log(
            Arg.Any<LogLevel>(),
            Arg.Is<EventId>(e => e.Id == (int)SecretOperation.Refresh && e.Name == "Refresh"),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public void Log_WithNullKeyPattern_ShouldNotThrow()
    {
        // Arrange
        var entry = new SecretAuditEntry(
            Operation: SecretOperation.Initialize,
            ProviderType: SecretProviderType.None,
            KeyPattern: null,
            Timestamp: DateTimeOffset.UtcNow,
            Success: true);

        // Act & Assert
        var act = () => _auditLogger.Log(entry);
        act.Should().NotThrow();
    }

    [Test]
    public void Log_WithAllFields_ShouldNotThrow()
    {
        // Arrange
        var entry = new SecretAuditEntry(
            Operation: SecretOperation.Access,
            ProviderType: SecretProviderType.AwsSecretsManager,
            KeyPattern: "App:***",
            Timestamp: DateTimeOffset.UtcNow,
            UserId: "user-123",
            CorrelationId: "corr-456",
            Success: true,
            ErrorMessage: null);

        // Act & Assert
        var act = () => _auditLogger.Log(entry);
        act.Should().NotThrow();
    }

    [Test]
    public void Constructor_WithNullLogger_ShouldThrow()
    {
        // Act
        var act = () => new StructuredSecretAuditLogger(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
