// ABOUTME: Unit tests for SecretRefreshMetrics.
// Tests metric recording, consecutive failure tracking, and refresh timestamp.

using Explore.Secrets.Abstractions;
using Explore.Secrets.Observability;
using FluentAssertions;
using TUnit.Core;

namespace Explore.Secrets.UnitTests.Observability;

public class SecretRefreshMetricsTests : IDisposable
{
    private readonly SecretRefreshMetrics _metrics;

    public SecretRefreshMetricsTests()
    {
        _metrics = new SecretRefreshMetrics();
    }

    public void Dispose()
    {
        _metrics.Dispose();
    }

    [Test]
    public void Constructor_ShouldInitializeWithZeroFailures()
    {
        _metrics.ConsecutiveFailures.Should().Be(0);
    }

    [Test]
    public void Constructor_ShouldInitializeWithMinValueTimestamp()
    {
        _metrics.LastSuccessfulRefresh.Should().Be(DateTimeOffset.MinValue);
    }

    [Test]
    public void RecordRefreshSuccess_ShouldUpdateLastSuccessfulRefresh()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        _metrics.RecordRefreshSuccess(SecretProviderType.Infisical, 0.5);

        // Assert
        _metrics.LastSuccessfulRefresh.Should().BeOnOrAfter(before);
        _metrics.LastSuccessfulRefresh.Should().BeOnOrBefore(DateTimeOffset.UtcNow);
    }

    [Test]
    public void RecordRefreshSuccess_ShouldResetConsecutiveFailures()
    {
        // Arrange - Record some failures first
        _metrics.RecordRefreshFailure(SecretProviderType.Infisical, 0.1);
        _metrics.RecordRefreshFailure(SecretProviderType.Infisical, 0.1);
        _metrics.ConsecutiveFailures.Should().Be(2);

        // Act
        _metrics.RecordRefreshSuccess(SecretProviderType.Infisical, 0.5);

        // Assert
        _metrics.ConsecutiveFailures.Should().Be(0);
    }

    [Test]
    public void RecordRefreshFailure_ShouldIncrementConsecutiveFailures()
    {
        // Act
        _metrics.RecordRefreshFailure(SecretProviderType.Vault, 1.0);
        _metrics.RecordRefreshFailure(SecretProviderType.Vault, 1.5);
        _metrics.RecordRefreshFailure(SecretProviderType.Vault, 2.0);

        // Assert
        _metrics.ConsecutiveFailures.Should().Be(3);
    }

    [Test]
    public void RecordRefreshFailure_ShouldNotUpdateLastSuccessfulRefresh()
    {
        // Arrange
        var initial = _metrics.LastSuccessfulRefresh;

        // Act
        _metrics.RecordRefreshFailure(SecretProviderType.Infisical, 0.5);

        // Assert
        _metrics.LastSuccessfulRefresh.Should().Be(initial);
    }

    [Test]
    public void StartRefreshOperation_Complete_ShouldRecordSuccess()
    {
        // Arrange
        using var operation = _metrics.StartRefreshOperation(SecretProviderType.AzureKeyVault);

        // Act
        operation.Complete();

        // Assert
        _metrics.ConsecutiveFailures.Should().Be(0);
        _metrics.LastSuccessfulRefresh.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void StartRefreshOperation_Fail_ShouldRecordFailure()
    {
        // Arrange
        using var operation = _metrics.StartRefreshOperation(SecretProviderType.AwsSecretsManager);

        // Act
        operation.Fail("timeout");

        // Assert
        _metrics.ConsecutiveFailures.Should().Be(1);
    }

    [Test]
    public void StartRefreshOperation_Dispose_ShouldNotRecordMetrics()
    {
        // Arrange
        var initialTimestamp = _metrics.LastSuccessfulRefresh;
        var initialFailures = _metrics.ConsecutiveFailures;

        // Act - Only dispose, don't call Complete() or Fail()
        using (var operation = _metrics.StartRefreshOperation(SecretProviderType.Infisical))
        {
            // Let it dispose without calling Complete or Fail
        }

        // Assert - Metrics should remain unchanged
        _metrics.LastSuccessfulRefresh.Should().Be(initialTimestamp);
        _metrics.ConsecutiveFailures.Should().Be(initialFailures);
    }

    [Test]
    public void MeterName_ShouldBeExploreSecrets()
    {
        SecretRefreshMetrics.MeterName.Should().Be("Explore.Secrets");
    }

    [Test]
    public void MultipleProviders_ShouldTrackIndependently()
    {
        // Act - Failures on different providers
        _metrics.RecordRefreshFailure(SecretProviderType.Infisical, 0.1);
        _metrics.RecordRefreshFailure(SecretProviderType.Vault, 0.1);

        // Assert - All failures counted together (single metrics instance)
        _metrics.ConsecutiveFailures.Should().Be(2);
    }

    [Test]
    public void RecordRefreshSuccess_AfterMultipleFailures_ShouldResetAll()
    {
        // Arrange
        _metrics.RecordRefreshFailure(SecretProviderType.Infisical, 0.1);
        _metrics.RecordRefreshFailure(SecretProviderType.Infisical, 0.1);
        _metrics.RecordRefreshFailure(SecretProviderType.Infisical, 0.1);
        _metrics.ConsecutiveFailures.Should().Be(3);

        // Act
        _metrics.RecordRefreshSuccess(SecretProviderType.Infisical, 0.5);

        // Assert
        _metrics.ConsecutiveFailures.Should().Be(0);
    }
}
